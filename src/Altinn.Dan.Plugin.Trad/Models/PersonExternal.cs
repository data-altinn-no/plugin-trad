using System.Collections.Generic;
using Newtonsoft.Json;

namespace Altinn.Dan.Plugin.Trad.Models
{
    public record PersonExternal
    {
        [JsonProperty("regnr", NullValueHandling = NullValueHandling.Ignore)]
        public string RegistrationNumber { get; set; }
        
        [JsonProperty("fodselsnummer")]
        public string Ssn { get; set; }
        
        [JsonProperty("tittel")]
        public TitleTypeExternal Title { get; set; }

        [JsonProperty("tilknyttedePraksiser", NullValueHandling = NullValueHandling.Ignore)]
        public List<PracticeExternal> Practices { get; set; }
    }
}
