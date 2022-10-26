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
    private readonly IPackageRegistry _packageRegistry;
    private readonly IProjectLoader _projectLoader;

    public PackageUpdater(IProjectLoader projectLoader, IPackageRegistry packageRegistry, ILogger<PackageUpdater> logger)
    {
        this._projectLoader = projectLoader;
        this._packageRegistry = packageRegistry;
        this._logger = logger;
    }

    public async Task<int> UpdateAsync(string basePath, PackageUpdateConfiguration configuration, IReadOnlyList<string> packageSources, CancellationToken cancellationToken)
    {
        IReadOnlyList<IProject> projects = await this.FindProjectsAsync(basePath);

        ConcurrentDictionary<string, ConcurrentDictionary<IProject, NuGetVersion>> projectsByPackage = FindMatchingPackages(configuration: configuration, projects: projects);

        if (projectsByPackage.Count == 0)
        {
            this._logger.LogInformation("No matching packages installed");

            return 0;
        }

        IReadOnlyList<PackageVersion> matching = await this._packageRegistry.FindPackagesAsync(projectsByPackage.Keys.ToArray(), packageSources: packageSources, cancellationToken: cancellationToken);

        if (matching.Count == 0)
        {
            this._logger.LogInformation("No matching packages found in event source");

            return 0;
        }

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
                }
            }
        }

        foreach (IProject project in projects.Where(project => project.Changed))
        {
            this._logger.LogInformation($"Saving {project.FileName}");
            project.Save();
        }

        return updates;
    }

    private static ConcurrentDictionary<string, ConcurrentDictionary<IProject, NuGetVersion>> FindMatchingPackages(PackageUpdateConfiguration configuration, IReadOnlyList<IProject> projects)
    {
        ConcurrentDictionary<string, ConcurrentDictionary<IProject, NuGetVersion>> projectsByPackage = new(StringComparer.OrdinalIgnoreCase);

        foreach (IProject project in projects)
        {
            foreach (PackageVersion package in project.Packages.Where(package => IsMatchingPackage(configuration: configuration, package: package)))
            {
                ConcurrentDictionary<IProject, NuGetVersion> projectPackage = projectsByPackage.GetOrAdd(package.PackageId.ToLowerInvariant(), new ConcurrentDictionary<IProject, NuGetVersion>());
                projectPackage.TryAdd(key: project, value: package.Version);
            }
        }

        return projectsByPackage;
    }

    private static bool IsMatchingPackage(PackageUpdateConfiguration configuration, PackageVersion package)
    {
        return configuration.Package.IsMatchingPackage(package) && !configuration.ExcludedPackages.Any(x => x.IsMatchingPackage(package));
    }

    private async Task<IReadOnlyList<IProject>> FindProjectsAsync(string basePath)
    {
        IReadOnlyList<string> projectFileNames = FindProjects(basePath);

        IReadOnlyList<IProject?> loadedProjects = await Task.WhenAll(projectFileNames.Select(fileName => this._projectLoader.LoadAsync(fileName)));

        return loadedProjects.RemoveNulls()
                             .ToArray();
    }

    public static IReadOnlyList<string> FindProjects(string folder)
    {
        return Directory.EnumerateFiles(path: folder, searchPattern: "*.csproj", searchOption: SearchOption.AllDirectories)
                        .ToArray();
    }
}