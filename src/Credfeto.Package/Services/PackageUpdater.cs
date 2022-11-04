using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Package.Extensions;
using Microsoft.Extensions.Logging;
using NonBlocking;
using NuGet.Versioning;

namespace Credfeto.Package.Services;

public sealed class PackageUpdater : IPackageUpdater
{
    private readonly ILogger<PackageUpdater> _logger;
    private readonly IPackageCache _packageCache;
    private readonly IPackageRegistry _packageRegistry;
    private readonly IProjectLoader _projectLoader;

    public PackageUpdater(IProjectLoader projectLoader, IPackageRegistry packageRegistry, IPackageCache packageCache, ILogger<PackageUpdater> logger)
    {
        this._projectLoader = projectLoader;
        this._packageRegistry = packageRegistry;
        this._packageCache = packageCache;
        this._logger = logger;
    }

    public async Task<IReadOnlyList<PackageVersion>> UpdateAsync(string basePath,
                                                                 PackageUpdateConfiguration configuration,
                                                                 IReadOnlyList<string> packageSources,
                                                                 CancellationToken cancellationToken)
    {
        IReadOnlyList<IProject> projects = await this.FindProjectsAsync(basePath: basePath, cancellationToken: cancellationToken);

        ConcurrentDictionary<string, ConcurrentDictionary<IProject, NuGetVersion>> projectsByPackage = FindMatchingPackages(configuration: configuration, projects: projects);

        if (projectsByPackage.Count == 0)
        {
            this._logger.LogInformation("No matching packages installed");

            return Array.Empty<PackageVersion>();
        }

        IReadOnlyList<string> packageIds = projectsByPackage.Keys.ToArray();
        IReadOnlyList<PackageVersion> cachedVersions = this._packageCache.GetVersions(packageIds);

        IReadOnlyList<PackageVersion> matching;

        if (cachedVersions.Count != packageIds.Count)
        {
            matching = await this._packageRegistry.FindPackagesAsync(packageIds: packageIds, packageSources: packageSources, cancellationToken: cancellationToken);

            if (matching.Count == 0)
            {
                this._logger.LogInformation("No matching packages found in event source");

                return Array.Empty<PackageVersion>();
            }

            this._packageCache.SetVersions(matching);
        }
        else
        {
            matching = cachedVersions;
        }

        ConcurrentDictionary<string, NuGetVersion> updated = new(StringComparer.OrdinalIgnoreCase);

        int updates = this.UpdateProjects(matching: matching, projectsByPackage: projectsByPackage, updated: updated);

        if (updates == 0)
        {
            this._logger.LogInformation("All installed packages are up-to-date");

            return Array.Empty<PackageVersion>();
        }

        this.SaveChanges(projects);

        return updated.Select(p => new PackageVersion(packageId: p.Key, version: p.Value))
                      .ToArray();
    }

    private void SaveChanges(IReadOnlyList<IProject> projects)
    {
        foreach (IProject project in projects.Where(project => project.Changed))
        {
            this._logger.LogInformation($"Saving {project.FileName}");
            project.Save();
        }
    }

    private int UpdateProjects(IReadOnlyList<PackageVersion> matching,
                               ConcurrentDictionary<string, ConcurrentDictionary<IProject, NuGetVersion>> projectsByPackage,
                               ConcurrentDictionary<string, NuGetVersion> updated)
    {
        int updates = 0;

        foreach (PackageVersion packageVersion in matching)
        {
            if (!projectsByPackage.TryGetValue(key: packageVersion.PackageId, out ConcurrentDictionary<IProject, NuGetVersion>? projectsToUpdate))
            {
                continue;
            }

            foreach ((IProject project, NuGetVersion version) in projectsToUpdate)
            {
                if (packageVersion.Version > version)
                {
                    this._logger.LogInformation($"Updating {packageVersion.PackageId} from {version} to {packageVersion.Version} in {project.FileName}");

                    project.UpdatePackage(packageVersion);
                    ++updates;

                    updated.TryAdd(key: packageVersion.PackageId, value: packageVersion.Version);
                }
            }
        }

        return updates;
    }

    private static ConcurrentDictionary<string, ConcurrentDictionary<IProject, NuGetVersion>> FindMatchingPackages(
        PackageUpdateConfiguration configuration,
        IReadOnlyList<IProject> projects)
    {
        ConcurrentDictionary<string, ConcurrentDictionary<IProject, NuGetVersion>> projectsByPackage = new(StringComparer.OrdinalIgnoreCase);

        foreach (IProject project in projects)
        {
            foreach (PackageVersion package in project.Packages.Where(package => IsMatchingPackage(configuration: configuration, package: package)))
            {
                ConcurrentDictionary<IProject, NuGetVersion> projectPackage =
                    projectsByPackage.GetOrAdd(package.PackageId.ToLowerInvariant(), new ConcurrentDictionary<IProject, NuGetVersion>());
                projectPackage.TryAdd(key: project, value: package.Version);
            }
        }

        return projectsByPackage;
    }

    private static bool IsMatchingPackage(PackageUpdateConfiguration configuration, PackageVersion package)
    {
        return configuration.PackageMatch.IsMatchingPackage(package) && !configuration.ExcludedPackages.Any(x => x.IsMatchingPackage(package));
    }

    private async Task<IReadOnlyList<IProject>> FindProjectsAsync(string basePath, CancellationToken cancellationToken)
    {
        IReadOnlyList<string> projectFileNames = FindProjects(basePath);

        IReadOnlyList<IProject?> loadedProjects =
            await Task.WhenAll(projectFileNames.Select(fileName => this._projectLoader.LoadAsync(path: fileName, cancellationToken: cancellationToken)));

        return loadedProjects.RemoveNulls()
                             .ToArray();
    }

    private static IReadOnlyList<string> FindProjects(string folder)
    {
        return Directory.EnumerateFiles(path: folder, searchPattern: "*.csproj", searchOption: SearchOption.AllDirectories)
                        .ToArray();
    }
}