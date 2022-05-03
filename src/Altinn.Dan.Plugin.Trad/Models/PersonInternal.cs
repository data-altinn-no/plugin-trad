using System.Collections.Generic;
using Newtonsoft.Json;

namespace Altinn.Dan.Plugin.Trad.Models
{
    public record PersonInternal
    {
        [JsonProperty("ssn")]
        public string Ssn { get; set; }
        
        [JsonProperty("title")]
        public TitleType Title { get; set; }

        [JsonProperty("isAssociatedWithAuditedBusiness", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsAssociatedWithAuditedBusiness;

        [JsonProperty("authorizedRepresentatives", NullValueHandling = NullValueHandling.Ignore)]
        public List<PersonInternal> AuthorizedRepresentatives { get; set; }

        [JsonProperty("isAuthorizedRepresentativeFor", NullValueHandling = NullValueHandling.Ignore)]
        public List<PersonInternal> IsaAuthorizedRepresentativeFor { get; set; }
    }
}
