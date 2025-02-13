using System;
using System.Diagnostics;

namespace Credfeto.Package;

[DebuggerDisplay("{PackageId} => {Prefix}")]
public sealed record PackageMatch(string PackageId, bool Prefix)
{
    public bool IsMatchingPackage(PackageVersion packageVersion)
    {
        if (this.Prefix)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(
                    x: this.PackageId,
                    y: packageVersion.PackageId
                )
                || packageVersion.PackageId.StartsWith(
                    this.PackageId + ".",
                    comparisonType: StringComparison.OrdinalIgnoreCase
                );
        }

        return StringComparer.OrdinalIgnoreCase.Equals(
            x: this.PackageId,
            y: packageVersion.PackageId
        );
    }
}
