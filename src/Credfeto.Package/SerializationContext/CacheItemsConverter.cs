using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Credfeto.Package.SerializationContext;

internal sealed class CacheItemsConverter : JsonConverter<CacheItems>
{
    public override CacheItems Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            return ThrowInvalidJsonToken();
        }

        // step forward
        reader.Read();

        Dictionary<string, string> instance = new(StringComparer.OrdinalIgnoreCase);

        while (reader.TokenType != JsonTokenType.EndObject)
        {
            (string key, string value) = ReadEntry(reader: ref reader);
            instance.Add(key: key, value: value);
        }

        return new(instance);
    }

    [DoesNotReturn]
    private static CacheItems ThrowInvalidJsonToken()
    {
        throw new JsonException(message: "Invalid Json token");
    }

    public override void Write(Utf8JsonWriter writer, CacheItems value, JsonSerializerOptions options)
    {
        if (writer == null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        writer.WriteStartObject();

        foreach ((string key, string v) in value.Cache.OrderBy(x => x.Key.ToLowerInvariant()))
        {
            writer.WriteString(propertyName: key, value: v);
        }

        writer.WriteEndObject();
    }

    private static (string Key, string Value) ReadEntry(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.PropertyName)
        {
            return ThrowInvalidJsonTokenEntry();
        }

        string key = reader.GetString() ?? RequiredString();

        reader.Read();

        string value = reader.GetString() ?? RequiredString();

        reader.Read();

        return (key, value);
    }

    [DoesNotReturn]
    private static (string Key, string Value) ThrowInvalidJsonTokenEntry()
    {
        throw new JsonException(message: "Invalid Json token");
    }

    [DoesNotReturn]
    private static string RequiredString()
    {
        throw new JsonException(message: "Invalid Json token");
    }
}