using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Altinn.Dan.Plugin.Trad.Config;
using Altinn.Dan.Plugin.Trad.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nadobe.Common.Exceptions;
using Nadobe.Common.Util;
using Newtonsoft.Json;

namespace Altinn.Dan.Plugin.Trad
{
    public class ImportRegistry
    {
        private readonly ILogger _logger;
        private readonly ApplicationSettings _settings;
        private readonly HttpClient _client;
        private readonly IDistributedCache _cache;

        public ImportRegistry(ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory, IOptions<ApplicationSettings> settings, IDistributedCache cache)
        {
            _client = httpClientFactory.CreateClient("SafeHttpClient");
            _logger = loggerFactory.CreateLogger<ImportRegistry>();
            _settings = settings.Value;
            _cache = cache;
        }

#if !DEBUG
        [Function("ImportRegistry")]
        public async Task RunAsync([TimerTrigger("0 */5 * * * *")] MyInfo myTimer)
        {
            _logger.LogInformation($"Registry Import executed at: {DateTime.Now}");

            if (myTimer.IsPastDue)
            {
                _logger.LogInformation("Registry import was not run on schedule");
            }

            if (!Helpers.ShouldRunUpdate())
            {
                _logger.LogInformation("Skipping update outside of busy hours");
                return;
            }

            await PerformUpdate();

            _logger.LogInformation($"Import completed. Next scheduled import attempt at: {myTimer.ScheduleStatus.Next}");

        }


#else
        // This function is only used for local debugging, and expects to find a dump of advreg on disk
        [Function("Refresh")]
        public async Task<HttpResponseData> Refresh([HttpTrigger(AuthorizationLevel.Anonymous)] HttpRequestData req, FunctionContext context)
        {
            List<PersonInternal> debugRegistry;
            using (StreamReader file = File.OpenText(@"c:\repos\dan-plugin-trad\advreg.json"))
            {
                JsonSerializer serializer = new JsonSerializer();
                debugRegistry = (List<PersonInternal>)serializer.Deserialize(file, typeof(List<PersonInternal>));
            }

            if (debugRegistry == null) return req.CreateResponse(HttpStatusCode.InternalServerError)

            using (var _ = _logger.Timer("es-trad-update-cache"))
            {
                _logger.LogDebug($"Updating cache with {debugRegistry.Count} root entries");
                await UpdateCache(debugRegistry);
                _logger.LogDebug($"Done updating cache");
            }

            return req.CreateResponse(HttpStatusCode.OK);
        }
#endif

        public async Task PerformUpdate()
        {
            List<PersonInternal> registry;
            using (var _ = _logger.Timer("es-trad-fetch-people"))
            {
                _logger.LogDebug($"Attempting fetch from {_settings.RegistryURL}");
                registry = await GetPeople();
                _logger.LogDebug($"Fetched {registry.Count} entries");
            }

            using (var _ = _logger.Timer("es-trad-update-cache"))
            {
                _logger.LogDebug($"Updating cache with {registry.Count} root entries");
                await UpdateCache(registry);
                _logger.LogDebug($"Done updating cache");
            }
        }

        private async Task<List<PersonInternal>> GetPeople()
        {
            HttpResponseMessage result;
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, _settings.RegistryURL);
                request.Headers.Add("ApiKey", _settings.ApiKey);
                result = await _client.SendAsync(request);
            }
            catch (HttpRequestException ex)
            {
                throw new EvidenceSourcePermanentServerException(EvidenceSourceMetadata.ErrorCodeUpstreamError, null, ex);
            }

            if (!result.IsSuccessStatusCode)
            {
                _logger.LogError("Unable to fetch persons from TRAD, statuscode: {code} reasonphrase: {reason}", result.StatusCode.ToString(), result.ReasonPhrase);
                throw new EvidenceSourcePermanentClientException(EvidenceSourceMetadata.ErrorCodeUpstreamError, "Unable to fetch persons from TRAD");
            }

            try
            {
                var response = JsonConvert.DeserializeObject<List<PersonInternal>>(await result.Content.ReadAsStringAsync());
                return response;
            }
            catch (Exception e) {
                _logger.LogError("Unable to decode response from TRAD. {exception}: {message}", e.GetType().Name, e.Message);
                throw new EvidenceSourcePermanentServerException(EvidenceSourceMetadata.ErrorCodeUpstreamError,
                    "Did not understand the data model returned from upstream source");
            }
        }

        private async Task UpdateCache(List<PersonInternal> registry)
        {
            var seenPersons = new Dictionary<string, PersonInternal>();

            _logger.LogInformation("Starting updating principals ...");
            var principalCount = 0;

            foreach (PersonInternal person in registry)
            {
                if (person.Ssn == null)
                {
                    continue;
                }

                if (person.Ssn.Length == 10)
                {
                    person.Ssn = "0" + person.Ssn;
                }

                // The TR list contains a list of all "advokater" with zero or more practices containing zero or more authorized
                // representatives. The representatives are usually "fullmektige" that do not have their own list of representatives,
                // but may also be "advokater". Thus, persons may have practices where both AuthorizedRepresentatives and
                // IsAuthorizedRepresentative for is non-null. The relations does not nest - that is, in this case
                // ADV1
                //    -> PRACTICE1
                //             -> FULLMEKTIG1
                //             -> ADV2
                // ADV2
                //    -> PRACTICE1
                //             -> FULLMEKTIG2
                // even though FULLMEKTIG2 is a representative for ADV2, he/she isn't a representative for ADV1 unless explicitly stated
                //
                // Also note that there might be a case (as of writing one currently known instance) where a person is both a "advokat"
                // and a "fullmektig". This is not supported by the external model.
                

                // Create an empty seen-list, and add the current person if not already existing.
                // - Iterate all practices, find all associates, and add them to the seenList - expanding the list of isAuthorizedRepresentativeFor  

                bool isAlreadyAddedAsRepresentative = seenPersons.ContainsKey(person.Ssn);
                if (!isAlreadyAddedAsRepresentative)
                {
                    // We have not seen this person before, so add a full copy with all representatives
                    seenPersons[person.Ssn] = person with { };
                }

                if (person.Ssn == "19047434125")
                {
                    _logger.LogWarning("xx");
                }

                principalCount++;

                foreach (var practice in person.Practices)
                {
                    if (practice.AuthorizedRepresentatives == null || practice.AuthorizedRepresentatives.Count == 0) continue;

                    if (isAlreadyAddedAsRepresentative)
                    {
                        // The current person have been added to the seen list as an representative for someone else,
                        // so make sure we include the authorizedRepresentatives he/she has for this practice
                        if (!seenPersons[person.Ssn].Practices
                                .Exists(x => x.OrganizationNumber == practice.OrganizationNumber))
                        {
                            seenPersons[person.Ssn].Practices.Add(practice with {});
                        }
                        else
                        {
                            seenPersons[person.Ssn].Practices.First(x => 
                                x.OrganizationNumber == practice.OrganizationNumber).AuthorizedRepresentatives = practice.AuthorizedRepresentatives;
                        }
                    }

                    foreach (var associate in practice.AuthorizedRepresentatives)
                    {
                        if (associate.Ssn.Length == 10)
                        {
                            associate.Ssn = "0" + associate.Ssn;
                        }

                        if (!seenPersons.ContainsKey(associate.Ssn))
                        {
                            // Never seen this person before, add to seen-list with current practice without
                            // representatives and initialize a isRepresentativeFor with the current person

                            seenPersons[associate.Ssn] = associate with
                            {
                                Practices = new List<PracticeInternal>()
                                {
                                    practice with
                                    {
                                        AuthorizedRepresentatives = null,
                                        IsAnAuthorizedRepresentativeFor = new List<PersonInternal>
                                        {
                                            person with { Practices = null }
                                        }
                                    }
                                }
                            };
                        }
                        else if (!seenPersons[associate.Ssn].Practices
                                     .Exists(x => x.OrganizationNumber == practice.OrganizationNumber))
                        {
                            // We've seen this person before, but not the current practice. Add it without
                            // representatives and initialize a isRepresentativeFor with the current person

                            seenPersons[associate.Ssn].Practices.Add(
                                practice with { 
                                    AuthorizedRepresentatives = null, 
                                    IsAnAuthorizedRepresentativeFor = new List<PersonInternal> 
                                    {
                                        person with { Practices = null }

                                    }

                                });
                        }
                        else
                        {
                            // We've seen this person before, and the current practice. Initialize isRepresentativeFor if not already done, and
                            // add the current person to it

                            var i = seenPersons[associate.Ssn].Practices
                                .FindIndex(x => x.OrganizationNumber == practice.OrganizationNumber);

                            seenPersons[associate.Ssn].Practices[i].IsAnAuthorizedRepresentativeFor ??= new List<PersonInternal>();
                            seenPersons[associate.Ssn].Practices[i].IsAnAuthorizedRepresentativeFor.Add(person with { Practices = null });
                        }
                    }
                }
            }

            _logger.LogInformation($"Completed building list of {principalCount} principals and {seenPersons.Count-principalCount} associates, writing total {seenPersons.Count} entries to store ...");

            await UpdateCacheEntries(seenPersons);

            _logger.LogInformation("Completed writing persons");
        }

        private async Task UpdateCacheEntries(Dictionary<string, PersonInternal> persons)
        {
            // Number of concurrent tasks. 30 seems to do fine with current production workload.
            var throttler = new SemaphoreSlim(30);

            var tasks = persons.Values.ToList().Select(async value =>
            {
                await throttler.WaitAsync();
                try
                {
                    await UpdateCacheEntry(value);
                }
                finally
                {
                    throttler.Release();
                }
            });

            await Task.WhenAll(tasks);
        }

        private async Task UpdateCacheEntry(PersonInternal personInternal)
        {
            var key = Helpers.GetCacheKeyForSsn(personInternal.Ssn);
            var entry = JsonConvert.SerializeObject(personInternal);

            await _cache.SetAsync(key, Encoding.UTF8.GetBytes(entry), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(300)
            });
        }
    }

    public class MyInfo
    {
        public MyScheduleStatus ScheduleStatus { get; set; }

        public bool IsPastDue { get; set; }
    }

    public class MyScheduleStatus
    {
        public DateTime Last { get; set; }

        public DateTime Next { get; set; }

        public DateTime LastUpdated { get; set; }
    }
}
