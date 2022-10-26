using System.Diagnostics;

namespace Credfeto.Package.Update;

[DebuggerDisplay("{PackageId} => {Prefix}")]
internal sealed record ExcludedPackage(string PackageId, bool Prefix);