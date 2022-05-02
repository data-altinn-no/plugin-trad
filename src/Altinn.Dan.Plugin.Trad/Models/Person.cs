using System.Collections.Generic;
using Newtonsoft.Json;

namespace Altinn.Dan.Plugin.Trad.Models
{
    public record Person
    {
        [JsonProperty("ssn")]
        public string Ssn { get; set; }
        
        [JsonProperty("title")]
        public TitleType Title { get; set; }

        [JsonProperty("authorizedRepresentatives", NullValueHandling = NullValueHandling.Ignore)]
        public List<Person> AuthorizedRepresentatives { get; set; }

        [JsonProperty("isAuthorizedRepresentativeFor", NullValueHandling = NullValueHandling.Ignore)]
        public List<Person> IsaAuthorizedRepresentativeFor { get; set; }
    }
}
