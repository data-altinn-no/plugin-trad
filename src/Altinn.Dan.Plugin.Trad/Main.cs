using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Altinn.Dan.Plugin.Trad.Config;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nadobe;
using Nadobe.Common.Exceptions;
using Nadobe.Common.Models;
using Nadobe.Common.Util;
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

        [Function("Person")]
        public async Task<HttpResponseData> Person(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequestData req,
            FunctionContext context)
        {
            _logger = context.GetLogger(context.FunctionDefinition.Name);
            _logger.LogInformation("Running func 'Person'");
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var evidenceHarvesterRequest = JsonConvert.DeserializeObject<EvidenceHarvesterRequest>(requestBody);

            var actionResult = await EvidenceSourceResponse.CreateResponse(null, () => GetEvidenceValuesPerson(evidenceHarvesterRequest)) as ObjectResult;
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(actionResult?.Value);
            return response;
        }

        private async Task<List<EvidenceValue>> GetEvidenceValuesPerson(EvidenceHarvesterRequest evidenceHarvesterRequest)
        {
            dynamic content = await MakeRequest(string.Format(_settings.PersonURL, evidenceHarvesterRequest.OrganizationNumber), evidenceHarvesterRequest.OrganizationNumber);

            var ecb = new EvidenceBuilder(_metadata, "Person");
            ecb.AddEvidenceValue($"firstName", content.firstName, EvidenceSourceMetadata.SOURCE);
            ecb.AddEvidenceValue($"lastName", content.lastName, EvidenceSourceMetadata.SOURCE);
            ecb.AddEvidenceValue($"company", content.company, EvidenceSourceMetadata.SOURCE);
            ecb.AddEvidenceValue($"county", content.county, EvidenceSourceMetadata.SOURCE);
            ecb.AddEvidenceValue($"city", content.city, EvidenceSourceMetadata.SOURCE);
            ecb.AddEvidenceValue($"cityCode", content.cityCode, EvidenceSourceMetadata.SOURCE);

            return ecb.GetEvidenceValues();
        }

        private async Task<dynamic> MakeRequest(string target, string organizationNumber)
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

        [Function("Company")]
        public async Task<HttpResponseData> Company(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestData req,
            FunctionContext context)
        {
            _logger = context.GetLogger(context.FunctionDefinition.Name);
            _logger.LogInformation("Running func 'Company'");
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var evidenceHarvesterRequest = JsonConvert.DeserializeObject<EvidenceHarvesterRequest>(requestBody);

            var actionResult = await EvidenceSourceResponse.CreateResponse(null, () => GetEvidenceValuesDatasetName2(evidenceHarvesterRequest)) as ObjectResult;
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(actionResult?.Value);
            return response;
        }

        private async Task<List<EvidenceValue>> GetEvidenceValuesDatasetName2(EvidenceHarvesterRequest evidenceHarvesterRequest)
        {
            dynamic content = await MakeRequest(string.Format(_settings.CompanyURL, evidenceHarvesterRequest.OrganizationNumber), evidenceHarvesterRequest.OrganizationNumber);

            var ecb = new EvidenceBuilder(_metadata, "Company");
            ecb.AddEvidenceValue($"Tlf", content.Tlf, EvidenceSourceMetadata.SOURCE);
            ecb.AddEvidenceValue($"TeleFax", content.TeleFax, EvidenceSourceMetadata.SOURCE);
            ecb.AddEvidenceValue($"personList", content.personList, EvidenceSourceMetadata.SOURCE);

            return ecb.GetEvidenceValues();
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
