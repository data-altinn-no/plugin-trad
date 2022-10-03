using System.Collections.Generic;
using Newtonsoft.Json;

namespace Altinn.Dan.Plugin.Trad.Models
{
    public record PersonInternal
    {
        [JsonProperty("ssn")]
        public string Ssn { get; set; }
        
        [JsonProperty("title")]
        public TitleTypeInternal Title { get; set; }

        [JsonProperty("practices", NullValueHandling = NullValueHandling.Ignore)]
        public List<PracticeInternal> Practices { get; set; }
    }
}
