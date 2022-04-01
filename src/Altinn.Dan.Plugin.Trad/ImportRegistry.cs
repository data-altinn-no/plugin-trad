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

        public ImportRegistry(ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory, IOptions<ApplicationSettings> settings, IDistributedCache cache)
        {
            _client = httpClientFactory.CreateClient("SafeHttpClient");
            _logger = loggerFactory.CreateLogger<ImportRegistry>();
            _settings = settings.Value;
            _metadata = new EvidenceSourceMetadata(settings);
            _cache = cache;
        }

        [Function("ImportRegistry")]
        public async Task RunAsync([TimerTrigger("0 */5 * * *")] MyInfo myTimer)
        {
            if(myTimer.IsPastDue)
            {
                _logger.LogInformation($"Registry import was not run on schedule");
            }

            _logger.LogInformation($"Registry Import executed at: {DateTime.Now}");
            _logger.LogInformation($"Next scheduled import at: {myTimer.ScheduleStatus.Next}");

            var registry = await GetPeople();
            UpdateCache(registry);
        }

        private async Task<List<Person>> GetPeople()
        {
            HttpResponseMessage result = null;
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, _settings.RegistryURL);
                result = await _client.SendAsync(request);
            }
            catch (HttpRequestException ex)
            {
                throw new EvidenceSourcePermanentServerException(EvidenceSourceMetadata.ERROR_CCR_UPSTREAM_ERROR, null, ex);
            }

            if (result.StatusCode == HttpStatusCode.NotFound)
            {
                throw new EvidenceSourcePermanentClientException(EvidenceSourceMetadata.ERROR_CCR_UPSTREAM_ERROR, $"Registry data could not be found");
            }

            var response = JsonConvert.DeserializeObject<List<Person>>(await result.Content.ReadAsStringAsync());
            if (response == null)
            {
                throw new EvidenceSourcePermanentServerException(EvidenceSourceMetadata.ERROR_CCR_UPSTREAM_ERROR,
                    "Did not understand the data model returned from upstream source");
            }

            return response;
        }

        private void UpdateCache(List<Person> registry)
        {
            var sha = SHA256.Create();
            foreach(Person p in registry)
            {
                var entry = JsonConvert.SerializeObject(p);

                var plainTextBytes = Encoding.UTF8.GetBytes("tr-registry-" + p.ssn);
                var key = Convert.ToBase64String(plainTextBytes);

                _cache.Set(key, Encoding.UTF8.GetBytes(entry), null);
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
