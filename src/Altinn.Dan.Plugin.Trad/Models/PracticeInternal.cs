using System.Collections.Generic;
using Altinn.Dan.Plugin.Trad.Converters;
using Newtonsoft.Json;

namespace Altinn.Dan.Plugin.Trad.Models
{
    public record PracticeInternal
    {
        [JsonProperty("firmanr", NullValueHandling = NullValueHandling.Ignore)]
        public string CompanyNumber { get; set; }

        [JsonProperty("orgNumber")]
        [JsonConverter(typeof(IntegersWithSpacesConverter))]
        public int OrganizationNumber { get; set; }

        [JsonProperty("subOrgNumber", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(IntegersWithSpacesConverter))]
        public int? SubOrganizationNumber { get; set; }

        [JsonProperty("authorizedRepresentatives", NullValueHandling = NullValueHandling.Ignore)]
        public List<PersonInternal> AuthorizedRepresentatives;

        [JsonProperty("isAuthorizedRepresentativeFor", NullValueHandling = NullValueHandling.Ignore)]
        public List<PersonInternal> IsAnAuthorizedRepresentativeFor;
        
        [JsonProperty("hovedpraksis", NullValueHandling = NullValueHandling.Ignore)]
        public bool? MainPractice;

        // Not fetched from trad-registry, fetched from ER, so no json property needed, as we'll cache as we go
        public string OrganizationName { get; set; }
        public string SubOrganizationName { get; set; }
    }
}
