using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Altinn.Dan.Plugin.Trad.Config;
using Altinn.Dan.Plugin.Trad.Models;
using Dan.Common;
using Dan.Common.Interfaces;
using Dan.Common.Models;
using Dan.Common.Util;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Altinn.Dan.Plugin.Trad;

public class Main
{
    private readonly IEvidenceSourceMetadata _metadata;
    private readonly IDistributedCache _cache;
    private readonly IConnectionMultiplexer _redis;

    public Main(IDistributedCache cache, IEvidenceSourceMetadata metadata, IConnectionMultiplexer connectionMultiplexer)
    {
        _metadata = metadata;
        _cache = cache;
        _redis = connectionMultiplexer;
    }

    [Function("AdvRegPersonVerifikasjon")]
    public async Task<HttpResponseData> RunAsyncVerifiserAdvokat([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req, FunctionContext context)
    {
        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var evidenceHarvesterRequest = JsonConvert.DeserializeObject<EvidenceHarvesterRequest>(requestBody);

        return await EvidenceSourceResponse.CreateResponse(req, () => GetEvidenceValuesVerifiserAdvokat(evidenceHarvesterRequest));
    }

    [Function("AdvokatverifikasjonPrivat")]
    public async Task<HttpResponseData> RunAsyncAdvokatverifikasjonPrivat(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req, 
        FunctionContext context)
    {
        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var evidenceHarvesterRequest = JsonConvert.DeserializeObject<EvidenceHarvesterRequest>(requestBody);
        return await EvidenceSourceResponse.CreateResponse(req, () => GetEvidenceValuesVerifiserAdvokatPrivat(evidenceHarvesterRequest));
    }

    [Function("AdvRegPerson")]
    public async Task<HttpResponseData> RunAsyncHentPerson([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req, FunctionContext context)
    {
        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var evidenceHarvesterRequest = JsonConvert.DeserializeObject<EvidenceHarvesterRequest>(requestBody);

        return await EvidenceSourceResponse.CreateResponse(req, () => GetEvidenceValuesHentAdvokatRegisterPerson(evidenceHarvesterRequest));
    }
    
    [Function("AdvRegPersonPrivat")]
    public async Task<HttpResponseData> RunAsyncHentPersonPrivat([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req, FunctionContext context)
    {
        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var evidenceHarvesterRequest = JsonConvert.DeserializeObject<EvidenceHarvesterRequest>(requestBody);

        return await EvidenceSourceResponse.CreateResponse(req, () => GetEvidenceValuesHentAdvokatRegisterPersonPrivate(evidenceHarvesterRequest));
    }
        
    [Function("AdvRegBulkzip")]
    public async Task<HttpResponseData> RunAsyncBulk([HttpTrigger(AuthorizationLevel.Function, "post", Route = "AdvRegBulk.zip")] HttpRequestData req, FunctionContext context)
    {
        Stream bulkStream = await GetZippedRegistryAsBulk();
        var response = req.CreateResponse(HttpStatusCode.OK);
        await bulkStream.CopyToAsync(response.Body);
        return response;
    }
    
    [Function("AdvRegBulkPrivatzip")]
    public async Task<HttpResponseData> RunAsyncBulkPrivate([HttpTrigger(AuthorizationLevel.Function, "post", Route = "AdvRegBulkPrivat.zip")] HttpRequestData req, FunctionContext context)
    {
        Stream bulkStream = await GetPrivateZippedRegistryAsBulk();
        var response = req.CreateResponse(HttpStatusCode.OK);
        await bulkStream.CopyToAsync(response.Body);
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
        var res = await _cache.GetAsync(Helpers.GetCacheKeyForSsn(evidenceHarvesterRequest.SubjectParty!.NorwegianSocialSecurityNumber));

        var ecb = new EvidenceBuilder(_metadata, "AdvRegPersonVerifikasjon");
        ecb.AddEvidenceValue("fodselsnummer", evidenceHarvesterRequest.SubjectParty.NorwegianSocialSecurityNumber, EvidenceSourceMetadata.Source);
        if (res != null)
        {
            var person = JsonConvert.DeserializeObject<PersonInternal>(Encoding.UTF8.GetString(res));
            ecb.AddEvidenceValue("verifisert", true, EvidenceSourceMetadata.Source);
            ecb.AddEvidenceValue("tittel", person.Title, EvidenceSourceMetadata.Source);
            return ecb.GetEvidenceValues();
        }

        ecb.AddEvidenceValue("verifisert", false, EvidenceSourceMetadata.Source);
        return ecb.GetEvidenceValues();
    }

    private async Task<List<EvidenceValue>> GetEvidenceValuesVerifiserAdvokatPrivat(
        EvidenceHarvesterRequest evidenceHarvesterRequest)
    {
        var res = await _cache.GetAsync(Helpers.GetCacheKeyForSsn(evidenceHarvesterRequest.SubjectParty!.NorwegianSocialSecurityNumber));

        var ecb = new EvidenceBuilder(_metadata, "AdvokatverifikasjonPrivat");
        if (res != null)
        {
            var person = JsonConvert.DeserializeObject<PersonInternal>(Encoding.UTF8.GetString(res));
            ecb.AddEvidenceValue("verifisert", true, EvidenceSourceMetadata.Source);
            ecb.AddEvidenceValue("tittel", person.Title, EvidenceSourceMetadata.Source);
            ecb.AddEvidenceValue("fornavn", person.Firstname, EvidenceSourceMetadata.Source);
            ecb.AddEvidenceValue("mellomnavn", person.MiddleName, EvidenceSourceMetadata.Source);
            ecb.AddEvidenceValue("etternavn", person.LastName, EvidenceSourceMetadata.Source);
            return ecb.GetEvidenceValues();
        }
        
        ecb.AddEvidenceValue("verifisert", false, EvidenceSourceMetadata.Source);
        return ecb.GetEvidenceValues();
    }

    private async Task<List<EvidenceValue>> GetEvidenceValuesHentAdvokatRegisterPerson(EvidenceHarvesterRequest evidenceHarvesterRequest)
    {
        var ecb = new EvidenceBuilder(_metadata, "AdvRegPerson");

        var res = await _cache.GetAsync(Helpers.GetCacheKeyForSsn(evidenceHarvesterRequest.SubjectParty!.NorwegianSocialSecurityNumber));
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
    
    private async Task<List<EvidenceValue>> GetEvidenceValuesHentAdvokatRegisterPersonPrivate(EvidenceHarvesterRequest evidenceHarvesterRequest)
    {
        var ecb = new EvidenceBuilder(_metadata, "AdvRegPerson");

        var res = await _cache.GetAsync(Helpers.GetCacheKeyForSsn(evidenceHarvesterRequest.SubjectParty!.NorwegianSocialSecurityNumber));
        if (res != null)
        {
            var personInternal = JsonConvert.DeserializeObject<PersonInternal>(Encoding.UTF8.GetString(res));
            var person = Helpers.MapInternalModelToPrivate(personInternal);
            ecb.AddEvidenceValue("default", JsonConvert.SerializeObject(person), EvidenceSourceMetadata.Source);
        }
        else
        {
            ecb.AddEvidenceValue("default", "{}", EvidenceSourceMetadata.Source);
        }

        return ecb.GetEvidenceValues();
    }
        
    private async Task<Stream> GetZippedRegistryAsBulk()
    {
        var db = _redis.GetDatabase();
        byte[] bytes = await db.StringGetAsync(ApplicationSettings.RedisBulkEntryKey);
        return new MemoryStream(bytes);
    }
    
    private async Task<Stream> GetPrivateZippedRegistryAsBulk()
    {
        var db = _redis.GetDatabase();
        byte[] bytes = await db.StringGetAsync(ApplicationSettings.RedisBulkEntryPrivateKey);
        return new MemoryStream(bytes);
    }
}