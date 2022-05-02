using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Altinn.Dan.Plugin.Trad.Models
{
    [JsonConverter(typeof(TitleTypeStringEnumConverter))]
    public enum TitleType
    {
        [EnumMember] 
        Ukjent,

        [EnumMember]
        Advokat,

        [EnumMember]
        Advokatfullmektig,

        [EnumMember]
        Rettshjelper,

        [EnumMember(Value = "EØS-Advokat")]
        EosAdvokat,

        [EnumMember(Value = "Utenlandsk advokat")]
        UtenlandskAdvokat
    }

    public class TitleTypeStringEnumConverter : StringEnumConverter
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.Value == null || string.IsNullOrEmpty(reader.Value.ToString()) ||
                !Enum.TryParse<TitleType>(reader.Value.ToString(), true, out _))
            {
                return TitleType.Ukjent;
            }

            return base.ReadJson(reader, objectType, existingValue, serializer);
        }
    }
}
