using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Extensions.Configuration;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Credfeto.Package.Update
{
    internal static class Program
    {
        private const int SUCCESS = 0;
        private const int ERROR = 1;

        private const bool INCLUDE_UNLISTED_PACKAGES = false;

        private static readonly SearchFilter SearchFilter =
            new SearchFilter(includePrerelease: false, filter: SearchFilterType.IsLatestVersion) {IncludeDelisted = INCLUDE_UNLISTED_PACKAGES, OrderBy = SearchOrderBy.Id};

        private static readonly ILogger NugetLogger = new NullLogger();

        private static async Task<int> Main(string[] args)
        {
            Console.WriteLine($"{typeof(Program).Namespace} {ExecutableVersionInformation.ProgramVersion()}");

            try
            {
                IConfigurationRoot configuration = LoadConfiguration(args);

                Dictionary<string, string> packages = new Dictionary<string, string>();

                string folder = configuration.GetValue<string>(key: @"Folder");

                if (string.IsNullOrEmpty(folder))
                {
                    Console.WriteLine("ERROR: folder not specified");

                    return ERROR;
                }

                string packageId = configuration.GetValue<string>(key: @"packageid");

                if (string.IsNullOrEmpty(packageId))
                {
                    Console.WriteLine("ERROR: packageid not specified");

                    return ERROR;
                }

                // string version = configuration.GetValue<string>(key: @"Version");

                string source = configuration.GetValue<string>(key: @"source");

                PackageSourceProvider packageSourceProvider = new PackageSourceProvider(Settings.LoadDefaultSettings(Environment.CurrentDirectory));

                List<PackageSource> sources = packageSourceProvider.LoadPackageSources()
                                                                   .ToList();

                if (!string.IsNullOrEmpty(source))
                {
                    sources.Add(new PackageSource(name: "Custom", source: source, isEnabled: true, isPersistable: true, isOfficial: true));
                }

                await FindPackagesAsync(sources: sources, packageId: packageId, packages: packages, cancellationToken: CancellationToken.None);

                IEnumerable<string> projects = Directory.EnumerateFiles(path: folder, searchPattern: "*.csproj", searchOption: SearchOption.AllDirectories);

                int updates = 0;

                Dictionary<string, string> updatesMade = new Dictionary<string, string>();

                foreach (string project in projects)
                {
                    updates += UpdateProject(project: project, packages: packages, updatesMade: updatesMade);
                }

                Console.WriteLine();

                if (updates > 0)
                {
                    Console.WriteLine($"Total Updates: {updates}");

                    foreach (KeyValuePair<string, string> update in updatesMade)
                    {
                        Console.WriteLine($"echo ::set-env name={update.Key}::{update.Value}");
                    }

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

        private static IConfigurationRoot LoadConfiguration(string[] args)
        {
            return new ConfigurationBuilder().AddCommandLine(args: args, new Dictionary<string, string> {{@"-packageId", @"packageid"}, {@"-folder", @"folder"}, {@"-source", @"source"}})
                                             .Build();
        }

        private static async Task FindPackagesAsync(List<PackageSource> sources, string packageId, Dictionary<string, string> packages, CancellationToken cancellationToken)
        {
            Console.WriteLine(value: "Enumerating matching packages...");

            ConcurrentDictionary<string, string> found = new ConcurrentDictionary<string, string>();

            await Task.WhenAll(sources.Select(selector: source => LoadPackagesFromSourceAsync(packageSource: source,
                                                                                              packageId: packageId,
                                                                                              concurrentDictionary: found,
                                                                                              cancellationToken: cancellationToken)));

            foreach (KeyValuePair<string, string> item in found)
            {
                packages.TryAdd(key: item.Key, value: item.Value);
            }
        }

        private static async Task LoadPackagesFromSourceAsync(PackageSource packageSource,
                                                              string packageId,
                                                              ConcurrentDictionary<string, string> concurrentDictionary,
                                                              CancellationToken cancellationToken)
        {
            SourceRepository sourceRepository = new SourceRepository(source: packageSource, new List<Lazy<INuGetResourceProvider>>(Repository.Provider.GetCoreV3()));

            PackageSearchResource searcher = await sourceRepository.GetResourceAsync<PackageSearchResource>(cancellationToken);
            IEnumerable<IPackageSearchMetadata> result =
                await searcher.SearchAsync(searchTerm: packageId, filters: SearchFilter, log: NugetLogger, cancellationToken: cancellationToken, skip: 0, take: int.MaxValue);

            foreach (IPackageSearchMetadata entry in result)
            {
                PackageVersion packageVersion = new PackageVersion(packageId: entry.Identity.Id, entry.Identity.Version.ToString());

                if (IsExactMatch(packageId: packageId, packageVersion: packageVersion) && !IsBannedPackage(packageVersion))
                {
                    concurrentDictionary.TryAdd(key: packageVersion.PackageId, value: packageVersion.Version);
                }
            }
        }

        private static bool IsExactMatch(string packageId, PackageVersion packageVersion)
        {
            return IsExactMatch(package: packageVersion.PackageId, packageId: packageId);
        }

        private static bool IsBannedPackage(PackageVersion packageVersion)
        {
            return packageVersion.Version.Contains(value: "+", comparisonType: StringComparison.Ordinal);
        }

        private static int UpdateProject(string project, Dictionary<string, string> packages, Dictionary<string, string> updatesMade)
        {
            XmlDocument? doc = TryLoadDocument(project);

            if (doc == null)
            {
                return 0;
            }

            int changes = 0;
            XmlNodeList nodes = doc.SelectNodes(xpath: "/Project/ItemGroup/PackageReference");

            if (nodes.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine($"* {project}");

                foreach (XmlElement? node in nodes)
                {
                    if (node == null)
                    {
                        continue;
                    }

                    string package = node.GetAttribute(name: "Include");

                    foreach ((string nugetPackageId, string nugetVersion) in packages)
                    {
                        if (IsExactMatch(package: package, packageId: nugetPackageId))
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

                                node.SetAttribute(name: "Version", value: nugetVersion);
                                changes++;
                                updatesMade.TryAdd(key: nugetPackageId, value: nugetVersion);
                            }
                            else
                            {
                                Console.WriteLine($"  >> {package} Installed: {installedVersion} Upgrade: False.");
                            }

                            break;
                        }
                    }
                }

                if (changes > 0)
                {
                    Console.WriteLine(value: "=========== UPDATED ===========");
                    doc.Save(project);
                }
            }

            return changes;
        }

        private static bool IsPackageIdCasedDifferently(string package, string actualName)
        {
            return StringComparer.InvariantCultureIgnoreCase.Equals(x: package, y: actualName) && !StringComparer.InvariantCultureIgnoreCase.Equals(x: package, y: actualName);
        }

        private static bool ShouldUpgrade(string installedVersion, string nugetVersion)
        {
            if (!StringComparer.InvariantCultureIgnoreCase.Equals(x: installedVersion, y: nugetVersion))
            {
                NuGetVersion iv = new NuGetVersion(installedVersion);
                NuGetVersion ev = new NuGetVersion(nugetVersion);

                return iv < ev;
            }

            return false;
        }

        private static XmlDocument? TryLoadDocument(string project)
        {
            try
            {
                XmlDocument doc = new XmlDocument();

                doc.Load(project);

                return doc;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"Failed to load {project}: {exception.Message}");

                return null;
            }
        }

        private static bool IsExactMatch(string package, string packageId)
        {
            return package.Equals(value: packageId, comparisonType: StringComparison.OrdinalIgnoreCase);
        }
    }
}