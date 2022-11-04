using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NonBlocking;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Credfeto.Package.Services;

public sealed class PackageRegistry : IPackageRegistry
{
    private const bool INCLUDE_UNLISTED_PACKAGES = false;

    private static readonly SearchFilter SearchFilter =
        new(includePrerelease: false, filter: SearchFilterType.IsLatestVersion) { IncludeDelisted = INCLUDE_UNLISTED_PACKAGES, OrderBy = SearchOrderBy.Id };

    private readonly ILogger<PackageRegistry> _logger;

    public PackageRegistry(ILogger<PackageRegistry> logger)
    {
        this._logger = logger;
    }

    public async Task<IReadOnlyList<PackageVersion>> FindPackagesAsync(IReadOnlyList<string> packageIds, IReadOnlyList<string> packageSources, CancellationToken cancellationToken)
    {
        IReadOnlyList<PackageSource> sources = DefinePackageSources(packageSources);

        ConcurrentDictionary<string, NuGetVersion> packages = new(StringComparer.OrdinalIgnoreCase);

        foreach (string packageId in packageIds)
        {
            await this.FindPackageInSourcesAsync(sources: sources, packageId: packageId, packages: packages, cancellationToken: cancellationToken);
        }

        return packages.Select(p => new PackageVersion(packageId: p.Key, version: p.Value))
                       .ToArray();
    }

    private static IReadOnlyList<PackageSource> DefinePackageSources(IReadOnlyList<string> sources)
    {
        PackageSourceProvider packageSourceProvider = new(Settings.LoadDefaultSettings(Environment.CurrentDirectory));

        return packageSourceProvider.LoadPackageSources()
                                    .Concat(sources.Select(CreateCustomPackageSource))
                                    .ToArray();
    }

    private static PackageSource CreateCustomPackageSource(string source, int sourceId)
    {
        return new(name: $"Custom{sourceId}", source: source, isEnabled: true, isPersistable: true, isOfficial: true);
    }

    private async Task LoadPackagesFromSourceAsync(PackageSource packageSource,
                                                   string packageId,
                                                   ConcurrentDictionary<string, NuGetVersion> found,
                                                   CancellationToken cancellationToken)
    {
        SourceRepository sourceRepository = new(source: packageSource, new List<Lazy<INuGetResourceProvider>>(Repository.Provider.GetCoreV3()));

        PackageSearchResource searcher = await sourceRepository.GetResourceAsync<PackageSearchResource>(cancellationToken);
        IEnumerable<IPackageSearchMetadata> result = await searcher.SearchAsync(searchTerm: packageId,
                                                                                filters: SearchFilter,
                                                                                log: NullLogger.Instance,
                                                                                cancellationToken: cancellationToken,
                                                                                skip: 0,
                                                                                take: int.MaxValue);

        foreach (PackageVersion packageVersion in result.Select(entry => entry.Identity)
                                                        .Select(identity => new PackageVersion(packageId: identity.Id, version: identity.Version)))
        {
            if (StringComparer.InvariantCultureIgnoreCase.Equals(x: packageId, y: packageVersion.PackageId) && !IsBannedPackage(packageVersion) &&
                found.TryAdd(key: packageVersion.PackageId, value: packageVersion.Version))
            {
                this._logger.LogInformation($"Found package {packageVersion.PackageId} version {packageVersion.Version} in {packageSource.Source}");
            }
        }
    }

    private static bool IsBannedPackage(PackageVersion packageVersion)
    {
        return packageVersion.Version.ToString()
                             .Contains(value: '+', comparisonType: StringComparison.Ordinal);
    }

    private async Task FindPackageInSourcesAsync(IReadOnlyList<PackageSource> sources,
                                                 string packageId,
                                                 ConcurrentDictionary<string, NuGetVersion> packages,
                                                 CancellationToken cancellationToken)
    {
        this._logger.LogInformation($"Enumerating matching package versions for {packageId}...");

        ConcurrentDictionary<string, NuGetVersion> found = new(StringComparer.Ordinal);

        await Task.WhenAll(
            sources.Select(selector: source => this.LoadPackagesFromSourceAsync(packageSource: source, packageId: packageId, found: found, cancellationToken: cancellationToken)));

        foreach ((string key, NuGetVersion value) in found)
        {
            packages.TryAdd(key: key, value: value);
        }
    }
}