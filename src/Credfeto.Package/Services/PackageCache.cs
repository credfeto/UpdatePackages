using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Package.SerializationContext;
using Microsoft.Extensions.Logging;
using NonBlocking;
using NuGet.Versioning;

namespace Credfeto.Package.Services;

public sealed class PackageCache : IPackageCache
{
    private readonly ConcurrentDictionary<string, NuGetVersion> _cache;
    private readonly ILogger<PackageCache> _logger;
    private readonly JsonTypeInfo<Dictionary<string, string>> _typeInfo;
    private bool _changed;

    public PackageCache(ILogger<PackageCache> logger)
    {
        this._logger = logger;
        this._cache = new(StringComparer.OrdinalIgnoreCase);

        this._typeInfo = (SettingsSerializationContext.Default.GetTypeInfo(typeof(Dictionary<string, string>)) as JsonTypeInfo<Dictionary<string, string>>)!;
        this._changed = false;
    }

    public async Task LoadAsync(string fileName, CancellationToken cancellationToken)
    {
        this._logger.LogInformation($"Loading cache from {fileName}");

        string content = await File.ReadAllTextAsync(path: fileName, cancellationToken: cancellationToken);

        Dictionary<string, string>? packages = JsonSerializer.Deserialize(json: content, jsonTypeInfo: this._typeInfo);

        if (packages != null)
        {
            foreach ((string packageId, string version) in packages)
            {
                this._logger.LogInformation($"Loaded {packageId} {version} from cache");
                this._cache.TryAdd(key: packageId, NuGetVersion.Parse(version));
            }
        }
    }

    public Task SaveAsync(string fileName, CancellationToken cancellationToken)
    {
        if (!this._changed)
        {
            return Task.CompletedTask;
        }

        this._logger.LogInformation($"Saving cache to {fileName}");

        Dictionary<string, string> toWrite =
            this._cache.ToDictionary(keySelector: x => x.Key, elementSelector: x => x.Value.ToString(), comparer: StringComparer.OrdinalIgnoreCase);

        string content = JsonSerializer.Serialize(value: toWrite, jsonTypeInfo: this._typeInfo);

        return File.WriteAllTextAsync(path: fileName, contents: content, cancellationToken: cancellationToken);
    }

    public IReadOnlyList<PackageVersion> GetVersions(IReadOnlyList<string> packageIds)
    {
        return this._cache.Where(x => packageIds.Contains(value: x.Key, comparer: StringComparer.OrdinalIgnoreCase))
                   .Select(x => new PackageVersion(packageId: x.Key, version: x.Value))
                   .ToArray();
    }

    public void SetVersions(IReadOnlyList<PackageVersion> matching)
    {
        foreach (PackageVersion packageVersion in matching)
        {
            this.UpdateCache(packageVersion);
        }
    }

    private void UpdateCache(PackageVersion packageVersion)
    {
        if (this._cache.TryGetValue(key: packageVersion.PackageId, out NuGetVersion? existing))
        {
            if (existing < packageVersion.Version)
            {
                if (this._cache.TryUpdate(key: packageVersion.PackageId, newValue: packageVersion.Version, comparisonValue: existing))
                {
                    this._logger.LogInformation($"Updated cache of {packageVersion.PackageId} from {existing} to {packageVersion.Version}");
                    this._changed = true;
                }
            }
        }
        else
        {
            if (this._cache.TryAdd(key: packageVersion.PackageId, value: packageVersion.Version))
            {
                this._logger.LogInformation($"Adding cache of {packageVersion.PackageId} at {packageVersion.Version}");
                this._changed = true;
            }
        }
    }
}