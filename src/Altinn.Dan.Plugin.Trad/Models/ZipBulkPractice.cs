using System.Collections.Generic;
using Newtonsoft.Json;

namespace Altinn.Dan.Plugin.Trad.Models;

public record ZipBulkPractice
{
    [JsonProperty("firmanr", NullValueHandling = NullValueHandling.Ignore)]
    public string CompanyNumber { get; set; }

    [JsonProperty("orgNumber")]
    public int OrganizationNumber { get; set; }

    [JsonProperty("subOrgNumber", NullValueHandling = NullValueHandling.Ignore)]
    public int? SubOrganizationNumber { get; set; }

    [JsonProperty("authorizedRepresentatives", NullValueHandling = NullValueHandling.Ignore)]
    public List<ZipBulkPerson> AuthorizedRepresentatives;

    [JsonProperty("isAuthorizedRepresentativeFor", NullValueHandling = NullValueHandling.Ignore)]
    public List<ZipBulkPerson> IsAnAuthorizedRepresentativeFor;
        
    [JsonProperty("hovedpraksis", NullValueHandling = NullValueHandling.Ignore)]
    public bool? MainPractice;
    
    [JsonProperty("orgNavn", NullValueHandling = NullValueHandling.Ignore)]
    public string OrganizationName { get; set; }
    
    [JsonProperty("subOrgNavn", NullValueHandling = NullValueHandling.Ignore)]
    public string SubOrganizationName { get; set; }
}