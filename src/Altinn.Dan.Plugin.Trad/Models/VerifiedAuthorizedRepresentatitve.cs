using Newtonsoft.Json;

namespace Altinn.Dan.Plugin.Trad.Models;

public class VerifiedAuthorizedRepresentatitve
{
    [JsonProperty("fornavn", NullValueHandling = NullValueHandling.Ignore)]
    public string FirstName { get; set; }
    
    [JsonProperty("etternavn", NullValueHandling = NullValueHandling.Ignore)]
    public string LastName { get; set; }
}