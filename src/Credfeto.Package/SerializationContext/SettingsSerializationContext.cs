using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Credfeto.Package.SerializationContext;

[SuppressMessage(category: "ReSharper", checkId: "PartialTypeWithSinglePart", Justification = "Required for JsonSerializerContext")]
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Serialization | JsonSourceGenerationMode.Metadata,
                             PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
                             DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                             WriteIndented = false,
                             IncludeFields = false)]
[JsonSerializable(typeof(CacheItems))]
internal sealed partial class SettingsSerializationContext : JsonSerializerContext
{
    // Code generated
}