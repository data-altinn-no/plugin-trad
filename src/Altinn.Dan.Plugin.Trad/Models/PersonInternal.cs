using System.Collections.Generic;
using Newtonsoft.Json;

namespace Altinn.Dan.Plugin.Trad.Models
{
    public record PersonInternal
    {
        [JsonProperty("regnr", NullValueHandling = NullValueHandling.Ignore)]
        public string RegistrationNumber { get; set; }

        [JsonProperty("ssn")]
        public string Ssn { get; set; }
        
        [JsonProperty("title")]
        public TitleTypeInternal Title { get; set; }
        
        [JsonProperty("firstName")]
        public string Firstname { get; set; }
        
        [JsonProperty("middleName")]
        public string MiddleName { get; set; }
        
        [JsonProperty("lastName")]
        public string LastName { get; set; }

        [JsonProperty("practices", NullValueHandling = NullValueHandling.Ignore)]
        public List<PracticeInternal> Practices { get; set; }
    }
}
