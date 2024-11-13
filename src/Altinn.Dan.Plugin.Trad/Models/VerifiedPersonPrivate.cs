using System.Collections.Generic;
using Newtonsoft.Json;

namespace Altinn.Dan.Plugin.Trad.Models;

public class VerifiedPersonPrivate
{
    [JsonProperty("verifisert")]
    public bool Verified { get; set; }
    
    [JsonProperty("tittel", NullValueHandling = NullValueHandling.Ignore)]
    public TitleTypeExternal? Title { get; set; }
    
    [JsonProperty("fornavn", NullValueHandling = NullValueHandling.Ignore)]
    public string FirstName { get; set; }
    
    [JsonProperty("etternavn", NullValueHandling = NullValueHandling.Ignore)]
    public string LastName { get; set; }
    
    [JsonProperty("fullmektige", NullValueHandling = NullValueHandling.Ignore)]
    public List<VerifiedAuthorizedRepresentatitve> AuthorizedRepresentatives { get; set; }
    
    [JsonProperty("firmatilknytning", NullValueHandling = NullValueHandling.Ignore)]
    public List<int> OrganizationNumbers { get; set; }
}