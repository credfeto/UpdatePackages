using System.Collections.Generic;
using System.Diagnostics;

namespace Credfeto.Package;

[DebuggerDisplay("Package : {PackageMatch.PackageId}")]
public sealed record PackageUpdateConfiguration(PackageMatch PackageMatch, IReadOnlyList<PackageMatch> ExcludedPackages);