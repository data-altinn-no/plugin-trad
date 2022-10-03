using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Altinn.Dan.Plugin.Trad.Models
{
    [JsonConverter(typeof(TitleTypeExternalStringEnumConverter))]
    public enum TitleTypeExternal
    {
        [EnumMember] 
        Ukjent,

        [EnumMember]
        Advokat,

        [EnumMember]
        Advokatfullmektig,

        [EnumMember]
        Rettshjelper,

        [EnumMember]
        EosAdvokat,

        [EnumMember]
        UtenlandskAdvokat
    }

    public class TitleTypeExternalStringEnumConverter : StringEnumConverter
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.Value == null || string.IsNullOrEmpty(reader.Value.ToString()) ||
                !Enum.TryParse<TitleTypeExternal>(reader.Value.ToString(), true, out _))
            {
                return TitleTypeExternal.Ukjent;
            }

            return base.ReadJson(reader, objectType, existingValue, serializer);
        }
    }
}
