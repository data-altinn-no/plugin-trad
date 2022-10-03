using System.Collections.Generic;
using Newtonsoft.Json;

namespace Altinn.Dan.Plugin.Trad.Models
{
    public record PracticeExternal
    {
        [JsonProperty("organisasjonsNummer")]
        public int OrganizationNumber { get; set; }

        [JsonProperty("autoriserteRepresentanter", NullValueHandling = NullValueHandling.Ignore)]
        public List<PersonExternal> AuthorizedRepresentatives { get; set; }

        [JsonProperty("erAutorisertRepresentantFor", NullValueHandling = NullValueHandling.Ignore)]
        public List<PersonExternal> IsaAuthorizedRepresentativeFor { get; set; }
    }
}