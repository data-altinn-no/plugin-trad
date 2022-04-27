using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Altinn.Dan.Plugin.Trad.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Nadobe.Common.Models;
using Nadobe.Common.Util;
using Newtonsoft.Json;

namespace Altinn.Dan.Plugin.Trad
{
    public class VerifiserAdvokat
    {
        private ILogger _logger;
        private readonly IDistributedCache _cache;

        public VerifiserAdvokat(IDistributedCache cache)
        {
            _cache = cache;
        }

        [Function("VerifiserAdvokat")]
        public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req, FunctionContext context)
        {
            _logger = context.GetLogger(context.FunctionDefinition.Name);
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var evidenceHarvesterRequest = JsonConvert.DeserializeObject<EvidenceHarvesterRequest>(requestBody);

            var response = req.CreateResponse(HttpStatusCode.OK);
            var actionResult = await EvidenceSourceResponse.CreateResponse(null, () => GetEvidenceValuesVerifiserAdvokat(evidenceHarvesterRequest)) as ObjectResult;

            await response.WriteAsJsonAsync(actionResult?.Value);

            return response;
        }

        private async Task<List<EvidenceValue>> GetEvidenceValuesVerifiserAdvokat(EvidenceHarvesterRequest evidenceHarvesterRequest)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes("tr-registry-" + evidenceHarvesterRequest.OrganizationNumber);
            var key = Convert.ToBase64String(plainTextBytes);

            var res = await _cache.GetAsync(key);
            Person person = JsonConvert.DeserializeObject<Person>(res.ToString());

            var ecb = new EvidenceBuilder(new Metadata(), "VerifiserAdvokat");
            ecb.AddEvidenceValue("Personnummer", evidenceHarvesterRequest.OrganizationNumber, EvidenceSourceMetadata.SOURCE);
            if(res != null)
            {
                ecb.AddEvidenceValue("ErRegistrert", true, EvidenceSourceMetadata.SOURCE);
                ecb.AddEvidenceValue("Tittel", person.titleType, EvidenceSourceMetadata.SOURCE);
            }
            else
            {
                ecb.AddEvidenceValue("ErRegistrert", false, EvidenceSourceMetadata.SOURCE);
            }
            
            return ecb.GetEvidenceValues();
        }
    }
}
