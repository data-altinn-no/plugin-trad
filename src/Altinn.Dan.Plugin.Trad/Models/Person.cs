using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Altinn.Dan.Plugin.Trad.Models
{
    public class Person
    {
        [JsonProperty("ssn")]
        public string Ssn { get; set; }
        
        [JsonProperty("title")]
        public TitleType TitleType { get; set; }

        [JsonProperty("authorizedRepresentatives", NullValueHandling = NullValueHandling.Ignore)]
        public List<Person> IsPrincipalFor { get; set; }

        [JsonProperty("principal", NullValueHandling = NullValueHandling.Ignore)]
        public Person Principal { get; set; }
    }
}
