using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Altinn.Dan.Plugin.Trad.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Caching.Distributed;
using Nadobe;
using Nadobe.Common.Interfaces;
using Nadobe.Common.Models;
using Nadobe.Common.Util;
using Newtonsoft.Json;

namespace Altinn.Dan.Plugin.Trad
{
    public class Main
    {
        private readonly IEvidenceSourceMetadata _metadata;
        private readonly IDistributedCache _cache;

        public Main(IDistributedCache cache, IEvidenceSourceMetadata metadata)
        {
            _metadata = metadata;
            _cache = cache;
        }

        [Function("AdvRegPersonVerifikasjon")]
        public async Task<HttpResponseData> RunAsyncVerifiserAdvokat([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req, FunctionContext context)
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var evidenceHarvesterRequest = JsonConvert.DeserializeObject<EvidenceHarvesterRequest>(requestBody);

            var response = req.CreateResponse(HttpStatusCode.OK);
            var actionResult = await EvidenceSourceResponse.CreateResponse(null, () => GetEvidenceValuesVerifiserAdvokat(evidenceHarvesterRequest)) as ObjectResult;

            await response.WriteAsJsonAsync(actionResult?.Value);

            return response;
        }

        [Function("AdvRegPerson")]
        public async Task<HttpResponseData> RunAsyncHentPerson([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req, FunctionContext context)
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var evidenceHarvesterRequest = JsonConvert.DeserializeObject<EvidenceHarvesterRequest>(requestBody);

            var response = req.CreateResponse(HttpStatusCode.OK);
            var actionResult = await EvidenceSourceResponse.CreateResponse(null, () => GetEvidenceValuesHentAdvokatRegisterPerson(evidenceHarvesterRequest)) as ObjectResult;

            await response.WriteAsJsonAsync(actionResult?.Value);

            return response;
        }


        [Function(Constants.EvidenceSourceMetadataFunctionName)]
        public async Task<HttpResponseData> RunAsyncMetadata(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequestData req,
            FunctionContext context)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(_metadata.GetEvidenceCodes());
            return response;
        }

        private async Task<List<EvidenceValue>> GetEvidenceValuesVerifiserAdvokat(EvidenceHarvesterRequest evidenceHarvesterRequest)
        {
            var res = await _cache.GetAsync(Helpers.GetCacheKeyForSsn(evidenceHarvesterRequest.SubjectParty.NorwegianSocialSecurityNumber));

            var ecb = new EvidenceBuilder(_metadata, "AdvRegPersonVerifikasjon");
            ecb.AddEvidenceValue("Fodselsnummer", evidenceHarvesterRequest.SubjectParty.NorwegianSocialSecurityNumber, EvidenceSourceMetadata.Source);
            if (res != null)
            {
                var person = JsonConvert.DeserializeObject<PersonInternal>(Encoding.UTF8.GetString(res));

                var includePersonsWithoutAuditedBusinessRelation =
                    evidenceHarvesterRequest.Parameters != null &&
                    evidenceHarvesterRequest.Parameters.Any() &&
                    (bool)evidenceHarvesterRequest.Parameters.First().Value;

                if ((person.IsAssociatedWithAuditedBusiness ?? true) || includePersonsWithoutAuditedBusinessRelation)
                {
                    ecb.AddEvidenceValue("Verifisert", true, EvidenceSourceMetadata.Source);
                    ecb.AddEvidenceValue("ErTilknyttetVirksomhetMedRevisjonsPlikt", person.IsAssociatedWithAuditedBusiness ?? true);
                    ecb.AddEvidenceValue("Tittel", person.Title, EvidenceSourceMetadata.Source);
                    return ecb.GetEvidenceValues();
                }

            }

            ecb.AddEvidenceValue("Verifisert", false, EvidenceSourceMetadata.Source);
            return ecb.GetEvidenceValues();
        }

        private async Task<List<EvidenceValue>> GetEvidenceValuesHentAdvokatRegisterPerson(EvidenceHarvesterRequest evidenceHarvesterRequest)
        {
            var ecb = new EvidenceBuilder(_metadata, "AdvRegPerson");

            var res = await _cache.GetAsync(Helpers.GetCacheKeyForSsn(evidenceHarvesterRequest.SubjectParty.NorwegianSocialSecurityNumber));
            if (res != null)
            {
                var personInternal = JsonConvert.DeserializeObject<PersonInternal>(Encoding.UTF8.GetString(res));
                var person = Helpers.MapInternalModelToExternal(personInternal);
                ecb.AddEvidenceValue("default", JsonConvert.SerializeObject(person), EvidenceSourceMetadata.Source);
            }
            else
            {
                ecb.AddEvidenceValue("default", "{}", EvidenceSourceMetadata.Source);
            }

            return ecb.GetEvidenceValues();
        }
    }
}
