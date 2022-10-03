using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Altinn.Dan.Plugin.Trad.Models
{
    [JsonConverter(typeof(TitleTypeInternalStringEnumConverter))]
    public enum TitleTypeInternal
    {
        [EnumMember] 
        Ukjent,

        [EnumMember]
        Advokat,

        [EnumMember]
        Advokatfullmektig,

        [EnumMember]
        Rettshjelper,

        [EnumMember(Value = "E�S-Advokat")]
        EosAdvokat,

        [EnumMember(Value = "Utenlandsk advokat")]
        UtenlandskAdvokat
    }

    public class TitleTypeInternalStringEnumConverter : StringEnumConverter
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.Value == null || string.IsNullOrEmpty(reader.Value.ToString()) ||
                !Enum.TryParse<TitleTypeInternal>(reader.Value.ToString(), true, out _))
            {
                return TitleTypeInternal.Ukjent;
            }

            return base.ReadJson(reader, objectType, existingValue, serializer);
        }
    }
}
