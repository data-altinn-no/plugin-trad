using System.Collections.Generic;
using Newtonsoft.Json;

namespace Altinn.Dan.Plugin.Trad.Models
{
    public record PersonExternal
    {
        [JsonProperty("fodselsnummer")]
        public string Ssn { get; set; }
        
        [JsonProperty("tittel")]
        public TitleType Title { get; set; }

        [JsonProperty("erTilknyttetVirksomhetMedRevisjonsplikt", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsAssociatedWithAuditedBusiness;

        [JsonProperty("tilknyttedePraksiser", NullValueHandling = NullValueHandling.Ignore)]
        public List<PracticeExternal> Practices { get; set; }
    }
}
