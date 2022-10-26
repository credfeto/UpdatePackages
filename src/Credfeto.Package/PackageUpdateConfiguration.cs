using System.Collections.Generic;
using System.Diagnostics;

namespace Credfeto.Package;

[DebuggerDisplay("Package : {Package.PackageId}")]
public sealed record PackageUpdateConfiguration(ExcludedPackage Package, IReadOnlyList<ExcludedPackage> ExcludedPackages);