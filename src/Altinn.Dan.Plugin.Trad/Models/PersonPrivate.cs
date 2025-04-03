using System.Collections.Generic;
using Newtonsoft.Json;

namespace Altinn.Dan.Plugin.Trad.Models;

public record PersonPrivate
{
    [JsonProperty("tittel")]
    public TitleTypeExternal? Title { get; set; }
    
    [JsonProperty("fornavn")]
    public string FirstName { get; set; }
    
    [JsonProperty("mellomnavn")]
    public string MiddleName { get; set; }
    
    [JsonProperty("etternavn")]
    public string LastName { get; set; }
    
    [JsonProperty("tilknyttedePraksiser", NullValueHandling = NullValueHandling.Ignore)]
    public List<PracticePrivate> Practices { get; set; }
}