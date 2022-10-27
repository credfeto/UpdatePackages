using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Credfeto.Package.Extensions;
using NuGet.Versioning;

namespace Credfeto.Package.Services;

internal sealed class Project : IProject
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

    private readonly XmlDocument _doc;

    public Project(string fileName, XmlDocument doc)
    {
        this.FileName = fileName;
        this._doc = doc;
        this.Changed = false;
    }

    public string FileName { get; }

    public IReadOnlyList<PackageVersion> Packages => this.GetCurrentPackageVersions();

    public bool Changed { get; private set; }

    public void UpdatePackage(PackageVersion package)
    {
        if (this.Packages.All(p => p.PackageId != package.PackageId))
        {
            return;
        }

        this.UpdatePackageFromReference(package);
        this.UpdatePackageFromSdk(package);
    }

    public bool Save()
    {
        if (!this.Changed)
        {
            return false;
        }

        using (XmlWriter writer = XmlWriter.Create(outputFileName: this.FileName, settings: WriterSettings))
        {
            this._doc.Save(writer);

            return true;
        }
    }

    private void UpdatePackageFromReference(PackageVersion package)
    {
        IEnumerable<XmlElement> references = this._doc.SelectNodes("/Project/ItemGroup/PackageReference")
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

            if (ShouldUpdate(package: package, packageId: packageId, version: version))
            {
                node.SetAttribute(name: "Include", value: package.PackageId);
                node.SetAttribute(name: "Version", package.Version.ToString());
                this.Changed = true;
            }
        }
    }

    private void UpdatePackageFromSdk(PackageVersion package)
    {
        if (this._doc.SelectSingleNode("/Project") is not XmlElement project)
        {
            return;
        }

        IReadOnlyList<string> sdk = project.GetAttribute("Sdk")
                                           .Split("/");

        if (sdk.Count != 2)
        {
            return;
        }

        if (ShouldUpdate(package: package, sdk[0], sdk[1]))
        {
            project.SetAttribute(name: "Sdk", $"{package.PackageId}/{package.Version}");
            this.Changed = true;
        }
    }

    private static bool ShouldUpdate(PackageVersion package, string packageId, string version)
    {
        if (!StringComparer.InvariantCultureIgnoreCase.Equals(x: packageId, y: package.PackageId))
        {
            return false;
        }

        if (!NuGetVersion.TryParse(value: version, out NuGetVersion? existingVersion))
        {
            return false;
        }

        if (package.Version <= existingVersion)
        {
            return false;
        }

        return true;
    }

    private IReadOnlyList<PackageVersion> GetCurrentPackageVersions()
    {
        IEnumerable<PackageVersion> refPackages = this.GetPackagesFromReferences();
        IEnumerable<PackageVersion> sdkPackages = this.GetPackagesFromSdk();

        return refPackages.Concat(sdkPackages)
                          .OrderBy(x => x.PackageId)
                          .ThenBy(x => x.Version)
                          .ToArray();
    }

    private IEnumerable<PackageVersion> GetPackagesFromReferences()
    {
        IEnumerable<XmlElement> references = this._doc.SelectNodes("/Project/ItemGroup/PackageReference")
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

    private IEnumerable<PackageVersion> GetPackagesFromSdk()
    {
        if (this._doc.SelectSingleNode("/Project") is not XmlElement project)
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