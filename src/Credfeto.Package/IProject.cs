using System.Collections.Generic;

namespace Credfeto.Package;

public interface IProject
{
    IReadOnlyList<PackageVersion> Packages { get; }

    bool Changed { get; }

    void UpdatePackage(PackageVersion package);

    bool Save();
}