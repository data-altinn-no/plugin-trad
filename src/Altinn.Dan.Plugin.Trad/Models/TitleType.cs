using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Altinn.Dan.Plugin.Trad.Models
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TitleType
    {
       Advokat,
       Advokatfullmektig,
       Rettshjelper
    }
}
