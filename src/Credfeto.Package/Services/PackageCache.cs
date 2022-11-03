using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NonBlocking;
using NuGet.Versioning;

namespace Credfeto.Package.Services;

public sealed class PackageCache : IPackageCache
{
    private readonly ConcurrentDictionary<string, NuGetVersion> _cache;
    private readonly ILogger<PackageCache> _logger;

    public PackageCache(ILogger<PackageCache> logger)
    {
        this._logger = logger;
        this._cache = new(StringComparer.OrdinalIgnoreCase);
    }

    public Task LoadAsync(string fileName, CancellationToken none)
    {
        this._logger.LogInformation($"Loading cache from {fileName}");

        return Task.CompletedTask;
    }

    public Task SaveAsync(string fileName, CancellationToken none)
    {
        this._logger.LogInformation($"Saving cache to {fileName}");

        return Task.CompletedTask;
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
                }
            }
        }
        else
        {
            if (this._cache.TryAdd(key: packageVersion.PackageId, value: packageVersion.Version))
            {
                this._logger.LogInformation($"Adding cache of {packageVersion.PackageId} at {packageVersion.Version}");
            }
        }
    }
}