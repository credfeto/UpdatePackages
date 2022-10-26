using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Credfeto.Package.Update.Services;

internal sealed class Project
{
    private readonly XmlDocument _doc;

    public Project(XmlDocument doc)
    {
        this._doc = doc;
    }

    public IReadOnlyList<PackageVersion> Packages => this.GetCurrentPackageVersions();

    private IReadOnlyList<PackageVersion> GetCurrentPackageVersions()
    {
        IEnumerable<PackageVersion> refPackages = GetPackagesFromReferences(this._doc);
        IEnumerable<PackageVersion> sdkPackages = GetPackagesFromSdk(this._doc);

        return refPackages.Concat(sdkPackages)
                          .OrderBy(x => x.PackageId)
                          .ThenBy(x => x.Version)
                          .ToArray();
    }

    private static IEnumerable<PackageVersion> GetPackagesFromReferences(XmlDocument doc)
    {
        IEnumerable<XmlElement> references = doc.SelectNodes("/Project/ItemGroup/PackageReference")
                                                ?.OfType<XmlElement>()
                                                .RemoveNulls() ?? Array.Empty<XmlElement>();

        foreach (XmlElement node in references)
        {
            string packageId = node.GetAttribute("Include");
            string version = node.GetAttribute("Version");

            if (string.IsNullOrWhiteSpace(packageId) || string.IsNullOrWhiteSpace(version))
            {
                continue;
            }

            yield return new(packageId: packageId, new(version));
        }
    }

    private static IEnumerable<PackageVersion> GetPackagesFromSdk(XmlDocument doc)
    {
        if (doc.SelectSingleNode("/Project") is not XmlElement project)
        {
            yield break;
        }

        IReadOnlyList<string> sdk = project.GetAttribute("Sdk")
                                           .Split("/");

        if (sdk.Count == 2)
        {
            yield return new(sdk[0], new(sdk[1]));
        }
    }
}