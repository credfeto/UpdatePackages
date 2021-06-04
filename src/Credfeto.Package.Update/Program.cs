using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
            new(includePrerelease: false, filter: SearchFilterType.IsLatestVersion) {IncludeDelisted = INCLUDE_UNLISTED_PACKAGES, OrderBy = SearchOrderBy.Id};

        private static readonly ILogger NugetLogger = new NullLogger();

        private static readonly XmlWriterSettings WriterSettings = new()
                                                                   {
                                                                       Async = true,
                                                                       Indent = true,
                                                                       IndentChars = "    ",
                                                                       OmitXmlDeclaration = true,
                                                                       Encoding = Encoding.UTF8,
                                                                       NewLineHandling = NewLineHandling.None,
                                                                       NewLineOnAttributes = false,
                                                                       NamespaceHandling = NamespaceHandling.OmitDuplicates,
                                                                       CloseOutput = true
                                                                   };

        private static async Task<int> Main(string[] args)
        {
            Console.WriteLine($"{typeof(Program).Namespace} {ExecutableVersionInformation.ProgramVersion()}");

            try
            {
                IConfigurationRoot configuration = LoadConfiguration(args);

                Dictionary<string, string> packages = new();

                string folder = configuration.GetValue<string>(key: @"Folder");

                if (string.IsNullOrEmpty(folder))
                {
                    Console.WriteLine("ERROR: folder not specified");

                    return ERROR;
                }

                string source = configuration.GetValue<string>(key: @"source");

                IReadOnlyList<PackageSource> sources = DefinePackageSources(source);

                IReadOnlyList<string> projects = FindProjects(folder);

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

                    foreach (string? id in packageIds)
                    {
                        await FindPackagesAsync(sources: sources, packageId: id, packages: packages, cancellationToken: CancellationToken.None);
                    }
                }
                else
                {
                    await FindPackagesAsync(sources: sources, packageId: packageId, packages: packages, cancellationToken: CancellationToken.None);
                }

                int updates = 0;

                Dictionary<string, string> updatesMade = new();

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

        private static IReadOnlyList<string> FindPackagesByPrefixFromProjects(IReadOnlyList<string> projects, string packageIdPrefix)
        {
            HashSet<string> packages = new();

            foreach (var project in projects)
            {
                XmlDocument? doc = TryLoadDocument(project);

                if (doc == null)
                {
                    continue;
                }

                XmlNodeList? nodes = doc.SelectNodes(xpath: "/Project/ItemGroup/PackageReference");

                if (nodes != null)
                {
                    foreach (XmlElement node in nodes.OfType<XmlElement>())
                    {
                        string package = node.GetAttribute(name: "Include");

                        if (IsPrefixMatch(packageIdPrefix: packageIdPrefix, package: package))
                        {
                            packages.Add(package.ToLowerInvariant());
                        }
                    }
                }
            }

            return packages.ToArray();
        }

        private static bool IsPrefixMatch(string packageIdPrefix, string package)
        {
            return StringComparer.InvariantCultureIgnoreCase.Equals(packageIdPrefix, package) ||
                   	package.StartsWith(packageIdPrefix + ".", comparisonType: StringComparison.OrdinalIgnoreCase);
        }

        private static string[] FindProjects(string folder)
        {
            return Directory.EnumerateFiles(path: folder, searchPattern: "*.csproj", searchOption: SearchOption.AllDirectories)
                            .ToArray();
        }

        private static List<PackageSource> DefinePackageSources(string source)
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

        private static IConfigurationRoot LoadConfiguration(string[] args)
        {
            Dictionary<string, string> mappings = new() {[@"-packageId"] = @"packageid", ["-packageprefix"] = "packageprefix", [@"-folder"] = @"folder", [@"-source"] = @"source"};

            return new ConfigurationBuilder().AddCommandLine(args: args, switchMappings: mappings)
                                             .Build();
        }

        private static async Task FindPackagesAsync(IReadOnlyList<PackageSource> sources, string packageId, Dictionary<string, string> packages, CancellationToken cancellationToken)
        {
            Console.WriteLine(value: $"Enumerating matching package versions for {packageId}...");

            ConcurrentDictionary<string, string> found = new();

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
            SourceRepository sourceRepository = new(source: packageSource, new List<Lazy<INuGetResourceProvider>>(Repository.Provider.GetCoreV3()));

            PackageSearchResource searcher = await sourceRepository.GetResourceAsync<PackageSearchResource>(cancellationToken);
            IEnumerable<IPackageSearchMetadata> result =
                await searcher.SearchAsync(searchTerm: packageId, filters: SearchFilter, log: NugetLogger, cancellationToken: cancellationToken, skip: 0, take: int.MaxValue);

            foreach (IPackageSearchMetadata entry in result)
            {
                PackageVersion packageVersion = new(packageId: entry.Identity.Id, entry.Identity.Version.ToString());

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
            XmlNodeList? nodes = doc.SelectNodes(xpath: "/Project/ItemGroup/PackageReference");

            if (nodes != null && nodes.Count != 0)
            {
                Console.WriteLine();
                Console.WriteLine($"* {project}");

                foreach (XmlElement node in nodes.OfType<XmlElement>())
                {
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

                    using (XmlWriter writer = XmlWriter.Create(outputFileName: project, settings: WriterSettings))
                    {
                        doc.Save(writer);
                    }
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
                NuGetVersion iv = new(installedVersion);
                NuGetVersion ev = new(nugetVersion);

                return iv < ev;
            }

            return false;
        }

        private static XmlDocument? TryLoadDocument(string project)
        {
            try
            {
                XmlDocument doc = new();

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
