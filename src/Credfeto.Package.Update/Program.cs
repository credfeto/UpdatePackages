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

namespace Credfeto.Package.Update
{
    internal static class Program
    {
        private const int SUCCESS = 0;
        private const int ERROR = 1;

        private static async Task<int> Main(string[] args)
        {
            Console.WriteLine($"{typeof(Program).Namespace} {ExecutableVersionInformation.ProgramVersion()}");

            try
            {
                IConfigurationRoot configuration = ConfigurationLoader.LoadConfiguration(args);

                Dictionary<string, NuGetVersion> packages = new();

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

                if (string.IsNullOrEmpty(packageId))
                {
                    string prefix = configuration.GetValue<string>(key: @"packageprefix");

                    if (string.IsNullOrWhiteSpace(prefix))
                    {
                        Console.WriteLine("ERROR: neither packageid or packageprefix specified");

                        return ERROR;
                    }

                    IReadOnlyList<string> packageIds = FindPackagesByPrefixFromProjects(projects: projects, packageIdPrefix: prefix);

                    if (packageIds.Count == 0)
                    {
                        Console.WriteLine("No matching packages used by any project");

                        return SUCCESS;
                    }

                    foreach (string id in packageIds)
                    {
                        await PackageSourceHelpers.FindPackagesAsync(sources: sources, packageId: id, packages: packages, cancellationToken: CancellationToken.None);
                    }
                }
                else
                {
                    if (!HasMatchingPackagesInProjects(projects: projects, packageId: packageId))
                    {
                        Console.WriteLine("No matching packages used by any project");

                        return SUCCESS;
                    }

                    await PackageSourceHelpers.FindPackagesAsync(sources: sources, packageId: packageId, packages: packages, cancellationToken: CancellationToken.None);
                }

                int updates = UpdateProjects(projects: projects, packages: packages);

                Console.WriteLine();

                if (updates > 0)
                {
                    Console.WriteLine($"Total Updates: {updates}");

                    return SUCCESS;
                }

                Console.WriteLine(value: "No updates made.");

                return ERROR;
            }
            catch (Exception exception)
            {
                Console.WriteLine($"ERROR: {exception.Message}");

                return ERROR;
            }
        }

        private static int UpdateProjects(IReadOnlyList<string> projects, Dictionary<string, NuGetVersion> packages)
        {
            return projects.Sum(project => UpdateProject(project: project, packages: packages));
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
                                 .Distinct()
                                 .ToArray();
        }

        private static int UpdateProject(string project, IReadOnlyDictionary<string, NuGetVersion> packages)
        {
            XmlDocument? doc = ProjectHelpers.TryLoadDocument(project);

            if (doc == null)
            {
                return 0;
            }

            int changes = 0;
            XmlNodeList? nodes = doc.SelectNodes(xpath: "/Project/ItemGroup/PackageReference");

            if (nodes != null && nodes.Count != 0)
            {
                Console.WriteLine();
                Console.WriteLine($"* {project}");

                foreach (XmlElement node in nodes.OfType<XmlElement>())
                {
                    string package = node.GetAttribute(name: "Include");

                    foreach ((string nugetPackageId, NuGetVersion nugetVersion) in packages)
                    {
                        if (PackageIdHelpers.IsExactMatch(package: package, packageId: nugetPackageId))
                        {
                            string installedVersion = node.GetAttribute(name: "Version");
                            bool upgrade = ShouldUpgrade(installedVersion: installedVersion, nugetVersion: nugetVersion);

                            if (upgrade)
                            {
                                Console.WriteLine($"  >> {package} Installed: {installedVersion} Upgrade: True. New Version: {nugetVersion}.");

                                // Set the package Id to be that from nuget
                                if (IsPackageIdCasedDifferently(package: package, actualName: nugetPackageId))
                                {
                                    node.SetAttribute(name: "Include", value: nugetPackageId);
                                }

                                node.SetAttribute(name: "Version", nugetVersion.ToString());
                                changes++;
                            }
                            else
                            {
                                Console.WriteLine($"  >> {package} Installed: {installedVersion} Upgrade: False.");
                            }

                            break;
                        }
                    }
                }

                if (changes != 0)
                {
                    Console.WriteLine(value: "=========== UPDATED ===========");

                    ProjectHelpers.SaveProject(project: project, doc: doc);
                }
            }

            return changes;
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
}