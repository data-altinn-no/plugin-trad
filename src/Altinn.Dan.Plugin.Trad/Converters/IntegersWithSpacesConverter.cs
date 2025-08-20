using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Altinn.Dan.Plugin.Trad.Converters;

public class IntegersWithSpacesConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var token = JToken.FromObject(value);
        token.WriteTo(writer);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var value = reader.Value;
        return value switch
        {
            null or int => value,
            long l => (int)l,
            string s => int.Parse(s.Replace(" ", string.Empty)),
            _ => throw new JsonSerializationException($"Unable to serialize property {value} to integer")
        };
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(int) || objectType == typeof(int?) || objectType == typeof(string);
    }
}