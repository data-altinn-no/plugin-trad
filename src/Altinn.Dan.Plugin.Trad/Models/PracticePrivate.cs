using System.Collections.Generic;
using Newtonsoft.Json;

namespace Altinn.Dan.Plugin.Trad.Models;

public record PracticePrivate
{
    [JsonProperty("organisasjonsNummer")]
    public int OrganizationNumber { get; set; }

    [JsonProperty("underenhet", NullValueHandling = NullValueHandling.Ignore)]
    public int? SubOrganizationNumber { get; set; }

    [JsonProperty("autoriserteRepresentanter", NullValueHandling = NullValueHandling.Ignore)]
    public List<PersonPrivate> AuthorizedRepresentatives { get; set; }

    [JsonProperty("erAutorisertRepresentantFor", NullValueHandling = NullValueHandling.Ignore)]
    public List<PersonPrivate> IsaAuthorizedRepresentativeFor { get; set; }
        
    [JsonProperty("hovedpraksis", NullValueHandling = NullValueHandling.Ignore)]
    public bool? MainPractice { get; set; }
}