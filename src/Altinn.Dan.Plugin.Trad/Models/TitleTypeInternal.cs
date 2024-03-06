using System;
using System.Globalization;
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

        [EnumMember]
        EosAdvokat,

        [EnumMember]
        UtenlandskAdvokat
    }

    public class TitleTypeInternalStringEnumConverter : StringEnumConverter
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.Value == null || string.IsNullOrEmpty(reader.Value.ToString()) ||
                !Enum.TryParse<TitleTypeInternal>(reader.Value.ToString(), true, out _))
            {
                var valueString = reader.Value.ToString();

                if (string.IsNullOrEmpty(valueString))
                    return TitleTypeInternal.Ukjent;

                switch (valueString)
                {
                    case "Rettshjelper nr1": return TitleTypeInternal.Rettshjelper;
                    case "EØS-Advokat": return TitleTypeInternal.EosAdvokat;
                    case "Utenlandsk advokat": return TitleTypeInternal.UtenlandskAdvokat;
                    case var _ when valueString.Contains('-'): return TitleTypeInternal.EosAdvokat;
                    default: return TitleTypeInternal.Ukjent;
                }
            }

            return base.ReadJson(reader, objectType, existingValue, serializer);
        }
    }
}
