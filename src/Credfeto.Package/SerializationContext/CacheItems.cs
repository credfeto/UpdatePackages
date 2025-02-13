using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Credfeto.Package.SerializationContext;

[JsonConverter(typeof(CacheItemsConverter))]
internal sealed class CacheItems
{
    private readonly Dictionary<string, string> _cache;

    public CacheItems(Dictionary<string, string> cache)
    {
        this._cache = cache;
    }

    [JsonIgnore]
    public IReadOnlyDictionary<string, string> Cache => this._cache;
}
