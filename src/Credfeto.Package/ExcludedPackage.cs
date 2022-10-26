using System.Diagnostics;

namespace Credfeto.Package;

[DebuggerDisplay("{PackageId} => {Prefix}")]
public sealed record ExcludedPackage(string PackageId, bool Prefix);