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

namespace UpdatePackages
{
    internal static class Program
    {
        private const int SUCCESS = 0;
        private const int ERROR = 1;

        private const bool INCLUDE_UNLISTED_PACKAGES = false;

        private static readonly SearchFilter SearchFilter =
            new SearchFilter(includePrerelease: false, SearchFilterType.IsLatestVersion) {IncludeDelisted = INCLUDE_UNLISTED_PACKAGES, OrderBy = SearchOrderBy.Id};

        private static readonly ILogger NugetLogger = new NullLogger();

        private static async Task<int> Main(string[] args)
        {
            try
            {
                IConfigurationRoot configuration = new ConfigurationBuilder()
                    .AddCommandLine(args, new Dictionary<string, string> {{@"-prefix", @"prefix"}, {@"-version", @"version"}, {@"-folder", @"folder"}, {@"-source", @"source"}})
                    .Build();

                Dictionary<string, string> packages = new Dictionary<string, string>();

                string folder = configuration.GetValue<string>(key: @"Folder");

                string prefix = configuration.GetValue<string>(key: @"Prefix");

                string version = configuration.GetValue<string>(key: @"Version");

                string source = configuration.GetValue<string>(key: @"source");

                PackageSourceProvider packageSourceProvider = new PackageSourceProvider(Settings.LoadDefaultSettings(Environment.CurrentDirectory));

                List<PackageSource> sources = packageSourceProvider.LoadPackageSources()
                    .ToList();

                if (!string.IsNullOrEmpty(source))
                {
                    sources.Add(new PackageSource(name: "Custom", source: source, isEnabled: true, isPersistable: true, isOfficial: true));
                }

                bool fromNuget = false;

                if (string.IsNullOrWhiteSpace(version))
                {
                    await FindPackages(sources, prefix, packages, CancellationToken.None);
                    fromNuget = true;
                }
                else
                {
                    packages.Add(prefix, version);
                }

                IEnumerable<string> projects = Directory.EnumerateFiles(folder, searchPattern: "*.csproj", SearchOption.AllDirectories);

                int updates = 0;

                Dictionary<string, string> updatesMade = new Dictionary<string, string>();

                foreach (string project in projects)
                {
                    updates += UpdateProject(project, packages, fromNuget, updatesMade);
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

        private static async Task FindPackages(List<PackageSource> sources, string prefix, Dictionary<string, string> packages, CancellationToken cancellationToken)
        {
            async Task LoadPackagesFromSource(PackageSource packageSource, ConcurrentDictionary<string, string> concurrentDictionary)
            {
                SourceRepository sourceRepository = new SourceRepository(packageSource, new List<Lazy<INuGetResourceProvider>>(Repository.Provider.GetCoreV3()));

                PackageSearchResource searcher = await sourceRepository.GetResourceAsync<PackageSearchResource>(cancellationToken);
                IEnumerable<IPackageSearchMetadata> result = await searcher.SearchAsync(prefix, SearchFilter, log: NugetLogger, cancellationToken: cancellationToken, skip: 0, take: int.MaxValue);

                foreach (IPackageSearchMetadata entry in result)
                {
                    PackageVersion packageVersion = new PackageVersion(entry.Identity.Id, entry.Identity.Version.ToString());

                    if (Matches(prefix, packageVersion) && !IsBannedPackage(packageVersion))
                    {
                        concurrentDictionary.TryAdd(packageVersion.PackageId, packageVersion.Version);
                    }
                }
            }

            Console.WriteLine(value: "Enumerating matching packages...");

            ConcurrentDictionary<string, string> found = new ConcurrentDictionary<string, string>();

            await Task.WhenAll(sources.Select(selector: source => LoadPackagesFromSource(source, found)));

            foreach (KeyValuePair<string, string> item in found)
            {
                packages.TryAdd(item.Key, item.Value);
            }
        }

        private static bool Matches(string prefix, PackageVersion packageVersion)
        {
            return IsMatch(packageVersion.PackageId, prefix);
        }

        private static bool IsBannedPackage(PackageVersion packageVersion)
        {
            return packageVersion.Version.Contains(value: "+", StringComparison.Ordinal) || StringComparer.InvariantCultureIgnoreCase.Equals(packageVersion.PackageId, y: "Nuget.Version");
        }

        private static int UpdateProject(string project, Dictionary<string, string> packages, bool fromNuget, Dictionary<string, string> updatesMade)
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

                    foreach (KeyValuePair<string, string> entry in packages)
                    {
                        if (IsMatch(package, entry.Key))
                        {
                            string installedVersion = node.GetAttribute(name: "Version");
                            bool upgrade = ShouldUpgrade(installedVersion, entry);

                            if (upgrade)
                            {
                                Console.WriteLine($"  >> {package} Installed: {installedVersion} Upgrade: True. New Version: {entry.Value}.");

                                // Set the package Id to be that from nuget
                                if (fromNuget && StringComparer.InvariantCultureIgnoreCase.Equals(package, entry.Key) && !StringComparer.InvariantCultureIgnoreCase.Equals(package, entry.Key))
                                {
                                    node.SetAttribute(name: "Include", entry.Key);
                                }

                                node.SetAttribute(name: "Version", entry.Value);
                                changes++;
                                updatesMade.TryAdd(entry.Key, entry.Value);
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

        private static bool ShouldUpgrade(string installedVersion, KeyValuePair<string, string> entry)
        {
            if (!StringComparer.InvariantCultureIgnoreCase.Equals(installedVersion, entry.Value))
            {
                NuGetVersion iv = new NuGetVersion(installedVersion);
                NuGetVersion ev = new NuGetVersion(entry.Value);

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

        private static bool IsMatch(string package, string prefix)
        {
            return package.Equals(prefix, StringComparison.OrdinalIgnoreCase);
        }
    }
}