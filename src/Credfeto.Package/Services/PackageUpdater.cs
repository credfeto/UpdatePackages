using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Credfeto.Package.Extensions;
using Microsoft.Extensions.Logging;
using NonBlocking;
using NuGet.Versioning;

namespace Credfeto.Package.Services;

public sealed class PackageUpdater : IPackageUpdater
{
    private readonly ILogger<PackageUpdater> _logger;
    private readonly IProjectLoader _projectLoader;

    public PackageUpdater(IProjectLoader projectLoader, ILogger<PackageUpdater> logger)
    {
        this._projectLoader = projectLoader;
        this._logger = logger;
    }

    public async Task<int> UpdateAsync(string basePath, PackageUpdateConfiguration configuration, IReadOnlyList<string> packageSources)
    {
        IReadOnlyList<IProject> projects = await this.FindProjectsAsync(basePath);

        ConcurrentDictionary<string, ConcurrentDictionary<IProject, NuGetVersion>> projectsByPackage = FindMatchingPackages(configuration: configuration, projects: projects);

        if (projectsByPackage.Count == 0)
        {
            this._logger.LogInformation("Found 0 matching packages");

            return 0;
        }

        return projectsByPackage.Count;
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