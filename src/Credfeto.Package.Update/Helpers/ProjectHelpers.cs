using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Credfeto.Package.Update.Extensions;

namespace Credfeto.Package.Update.Helpers;

internal static class ProjectHelpers
{
    private static readonly XmlWriterSettings WriterSettings = new()
                                                               {
                                                                   Async = true,
                                                                   Indent = true,
                                                                   IndentChars = "  ",
                                                                   OmitXmlDeclaration = true,
                                                                   Encoding = Encoding.UTF8,
                                                                   NewLineHandling = NewLineHandling.None,
                                                                   NewLineOnAttributes = false,
                                                                   NamespaceHandling = NamespaceHandling.OmitDuplicates,
                                                                   CloseOutput = true
                                                               };

    public static IReadOnlyList<string> FindProjects(string folder)
    {
        return Directory.EnumerateFiles(path: folder, searchPattern: "*.csproj", searchOption: SearchOption.AllDirectories)
                        .ToArray();
    }

    public static IEnumerable<string> GetPackageIds(IReadOnlyList<string> projects)
    {
        return projects.Select(TryLoadDocument)
                       .RemoveNulls()
                       .SelectMany(doc => GetPackagesFromReferences(doc)
                                       .Concat(GetPackagesFromSdk(doc)))
                       .Select(item => item.ToLowerInvariant())
                       .Distinct(StringComparer.Ordinal);
    }

    private static IEnumerable<string> GetPackagesFromSdk(XmlDocument doc)
    {
        if (doc.SelectSingleNode("/Project") is not XmlElement project)
        {
            yield break;
        }

        IReadOnlyList<string> sdk = project.GetAttribute("Sdk")
                                           .Split("/");

        if (sdk.Count == 2)
        {
            yield return sdk[0];
        }
    }

    private static IEnumerable<string> GetPackagesFromReferences(XmlDocument doc)
    {
        return doc.SelectNodes("/Project/ItemGroup/PackageReference")
                  ?.OfType<XmlElement>()
                  .RemoveNulls()
                  .Select(node => node.GetAttribute("Include"))
                  .Where(include => !string.IsNullOrWhiteSpace(include)) ?? Array.Empty<string>();
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

    public static void SaveProject(string project, XmlDocument doc)
    {
        using (XmlWriter writer = XmlWriter.Create(outputFileName: project, settings: WriterSettings))
        {
            doc.Save(writer);
        }
    }
}