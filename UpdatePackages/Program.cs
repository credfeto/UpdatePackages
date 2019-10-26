using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;
using Microsoft.Extensions.Configuration;
using NuGet.Versioning;

namespace UpdatePackages
{
    internal static class Program
    {
        private const int SUCCESS = 0;
        private const int ERROR = 1;

        private static int Main(string[] args)
        {
            try
            {
                IConfigurationRoot configuration = new ConfigurationBuilder()
                    .AddCommandLine(args, new Dictionary<string, string> {{@"-prefix", @"prefix"}, {@"-version", @"version"}, {@"-folder", @"folder"}})
                    .Build();

                Dictionary<string, string> packages = new Dictionary<string, string>();

                string folder = configuration.GetValue<string>(key: @"Folder");

                string prefix = configuration.GetValue<string>(key: @"Prefix");

                string version = configuration.GetValue<string>(key: @"Version");

                bool fromNuget = false;

                if (string.IsNullOrWhiteSpace(version))
                {
                    FindPackages(prefix, packages);
                    fromNuget = true;
                }
                else
                {
                    packages.Add(prefix, version);
                }

                IEnumerable<string> projects = Directory.EnumerateFiles(folder, searchPattern: "*.csproj", SearchOption.AllDirectories);

                int updates = 0;

                foreach (string project in projects)
                {
                    updates += UpdateProject(project, packages, fromNuget);
                }

                Console.WriteLine();

                if (updates > 0)
                {
                    Console.WriteLine($"Total Updates: {updates}");
                }
                else
                {
                    Console.WriteLine(value: "No updates made.");
                }

                return SUCCESS;
            }
            catch (Exception exception)
            {
                Console.WriteLine($"ERROR: {exception.Message}");

                return ERROR;
            }
        }

        private static void FindPackages(string prefix, Dictionary<string, string> packages)
        {
            Console.WriteLine(value: "Enumerating matching packages...");

            ProcessStartInfo psi = new ProcessStartInfo(fileName: "nuget.exe", "list " + prefix) {RedirectStandardOutput = true, CreateNoWindow = true};

            using (Process p = Process.Start(psi))
            {
                if (p == null)
                {
                    throw new NotSupportedException($"ERROR: Could not execute {psi.FileName} {psi.Arguments}");
                }

                StreamReader s = p.StandardOutput;

                while (!s.EndOfStream)
                {
                    string? line = p.StandardOutput.ReadLine();

                    PackageVersion? packageVersion = ExtractPackageVersion(line);

                    if (packageVersion != null && Matches(prefix, packageVersion) && !IsBannedPackage(packageVersion))
                    {
                        packages.TryAdd(packageVersion.PackageId, packageVersion.Version);
                    }
                }

                p.WaitForExit();
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

        private static PackageVersion? ExtractPackageVersion(string? line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return null;
            }

            string[] pv = line.Trim()
                .Split(separator: ' ');

            if (pv.Length != 2)
            {
                return null;
            }

            return new PackageVersion(pv[0], pv[1]);
        }

        private static int UpdateProject(string project, Dictionary<string, string> packages, bool fromNuget)
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