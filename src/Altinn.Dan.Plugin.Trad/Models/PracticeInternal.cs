using System.Collections.Generic;
using Newtonsoft.Json;

namespace Altinn.Dan.Plugin.Trad.Models
{
    public record PracticeInternal
    {
        [JsonProperty("firmanr", NullValueHandling = NullValueHandling.Ignore)]
        public string CompanyNumber { get; set; }

        [JsonProperty("orgNumber")]
        public int OrganizationNumber { get; set; }

        [JsonProperty("subOrgNumber", NullValueHandling = NullValueHandling.Ignore)]
        public int? SubOrganizationNumber { get; set; }

        [JsonProperty("authorizedRepresentatives", NullValueHandling = NullValueHandling.Ignore)]
        public List<PersonInternal> AuthorizedRepresentatives;

        [JsonProperty("isAuthorizedRepresentativeFor", NullValueHandling = NullValueHandling.Ignore)]
        public List<PersonInternal> IsAnAuthorizedRepresentativeFor;
        
        [JsonProperty("hovedpraksis", NullValueHandling = NullValueHandling.Ignore)]
        public bool? MainPractice;
    }
}
