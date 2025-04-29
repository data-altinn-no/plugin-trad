using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Altinn.ApiClients.Maskinporten.Interfaces;
using Altinn.ApiClients.Maskinporten.Services;
using Altinn.Dan.Plugin.Trad.Config;
using Altinn.Dan.Plugin.Trad.Models;
using Altinn.Dan.Plugin.Trad.Services;
using Dan.Common.Exceptions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Altinn.Dan.Plugin.Trad;

public class ImportRegistry(
    ILoggerFactory loggerFactory,
    IHttpClientFactory httpClientFactory,
    IOptions<ApplicationSettings> settings,
    IDistributedCache cache,
    IConnectionMultiplexer connectionMultiplexer,
    IOrganizationService organizationService)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<ImportRegistry>();
    private readonly ApplicationSettings _settings = settings.Value;
    private readonly HttpClient _maskinportenClient = httpClientFactory.CreateClient("myMaskinportenClient");
    
    [Function("ImportRegistry")]
    public async Task RunAsync([TimerTrigger("0 */5 * * * *"
#if DEBUG
        , RunOnStartup = true
#endif
    )] TimerInfo myTimer)
    {
        _logger.LogInformation("Registry Import executed at: {Now}", DateTime.Now);

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

        _logger.LogInformation("Import completed. Next scheduled import attempt at: {ScheduleStatusNext}", myTimer.ScheduleStatus?.Next);

    }


    // This function is only used for local debugging, and expects to find a dump of advreg on disk
    [Function("DebugRefresh")]
    public async Task<HttpResponseData> DebugRefresh([HttpTrigger(AuthorizationLevel.Function)] HttpRequestData req, FunctionContext context)
    {
        List<PersonInternal> debugRegistry;
        using (StreamReader file = File.OpenText(@"c:\repos\dan-plugin-trad\advreg.json"))
        {
            JsonSerializer serializer = new JsonSerializer();
            debugRegistry = (List<PersonInternal>)serializer.Deserialize(file, typeof(List<PersonInternal>));
        }

        if (debugRegistry == null) return req.CreateResponse(HttpStatusCode.InternalServerError);

        using (var _ = _logger.Timer("es-trad-update-cache"))
        {
            _logger.LogDebug("Updating cache with {DebugRegistryCount} root entries", debugRegistry.Count);
            await UpdateCache(debugRegistry);
            _logger.LogDebug($"Done updating cache");
        }

        return req.CreateResponse(HttpStatusCode.OK);
    }

    public async Task PerformUpdate()
    {
        List<PersonInternal> registry;
        using (var _ = _logger.Timer("es-trad-fetch-people"))
        {
            _logger.LogDebug("Attempting fetch from {SettingsRegistryUrl}", _settings.RegistryURL);
            registry = await GetPeople();
            _logger.LogDebug("Fetched {RegistryCount} entries", registry.Count);
        }

        using (var _ = _logger.Timer("es-trad-update-cache"))
        {
            _logger.LogDebug("Updating cache with {RegistryCount} root entries", registry.Count);
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
            result = await _maskinportenClient.SendAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogCritical("Unable to fetch persons from TRAD, reasonphrase: {Reason}", ex.Message);
            throw new EvidenceSourcePermanentServerException(EvidenceSourceMetadata.ErrorCodeUpstreamError, null, ex);
        }

        if (!result.IsSuccessStatusCode)
        {
            _logger.LogCritical("Unable to fetch persons from TRAD, statuscode: {Code} reasonphrase: {Reason}", result.StatusCode.ToString(), result.ReasonPhrase);
            throw new EvidenceSourcePermanentClientException(EvidenceSourceMetadata.ErrorCodeUpstreamError, "Unable to fetch persons from TRAD");
        }

        try
        {
            var response = JsonConvert.DeserializeObject<List<PersonInternal>>(await result.Content.ReadAsStringAsync());
            return response;
        }
        catch (Exception e) {
            _logger.LogCritical("Unable to decode response from TRAD. {Exception}: {Message}", e.GetType().Name, e.Message);
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
            // even though FULLMEKTIG2 is a representative for ADV2, they aren't a representative for ADV1 unless explicitly stated
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

            principalCount++;

            if (person.Practices == null) continue;
            foreach (var practice in person.Practices)
            {
                var orgName = await organizationService.GetOrgName(practice.OrganizationNumber);
                if (orgName is not null)
                {
                    practice.OrganizationName = orgName;
                }

                if (practice.SubOrganizationNumber is not null)
                {
                    var subOrgName = await organizationService.GetOrgName(practice.SubOrganizationNumber.Value);
                    if (subOrgName is not null)
                    {
                        practice.SubOrganizationName = subOrgName;
                    }
                }
                
                if (practice.AuthorizedRepresentatives == null || practice.AuthorizedRepresentatives.Count == 0) continue;

                if (isAlreadyAddedAsRepresentative)
                {
                    // The current person have been added to the seen list as an representative for someone else,
                    // so make sure we include the authorizedRepresentatives they have for this practice
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

        _logger.LogInformation("Completed building list of {PrincipalCount} principals and {AssociateCount} associates, writing total {SeenPersonsCount} entries to store ...", principalCount, seenPersons.Count-principalCount, seenPersons.Count);

        // Concurrently update the list in Redis while we create a zipped dump of the entire thing
        var updateIndividualEntriesTask = UpdateCacheEntries(seenPersons);
        var updateBulkEntryTask = UpdateBulkEntry(registry);
        var cleanEntriesTask = CleanRemovedEntries(registry);

        await Task.WhenAll(updateIndividualEntriesTask, updateBulkEntryTask, cleanEntriesTask);
        
        _logger.LogInformation("Completed writing persons and bulk entry");
    }

    private async Task UpdateBulkEntry(List<PersonInternal> registry)
    {
        using var _ = _logger.Timer("es-trad-update-cache-bulk");
        
        await CacheZippedRegistry(
            registry, 
            ApplicationSettings.RedisBulkEntryKey,
            Helpers.MapInternalPersonToZipBulk);

        await CacheZippedRegistry(
            registry, 
            ApplicationSettings.RedisBulkEntryPrivateKey,
            Helpers.MapInternalPersonToZipBulkPrivate);
    }

    private async Task UpdateCacheEntries(Dictionary<string, PersonInternal> persons)
    {
        using var _ = _logger.Timer("es-trad-update-cache-individuals");
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

        await cache.SetAsync(key, Encoding.UTF8.GetBytes(entry), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2)
        });
    }

    private async Task CacheZippedRegistry<T>(List<PersonInternal> registry, string cacheKey, Func<PersonInternal, T> mapper)
    {
        var mappedRegistry = registry
            .Where(r => r is not null)
            .Select(mapper)
            .ToList();

        using var zipContent = new MemoryStream();
        using (var archive = new ZipArchive(zipContent, ZipArchiveMode.Create)) {
            var archiveEntry = archive.CreateEntry(ApplicationSettings.ZipEntryFileName);
            var jsonSerializer = new JsonSerializer();
            await using (var sw = new StreamWriter(archiveEntry.Open()))
            {
                jsonSerializer.Serialize(sw, mappedRegistry);            
            }
        }    
        var db = connectionMultiplexer.GetDatabase();
        await db.StringSetAsync(cacheKey, zipContent.ToArray(), TimeSpan.FromHours(4));
    }
    
    private async Task CleanRemovedEntries(List<PersonInternal> registry)
    {
        var newSsns = registry
            .Where(p => p.Ssn != null)
            .Select(p => p.Ssn)
            .Distinct()
            .ToList();

        var cachedResults = await cache.GetAsync(ApplicationSettings.RedisSsnListKey);
        // Nothing cached; nothing to clean
        if (cachedResults is null)
        {
            await UpdateSsnList(newSsns);
            return;
        }
        var cachedSsns = JsonConvert.DeserializeObject<List<string>>(Encoding.UTF8.GetString(cachedResults));

        var removedSsns = cachedSsns.Except(newSsns).ToList();
        if (removedSsns.Count == 0)
        {
            await UpdateSsnList(newSsns);
            return;
        }
        
        var throttler = new SemaphoreSlim(30);

        var tasks = removedSsns.Select(async ssn =>
        {
            await throttler.WaitAsync();
            try
            {
                var key = Helpers.GetCacheKeyForSsn(ssn);
                await cache.RemoveAsync(key);
            }
            finally
            {
                throttler.Release();
            }
        });
        await Task.WhenAll(tasks);
        await UpdateSsnList(newSsns);
    }

    private async Task UpdateSsnList(List<string> newSsns)
    {
        var entry = JsonConvert.SerializeObject(newSsns);
        await cache.SetAsync(ApplicationSettings.RedisSsnListKey, Encoding.UTF8.GetBytes(entry), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2)
        });
    }
}