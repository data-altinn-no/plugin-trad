using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Altinn.Dan.Plugin.Trad.Config;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nadobe;
using Nadobe.Common.Exceptions;
using Newtonsoft.Json;

namespace Altinn.Dan.Plugin.Trad
{
    public class Main
    {
        private ILogger _logger;
        private HttpClient _client;
        private ApplicationSettings _settings;
        private EvidenceSourceMetadata _metadata;

        public Main(IHttpClientFactory httpClientFactory, IOptions<ApplicationSettings> settings)
        {
            _client = httpClientFactory.CreateClient("SafeHttpClient");
            _settings = settings.Value;
            _metadata = new EvidenceSourceMetadata(settings);
        }

        internal async Task<dynamic> MakeRequest(string target, string organizationNumber)
        {
            HttpResponseMessage result = null;
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, target);
                result = await _client.SendAsync(request);
            }
            catch (HttpRequestException ex)
            {
                throw new EvidenceSourcePermanentServerException(EvidenceSourceMetadata.ERROR_CCR_UPSTREAM_ERROR, null, ex);
            }

            if (result.StatusCode == HttpStatusCode.NotFound)
            {
                throw new EvidenceSourcePermanentClientException(EvidenceSourceMetadata.ERROR_ORGANIZATION_NOT_FOUND, $"{organizationNumber} could not be found");
            }

            var response = JsonConvert.DeserializeObject(await result.Content.ReadAsStringAsync());
            if (response == null)
            {
                throw new EvidenceSourcePermanentServerException(EvidenceSourceMetadata.ERROR_CCR_UPSTREAM_ERROR,
                    "Did not understand the data model returned from upstream source");
            }

            return response;
        }

        [Function(Constants.EvidenceSourceMetadataFunctionName)]
        public async Task<HttpResponseData> Metadata(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequestData req,
            FunctionContext context)
        {
            _logger = context.GetLogger(context.FunctionDefinition.Name);
            _logger.LogInformation($"Running metadata for {Constants.EvidenceSourceMetadataFunctionName}");
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(_metadata.GetEvidenceCodes());
            return response;
        }
    }
}
