using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Microsoft.Extensions.Configuration;

namespace UpdatePackages
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .AddCommandLine(args, new Dictionary<string, string> {{@"-prefix", @"prefix"}, {@"-version", @"version"}, {@"-folder", @"folder"}})
                .Build();

            string folder = configuration.GetValue<string>(@"Folder");

            string prefix = configuration.GetValue<string>(@"Prefix");

            string version = configuration.GetValue<string>(@"Version");

            IEnumerable<string> projects = Directory.EnumerateFiles(folder, "*.csproj", SearchOption.AllDirectories);

            foreach (string project in projects) UpdateProject(project, prefix, version);
        }

        private static void UpdateProject(string project, string prefix, string version)
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
                    if (IsMatch(package, prefix))
                    {
                        string installedVersion = node.GetAttribute("Version");
                        bool upgrade = installedVersion != version;
                        Console.WriteLine($"  >> {package} Installed: {installedVersion} Upgrade: {upgrade}");

                        if (upgrade)
                        {
                            node.SetAttribute("Version", version);
                            changed = true;
                        }
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