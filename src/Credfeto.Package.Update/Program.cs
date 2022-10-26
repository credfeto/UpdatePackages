using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Credfeto.Package.Update.Helpers;
using Microsoft.Extensions.Configuration;
using NuGet.Configuration;
using NuGet.Versioning;

namespace Credfeto.Package.Update;

internal static class Program
{
    private const int SUCCESS = 0;
    private const int ERROR = 1;

    private static async Task<int> Main(string[] args)
    {
        Console.WriteLine($"{typeof(Program).Namespace} {ExecutableVersionInformation.ProgramVersion()}");

        try
        {
            return await LookForUpdatesAsync(args);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"ERROR: {exception.Message}");

            return ERROR;
        }
    }

    private static async Task<int> LookForUpdatesAsync(string[] args)
    {
        IConfigurationRoot configuration = ConfigurationLoader.LoadConfiguration(args);

        string folder = configuration.GetValue<string>(key: @"Folder");

        if (string.IsNullOrEmpty(folder))
        {
            Console.WriteLine("ERROR: folder not specified");

            return ERROR;
        }

        string source = configuration.GetValue<string>(key: @"source");

        IReadOnlyList<PackageSource> sources = PackageSourceHelpers.DefinePackageSources(source);

        IReadOnlyList<string> projects = ProjectHelpers.FindProjects(folder);

        string packageId = configuration.GetValue<string>(key: @"packageid");
        string prefix = configuration.GetValue<string>(key: @"packageprefix");

        IReadOnlyList<ExcludedPackage> excludedPackages = GetExcludedPackages(configuration);

        Dictionary<string, NuGetVersion>? packages =
            await DetermineMatchingInstalledPackagesAsync(packageId: packageId, prefix: prefix, projects: projects, excludes: excludedPackages, sources: sources);

        if (packages == null)
        {
            return ERROR;
        }

        if (packages.Count == 0)
        {
            Console.WriteLine($"No updates needed - package {packageId} is not used by any project.");

            return SUCCESS;
        }

        Dictionary<string, NuGetVersion> updatesMade = UpdateProjects(projects: projects, packages: packages);

        Console.WriteLine();

        if (updatesMade.Count != 0)
        {
            Console.WriteLine($"Total Updates: {updatesMade.Count}");

            OutputPackageUpdateTags(updatesMade);

            return SUCCESS;
        }

        Console.WriteLine(value: "No updates made.");

        return ERROR;
    }

    private static IReadOnlyList<ExcludedPackage> GetExcludedPackages(IConfigurationRoot configuration)
    {
        string[]? excludes = configuration.GetValue<string[]>("excludes");

        if (excludes != null && excludes.Length != 0)
        {
            List<ExcludedPackage> excludedPackages = new();

            foreach (string exclude in excludes)
            {
                string[] p = exclude.Split(separator: ':');

                if (p.Length == 2)
                {
                    excludedPackages.Add(new(p[0], StringComparer.InvariantCultureIgnoreCase.Equals(p[1], y: "prefix")));
                }
                else
                {
                    excludedPackages.Add(new(p[0], Prefix: false));
                }

                Console.WriteLine($"Excluding {exclude}");
            }

            return excludedPackages;
        }

        return Array.Empty<ExcludedPackage>();
    }

    private static async Task<Dictionary<string, NuGetVersion>?> DetermineMatchingInstalledPackagesAsync(string packageId,
                                                                                                         string prefix,
                                                                                                         IReadOnlyList<string> projects,
                                                                                                         IReadOnlyList<ExcludedPackage> excludes,
                                                                                                         IReadOnlyList<PackageSource> sources)
    {
        if (!string.IsNullOrEmpty(packageId))
        {
            return await DetermineMatchingInstalledPackagesForPackageIdAsync(packageId: packageId, projects: projects, excludes: excludes, sources: sources);
        }

        if (string.IsNullOrWhiteSpace(prefix))
        {
            Console.WriteLine("ERROR: neither packageid or packageprefix specified");

            return null;
        }

        return await DetermineMatchingInstalledPackagesForPrefixAsync(packageId: packageId, prefix: prefix, projects: projects, excludes: excludes, sources: sources);
    }

    private static async Task<Dictionary<string, NuGetVersion>?> DetermineMatchingInstalledPackagesForPackageIdAsync(string packageId,
                                                                                                                     IReadOnlyList<string> projects,
                                                                                                                     IReadOnlyList<ExcludedPackage> excludes,
                                                                                                                     IReadOnlyList<PackageSource> sources)
    {
        Dictionary<string, NuGetVersion> packages = new(StringComparer.Ordinal);

        if (!HasMatchingPackagesInProjects(projects: projects, packageId: packageId))
        {
            Console.WriteLine($"No updates needed - package {packageId} is not used by any project.");

            return packages;
        }

        if (IsExcluded(packageId: packageId, excludes: excludes))
        {
            Console.WriteLine($"No updates needed for package {packageId} as it is excluded.");

            return packages;
        }

        await PackageSourceHelpers.FindPackagesAsync(sources: sources, packageId: packageId, packages: packages, cancellationToken: CancellationToken.None);

        return packages;
    }

    private static bool IsExcluded(string packageId, IReadOnlyList<ExcludedPackage> excludes)
    {
        foreach (ExcludedPackage exclude in excludes)
        {
            if (exclude.Prefix)
            {
                if (PackageIdHelpers.IsPrefixMatch(packageIdPrefix: exclude.PackageId, package: packageId))
                {
                    return true;
                }
            }
            else
            {
                if (PackageIdHelpers.IsExactMatch(package: exclude.PackageId, packageId: packageId))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static async Task<Dictionary<string, NuGetVersion>?> DetermineMatchingInstalledPackagesForPrefixAsync(string packageId,
                                                                                                                  string prefix,
                                                                                                                  IReadOnlyList<string> projects,
                                                                                                                  IReadOnlyList<ExcludedPackage> excludes,
                                                                                                                  IReadOnlyList<PackageSource> sources)
    {
        Dictionary<string, NuGetVersion> packages = new(StringComparer.Ordinal);
        IReadOnlyList<string> packageIds = FindPackagesByPrefixFromProjects(projects: projects, packageIdPrefix: prefix);

        if (packageIds.Count == 0)
        {
            Console.WriteLine($"No updates needed - No packaged matching {packageId} is are used by any project.");

            return packages;
        }

        foreach (string id in packageIds)
        {
            if (IsExcluded(packageId: id, excludes: excludes))
            {
                Console.WriteLine($"No updates needed for package {id} as it is excluded.");

                continue;
            }

            await PackageSourceHelpers.FindPackagesAsync(sources: sources, packageId: id, packages: packages, cancellationToken: CancellationToken.None);
        }

        return packages;
    }

    private static void OutputPackageUpdateTags(Dictionary<string, NuGetVersion> updatesMade)
    {
        // Used to tell scripts that the package id has been updated and to what version
        foreach ((string updatedPackageId, NuGetVersion version) in updatesMade)
        {
            Console.WriteLine($"echo ::set-env name={updatedPackageId}::{version}");
        }
    }

    private static Dictionary<string, NuGetVersion> UpdateProjects(IReadOnlyList<string> projects, Dictionary<string, NuGetVersion> packages)
    {
        Dictionary<string, NuGetVersion> updates = new(StringComparer.Ordinal);

        foreach (string project in projects)
        {
            UpdateProject(project: project, packages: packages, updates: updates);
        }

        return updates;
    }

    private static bool HasMatchingPackagesInProjects(IReadOnlyList<string> projects, string packageId)
    {
        return ProjectHelpers.GetPackageIds(projects)
                             .Any(package => PackageIdHelpers.IsExactMatch(packageId: packageId, package: package));
    }

    private static IReadOnlyList<string> FindPackagesByPrefixFromProjects(IReadOnlyList<string> projects, string packageIdPrefix)
    {
        return ProjectHelpers.GetPackageIds(projects)
                             .Where(package => PackageIdHelpers.IsPrefixMatch(packageIdPrefix: packageIdPrefix, package: package))
                             .Select(packageId => packageId.ToLowerInvariant())
                             .Distinct(StringComparer.Ordinal)
                             .ToArray();
    }

    private static void UpdateProject(string project, IReadOnlyDictionary<string, NuGetVersion> packages, Dictionary<string, NuGetVersion> updates)
    {
        XmlDocument? doc = ProjectHelpers.TryLoadDocument(project);

        if (doc == null)
        {
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"* {project}");

        bool sdkUpdated = TryUpdateSdk(doc: doc, packages: packages, updates: updates);
        bool packageReferencesUpdated = TryUpdatePackageReferences(doc: doc, packages: packages, updates: updates);

        if (sdkUpdated || packageReferencesUpdated)
        {
            Console.WriteLine(value: "=========== UPDATED ===========");

            ProjectHelpers.SaveProject(project: project, doc: doc);
        }
    }

    private static bool TryUpdatePackageReferences(XmlDocument doc, IReadOnlyDictionary<string, NuGetVersion> packages, Dictionary<string, NuGetVersion> updates)
    {
        XmlNodeList? nodes = doc.SelectNodes(xpath: "/Project/ItemGroup/PackageReference");

        if (nodes == null || nodes.Count == 0)
        {
            return false;
        }

        bool packageReferencesUpdated = false;

        foreach (XmlElement node in nodes.OfType<XmlElement>())
        {
            string package = node.GetAttribute(name: "Include");

            foreach ((string nugetPackageId, NuGetVersion nugetVersion) in packages)
            {
                if (!PackageIdHelpers.IsExactMatch(package: package, packageId: nugetPackageId))
                {
                    continue;
                }

                bool updated = UpdateOnePackage(node: node, installedPackageId: package, nugetPackageId: nugetPackageId, nugetVersion: nugetVersion);

                if (updated)
                {
                    TrackUpdate(updates: updates, nugetPackageId: nugetPackageId, nugetVersion: nugetVersion);
                    packageReferencesUpdated = true;
                }

                break;
            }
        }

        return packageReferencesUpdated;
    }

    private static bool TryUpdateSdk(XmlDocument doc, IReadOnlyDictionary<string, NuGetVersion> packages, Dictionary<string, NuGetVersion> updates)
    {
        if (doc.SelectSingleNode("/Project") is not XmlElement projectNode)
        {
            return false;
        }

        string sdk = projectNode.GetAttribute("Sdk");

        if (string.IsNullOrWhiteSpace(sdk))
        {
            return false;
        }

        IReadOnlyList<string> parts = sdk.Split("/");

        if (parts.Count != 2)
        {
            return false;
        }

        string installedPackageId = parts[0];
        string installedVersion = parts[1];

        foreach ((string nugetPackageId, NuGetVersion nugetVersion) in packages)
        {
            if (!PackageIdHelpers.IsExactMatch(package: installedPackageId, packageId: nugetPackageId))
            {
                continue;
            }

            bool upgrade = ShouldUpgrade(installedVersion: installedVersion, nugetVersion: nugetVersion);

            if (!upgrade)
            {
                Console.WriteLine($"  >> {installedPackageId} Installed: {installedVersion} Upgrade: False.");

                return false;
            }

            Console.WriteLine($"  >> {installedPackageId} Installed: {installedVersion} Upgrade: True. New Version: {nugetVersion}.");
            string newSdk = string.Join(separator: "/", nugetPackageId, nugetVersion);
            projectNode.SetAttribute(name: "Sdk", value: newSdk);

            TrackUpdate(updates: updates, nugetPackageId: nugetPackageId, nugetVersion: nugetVersion);

            return true;

            // Package matched but was not upgraded.
        }

        return false;
    }

    private static bool UpdateOnePackage(XmlElement node, string installedPackageId, string nugetPackageId, NuGetVersion nugetVersion)
    {
        string installedVersion = node.GetAttribute(name: "Version");
        bool upgrade = ShouldUpgrade(installedVersion: installedVersion, nugetVersion: nugetVersion);

        if (upgrade)
        {
            Console.WriteLine($"  >> {installedPackageId} Installed: {installedVersion} Upgrade: True. New Version: {nugetVersion}.");

            // Set the package Id to be that from nuget
            if (IsPackageIdCasedDifferently(package: installedPackageId, actualName: nugetPackageId))
            {
                node.SetAttribute(name: "Include", value: nugetPackageId);
            }

            node.SetAttribute(name: "Version", nugetVersion.ToString());

            return true;
        }

        Console.WriteLine($"  >> {installedPackageId} Installed: {installedVersion} Upgrade: False.");

        return false;
    }

    private static void TrackUpdate(Dictionary<string, NuGetVersion> updates, string nugetPackageId, NuGetVersion nugetVersion)
    {
        if (!updates.ContainsKey(nugetPackageId))
        {
            updates.Add(key: nugetPackageId, value: nugetVersion);
        }
    }

    private static bool IsPackageIdCasedDifferently(string package, string actualName)
    {
        return StringComparer.InvariantCultureIgnoreCase.Equals(x: package, y: actualName) && !StringComparer.InvariantCultureIgnoreCase.Equals(x: package, y: actualName);
    }

    private static bool ShouldUpgrade(string installedVersion, NuGetVersion nugetVersion)
    {
        if (StringComparer.InvariantCultureIgnoreCase.Equals(x: installedVersion, nugetVersion.ToString()))
        {
            return false;
        }

        NuGetVersion iv = new(installedVersion);
        NuGetVersion ev = nugetVersion;

        return iv < ev;
    }
}