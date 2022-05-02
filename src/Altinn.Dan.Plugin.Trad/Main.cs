using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Altinn.Dan.Plugin.Trad.Config;
using Altinn.Dan.Plugin.Trad.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nadobe;
using Nadobe.Common.Models;
using Nadobe.Common.Util;
using Newtonsoft.Json;

namespace Altinn.Dan.Plugin.Trad
{
    public class Main
    {
        private ILogger _logger;
        private readonly EvidenceSourceMetadata _metadata;
        private readonly IDistributedCache _cache;

        public Main(IOptions<ApplicationSettings> settings, IDistributedCache cache)
        {
            _metadata = new EvidenceSourceMetadata(settings);
            _cache = cache;
        }

        [Function("VerifiserAdvokat")]
        public async Task<HttpResponseData> RunAsyncVerifiserAdvokat([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req, FunctionContext context)
        {
            _logger = context.GetLogger(context.FunctionDefinition.Name);

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var evidenceHarvesterRequest = JsonConvert.DeserializeObject<EvidenceHarvesterRequest>(requestBody);

            var response = req.CreateResponse(HttpStatusCode.OK);
            var actionResult = await EvidenceSourceResponse.CreateResponse(null, () => GetEvidenceValuesVerifiserAdvokat(evidenceHarvesterRequest)) as ObjectResult;

            await response.WriteAsJsonAsync(actionResult?.Value);

            return response;
        }

        [Function("HentPerson")]
        public async Task<HttpResponseData> RunAsyncHentPerson([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req, FunctionContext context)
        {
            _logger = context.GetLogger(context.FunctionDefinition.Name);

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var evidenceHarvesterRequest = JsonConvert.DeserializeObject<EvidenceHarvesterRequest>(requestBody);

            var response = req.CreateResponse(HttpStatusCode.OK);
            var actionResult = await EvidenceSourceResponse.CreateResponse(null, () => GetEvidenceValuesHentPerson(evidenceHarvesterRequest)) as ObjectResult;

            await response.WriteAsJsonAsync(actionResult?.Value);

            return response;
        }


        [Function(Constants.EvidenceSourceMetadataFunctionName)]
        public async Task<HttpResponseData> RunAsyncMetadata(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequestData req,
            FunctionContext context)
        {
            _logger = context.GetLogger(context.FunctionDefinition.Name);
            _logger.LogInformation($"Running metadata for {Constants.EvidenceSourceMetadataFunctionName}");
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(_metadata.GetEvidenceCodes());
            return response;
        }

        private async Task<List<EvidenceValue>> GetEvidenceValuesVerifiserAdvokat(EvidenceHarvesterRequest evidenceHarvesterRequest)
        {
            var res = await _cache.GetAsync(Helpers.GetCacheKeyForSsn(evidenceHarvesterRequest.SubjectParty.NorwegianSocialSecurityNumber));

            var ecb = new EvidenceBuilder(new Metadata(), "VerifiserAdvokat");
            ecb.AddEvidenceValue("Fodselsnummer", evidenceHarvesterRequest.SubjectParty.NorwegianSocialSecurityNumber, EvidenceSourceMetadata.SOURCE);
            if (res != null)
            {
                Person person = JsonConvert.DeserializeObject<Person>(Encoding.UTF8.GetString(res));

                ecb.AddEvidenceValue("ErRegistrert", true, EvidenceSourceMetadata.SOURCE);
                ecb.AddEvidenceValue("Tittel", person.TitleType, EvidenceSourceMetadata.SOURCE);
            }
            else
            {
                ecb.AddEvidenceValue("ErRegistrert", false, EvidenceSourceMetadata.SOURCE);
            }

            return ecb.GetEvidenceValues();
        }

        private async Task<List<EvidenceValue>> GetEvidenceValuesHentPerson(EvidenceHarvesterRequest evidenceHarvesterRequest)
        {
            var res = await _cache.GetAsync(Helpers.GetCacheKeyForSsn(evidenceHarvesterRequest.SubjectParty.NorwegianSocialSecurityNumber));

            var ecb = new EvidenceBuilder(new Metadata(), "HentPerson");
            
            if (res != null)
            {
                ecb.AddEvidenceValue("default", Encoding.UTF8.GetString(res), EvidenceSourceMetadata.SOURCE);
            }
            else
            {
                ecb.AddEvidenceValue("default", "{}", EvidenceSourceMetadata.SOURCE);
            }

            return ecb.GetEvidenceValues();
        }
    }
}
