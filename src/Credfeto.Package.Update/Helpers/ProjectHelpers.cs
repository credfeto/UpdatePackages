using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace Credfeto.Package.Update.Helpers
{
    internal static class ProjectHelpers
    {
        public static readonly XmlWriterSettings WriterSettings = new()
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

        public static string[] FindProjects(string folder)
        {
            return Directory.EnumerateFiles(path: folder, searchPattern: "*.csproj", searchOption: SearchOption.AllDirectories)
                            .ToArray();
        }

        public static IEnumerable<string> GetPackageIds(IReadOnlyList<string> projects)
        {
            return projects.Select(TryLoadDocument)
                           .Where(doc => doc != null)
                           .Select(doc => doc!.SelectNodes(xpath: "/Project/ItemGroup/PackageReference"))
                           .Where(nodes => nodes != null)
                           .SelectMany(nodes => nodes!.OfType<XmlElement>())
                           .Select(node => node.GetAttribute(name: "Include"));
        }

        public static XmlDocument? TryLoadDocument(string project)
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
    }
}