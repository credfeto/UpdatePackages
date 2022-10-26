using System.Collections.Generic;

namespace Credfeto.Package.Update.Services;

public interface IProject
{
    IReadOnlyList<PackageVersion> Packages { get; }
}