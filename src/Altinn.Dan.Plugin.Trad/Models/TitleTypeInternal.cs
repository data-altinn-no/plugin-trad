using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Altinn.Dan.Plugin.Trad.Models
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TitleTypeInternal
    {
        [EnumMember] 
        Ukjent,

        [EnumMember]
        Advokat,

        [EnumMember]
        Advokatfullmektig,

        [EnumMember(Value="Rettshjelper nr1")]
        Rettshjelper,

        [EnumMember(Value = "EØS-Advokat")]
        EosAdvokat,

        [EnumMember(Value = "Utenlandsk advokat")]
        UtenlandskAdvokat
    }
}
