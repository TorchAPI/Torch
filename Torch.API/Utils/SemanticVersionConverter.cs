#nullable enable
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Version = SemanticVersioning.Version;

namespace Torch.API.Utils;

public class SemanticVersionConverter : JsonConverter<Version>
{
    public override Version? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        Version.TryParse(reader.GetString(), out var ver);
        return ver;
    }

    public override void Write(Utf8JsonWriter writer, Version value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}