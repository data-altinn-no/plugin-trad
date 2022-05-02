using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Altinn.Dan.Plugin.Trad.Config;
using Altinn.Dan.Plugin.Trad.Models;
using Microsoft.Azure.Functions.Worker;
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

        [Function("ImportRegistry")]
        public async Task RunAsync([TimerTrigger("0 */5 * * * *"
#if DEBUG
                , RunOnStartup = true
#endif
            )
        ] MyInfo myTimer)
        {
            _logger.LogInformation($"Registry Import executed at: {DateTime.Now}");

            if (myTimer.IsPastDue)
            {
                _logger.LogInformation($"Registry import was not run on schedule");
            }

            List<Person> registry;
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

            _logger.LogInformation($"Import completed, now has {registry.Count} root entries. Next scheduled import at: {myTimer.ScheduleStatus.Next}");
        }

        private async Task<List<Person>> GetPeople()
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
                var response = JsonConvert.DeserializeObject<List<Person>>(await result.Content.ReadAsStringAsync());
                return response;
            }
            catch (Exception e) {
                _logger.LogError("Unable to decode response from TRAD. {exception}: {message}", e.GetType().Name, e.Message);
                throw new EvidenceSourcePermanentServerException(EvidenceSourceMetadata.ErrorCodeUpstreamError,
                    "Did not understand the data model returned from upstream source");
            }
        }

        private async Task UpdateCache(List<Person> registry)
        {
            var seenPersons = new Dictionary<string, Person>();

            _logger.LogInformation("Starting updating principals ...");
            var principalCount = 0;

            foreach (Person person in registry)
            {
                if (person.Ssn == null)
                {
                    continue;
                }

                if (person.Ssn.Length == 10)
                {
                    person.Ssn = "0" + person.Ssn;
                }

                if (seenPersons.ContainsKey(person.Ssn))
                {
                    // We skip any associates found on root level, as we insert these ourselves mapping to 
                    // any principals
                    continue;
                }

                seenPersons[person.Ssn] = person with {};
                principalCount++;

                // Explicitly add associates with a link to the principal
                if (person.AuthorizedRepresentatives == null || person.AuthorizedRepresentatives.Count == 0) continue;

                foreach (var associate in person.AuthorizedRepresentatives)
                {
                    if (associate.Ssn.Length == 10)
                    {
                        associate.Ssn = "0" + associate.Ssn;
                    }

                    if (!seenPersons.ContainsKey(associate.Ssn))
                    {
                        seenPersons[associate.Ssn] = associate with { IsaAuthorizedRepresentativeFor = new List<Person>() };
                    }

                    seenPersons[associate.Ssn].IsaAuthorizedRepresentativeFor ??= new List<Person>();
                    seenPersons[associate.Ssn].IsaAuthorizedRepresentativeFor.Add(person with { AuthorizedRepresentatives = null });
                }
            }

            _logger.LogInformation($"Completed building list of {principalCount} principals and {seenPersons.Count-principalCount} associates, writing total {seenPersons.Count} entries to store ...");

            await UpdateCacheEntries(seenPersons);

            _logger.LogInformation("Completed writing persons");
        }

        private async Task UpdateCacheEntries(Dictionary<string, Person> persons)
        {

            ParallelOptions parallelOptions = new()
            {
                MaxDegreeOfParallelism = 25
            };

            await Parallel.ForEachAsync(persons.Values, parallelOptions, async (person, _) =>
            {
                await UpdateCacheEntry(person);
            });
        }

        private async Task UpdateCacheEntry(Person person)
        {
            var key = Helpers.GetCacheKeyForSsn(person.Ssn);
            var entry = JsonConvert.SerializeObject(person);

            await _cache.SetAsync(key, Encoding.UTF8.GetBytes(entry), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
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
