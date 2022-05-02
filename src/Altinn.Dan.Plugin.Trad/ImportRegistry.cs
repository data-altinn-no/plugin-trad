using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Altinn.Dan.Plugin.Trad.Config;
using Altinn.Dan.Plugin.Trad.Models;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nadobe.Common.Exceptions;
using Nadobe.Common.Models;
using Nadobe.Common.Util;
using Newtonsoft.Json;

namespace Altinn.Dan.Plugin.Trad
{
    public class ImportRegistry
    {
        private readonly ILogger _logger;
        private EvidenceSourceMetadata _metadata;
        private ApplicationSettings _settings;
        private HttpClient _client;
        private IDistributedCache _cache;
        private SecretClient _secretClient;

        public ImportRegistry(ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory, IOptions<ApplicationSettings> settings, IDistributedCache cache)
        {
            _client = httpClientFactory.CreateClient("SafeHttpClient");
            _logger = loggerFactory.CreateLogger<ImportRegistry>();
            _settings = settings.Value;
            _metadata = new EvidenceSourceMetadata(settings);
            _cache = cache;
            _secretClient = new SecretClient(
              new Uri($"https://{_settings.KeyVaultName}.vault.azure.net/"),
              new DefaultAzureCredential());

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
            using (var t = _logger.Timer("es-trad-fetch-people"))
            {
                _logger.LogDebug($"Attempting fetch from {_settings.RegistryURL}");
                registry = await GetPeople();
                _logger.LogDebug($"Fetched {registry.Count} entries");
            }

            using (var t = _logger.Timer("es-trad-update-cache"))
            {
                _logger.LogDebug($"Updating cache with {registry.Count} entries");
                await UpdateCache(registry);
                _logger.LogDebug($"Done updating cache");
            }

            _logger.LogInformation($"Import completed, now has {registry.Count} entries. Next scheduled import at: {myTimer.ScheduleStatus.Next}");
        }

        private async Task<List<Person>> GetPeople()
        {
            HttpResponseMessage result;
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, _settings.RegistryURL);
                var apiKeySecret = await _secretClient.GetSecretAsync(_settings.ApiKeySecret);
                request.Headers.Add("ApiKey", apiKeySecret.Value.Value);
                result = await _client.SendAsync(request);
            }
            catch (HttpRequestException ex)
            {
                throw new EvidenceSourcePermanentServerException(EvidenceSourceMetadata.ERROR_CCR_UPSTREAM_ERROR, null, ex);
            }

            if (!result.IsSuccessStatusCode)
            {
                _logger.LogError("Unable to fetch persons from TRAD, statuscode: {code} reasonphrase: {reason}", result.StatusCode.ToString(), result.ReasonPhrase);
                throw new EvidenceSourcePermanentClientException(EvidenceSourceMetadata.ERROR_CCR_UPSTREAM_ERROR, "Unable to fetch persons from TRAD");
            }

            try
            {
                var response = JsonConvert.DeserializeObject<List<Person>>(await result.Content.ReadAsStringAsync());
                return response;
            }
            catch (Exception e) {
                _logger.LogError("Unable to decode response from TRAD. {exception}: {message}", e.GetType().Name, e.Message);
                throw new EvidenceSourcePermanentServerException(EvidenceSourceMetadata.ERROR_CCR_UPSTREAM_ERROR,
                    "Did not understand the data model returned from upstream source");
            }
        }

        private async Task UpdateCache(List<Person> registry)
        {
            foreach(Person p in registry)
            {
                if (p.ssn == null)
                {
                    continue;
                }

                if (p.ssn.Length == 10)
                {
                    p.ssn = "0" + p.ssn;
                }

                var key = Helpers.GetCacheKeyForSsn(p.ssn);
                p.ssn = Helpers.MaskSsn(p.ssn);
                var entry = JsonConvert.SerializeObject(p);
                await _cache.SetAsync(key, Encoding.UTF8.GetBytes(entry), new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = new DateTimeOffset(DateTime.UtcNow.AddHours(1), TimeSpan.Zero)
                });
            }
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
