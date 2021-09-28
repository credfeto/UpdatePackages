using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Credfeto.Package.Update.Helpers
{
    internal static class PackageSourceHelpers
    {
        private const bool INCLUDE_UNLISTED_PACKAGES = false;

        private static readonly SearchFilter SearchFilter =
            new(includePrerelease: false, filter: SearchFilterType.IsLatestVersion) { IncludeDelisted = INCLUDE_UNLISTED_PACKAGES, OrderBy = SearchOrderBy.Id };

        private static readonly ILogger NugetLogger = new NullLogger();

        public static List<PackageSource> DefinePackageSources(string source)
        {
            PackageSourceProvider packageSourceProvider = new(Settings.LoadDefaultSettings(Environment.CurrentDirectory));

            List<PackageSource> sources = packageSourceProvider.LoadPackageSources()
                                                               .ToList();

            if (!string.IsNullOrEmpty(source))
            {
                sources.Add(new PackageSource(name: "Custom", source: source, isEnabled: true, isPersistable: true, isOfficial: true));
            }

            return sources;
        }

        private static async Task LoadPackagesFromSourceAsync(PackageSource packageSource, string packageId, ConcurrentDictionary<string, NuGetVersion> found, CancellationToken cancellationToken)
        {
            SourceRepository sourceRepository = new(source: packageSource, new List<Lazy<INuGetResourceProvider>>(Repository.Provider.GetCoreV3()));

            PackageSearchResource searcher = await sourceRepository.GetResourceAsync<PackageSearchResource>(cancellationToken);
            IEnumerable<IPackageSearchMetadata> result =
                await searcher.SearchAsync(searchTerm: packageId, filters: SearchFilter, log: NugetLogger, cancellationToken: cancellationToken, skip: 0, take: int.MaxValue);

            foreach (IPackageSearchMetadata entry in result)
            {
                PackageVersion packageVersion = new(packageId: entry.Identity.Id, version: entry.Identity.Version);

                if (PackageIdHelpers.IsExactMatch(packageId: packageId, packageVersion: packageVersion) && !IsBannedPackage(packageVersion))
                {
                    found.TryAdd(key: packageVersion.PackageId, value: packageVersion.Version);
                }
            }
        }

        private static bool IsBannedPackage(PackageVersion packageVersion)
        {
            return packageVersion.Version.ToString()
                                 .Contains(value: "+", comparisonType: StringComparison.Ordinal);
        }

        public static async Task FindPackagesAsync(IReadOnlyList<PackageSource> sources, string packageId, Dictionary<string, NuGetVersion> packages, CancellationToken cancellationToken)
        {
            Console.WriteLine(value: $"Enumerating matching package versions for {packageId}...");

            ConcurrentDictionary<string, NuGetVersion> found = new();

            await Task.WhenAll(sources.Select(selector: source => LoadPackagesFromSourceAsync(packageSource: source, packageId: packageId, found: found, cancellationToken: cancellationToken)));

            foreach ((string key, NuGetVersion value) in found)
            {
                packages.TryAdd(key: key, value: value);
            }
        }
    }
}