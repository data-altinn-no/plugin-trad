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

        // Not fetched from trad-registry, fetched from ER, so no json property needed, as we'll cache as we go
        public string OrganizationName { get; set; }
        public string SubOrganizationName { get; set; }
    }
    
    // Temp model in lieu of a better fix
    public record PracticeInternalTemp
    {
        [JsonProperty("firmanr", NullValueHandling = NullValueHandling.Ignore)]
        public string CompanyNumber { get; set; }

        [JsonProperty("orgNumber")]
        public string OrganizationNumber { get; set; }

        [JsonProperty("subOrgNumber", NullValueHandling = NullValueHandling.Ignore)]
        public string SubOrganizationNumber { get; set; }

        [JsonProperty("authorizedRepresentatives", NullValueHandling = NullValueHandling.Ignore)]
        public List<PersonInternalTemp> AuthorizedRepresentatives;

        [JsonProperty("isAuthorizedRepresentativeFor", NullValueHandling = NullValueHandling.Ignore)]
        public List<PersonInternalTemp> IsAnAuthorizedRepresentativeFor;
        
        [JsonProperty("hovedpraksis", NullValueHandling = NullValueHandling.Ignore)]
        public bool? MainPractice;
    }
}
