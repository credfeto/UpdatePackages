using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;
using Microsoft.Extensions.Configuration;

namespace UpdatePackages
{
    internal class Program
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

                string folder = configuration.GetValue<string>(@"Folder");

                string prefix = configuration.GetValue<string>(@"Prefix");

                string version = configuration.GetValue<string>(@"Version");

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

                IEnumerable<string> projects = Directory.EnumerateFiles(folder, "*.csproj", SearchOption.AllDirectories);

                foreach (string project in projects) UpdateProject(project, packages, fromNuget);

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
            Console.WriteLine("Enumerating matching packages...");

            ProcessStartInfo psi = new ProcessStartInfo("nuget.exe", "list " + prefix) {RedirectStandardOutput = true, CreateNoWindow = true};

            using (Process p = Process.Start(psi))
            {
                if (p == null) throw new Exception($"ERROR: Could not execute {psi.FileName} {psi.Arguments}");

                StreamReader s = p.StandardOutput;
                while (!s.EndOfStream)
                {
                    string line = p.StandardOutput.ReadLine();

                    PackageVersion packageVersion = ExtractPackageVersion(line);
                    if (packageVersion != null) packages.Add(packageVersion.PackageId, packageVersion.Version);
                }

                p.WaitForExit();
            }
        }

        private static PackageVersion ExtractPackageVersion(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return null;

            string[] pv = line.Trim()
                .Split(' ');
            if (pv.Length != 2) return null;

            return new PackageVersion(pv[0], pv[1]);
        }

        private static void UpdateProject(string project, Dictionary<string, string> packages, bool fromNuget)
        {
            XmlDocument doc = TryLoadDocument(project);
            if (doc == null) return;

            XmlNodeList nodes = doc.SelectNodes("/Project/ItemGroup/PackageReference");
            if (nodes.Count > 0)
            {
                Console.WriteLine($"* {project}");
                bool changed = false;
                foreach (XmlElement node in nodes)
                {
                    string package = node.GetAttribute("Include");
                    foreach (KeyValuePair<string, string> entry in packages)
                        if (IsMatch(package, entry.Key))
                        {
                            string installedVersion = node.GetAttribute("Version");
                            bool upgrade = !StringComparer.InvariantCultureIgnoreCase.Equals(installedVersion, entry.Value);
                            Console.WriteLine($"  >> {package} Installed: {installedVersion} Upgrade: {upgrade}");

                            if (upgrade)
                            {
                                // Set the package Id to be that from nuget
                                if (fromNuget && StringComparer.InvariantCultureIgnoreCase.Equals(package, entry.Key) &&
                                    !StringComparer.InvariantCultureIgnoreCase.Equals(package, entry.Key)) node.SetAttribute("Include", entry.Key);

                                node.SetAttribute("Version", entry.Value);
                                changed = true;
                            }

                            break;
                        }
                }

                if (changed)
                {
                    Console.WriteLine($"  === UPDATED ===");
                    doc.Save(project);
                }
            }
        }

        private static XmlDocument TryLoadDocument(string project)
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
            return package.Equals(prefix, StringComparison.OrdinalIgnoreCase) || package.StartsWith(prefix + ".", StringComparison.OrdinalIgnoreCase);
        }
    }
}