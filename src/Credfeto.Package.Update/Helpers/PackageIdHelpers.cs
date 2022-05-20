using System;

namespace Credfeto.Package.Update.Helpers;

internal static class PackageIdHelpers
{
    public static bool IsPrefixMatch(string packageIdPrefix, string package)
    {
        return StringComparer.InvariantCultureIgnoreCase.Equals(x: packageIdPrefix, y: package) ||
               package.StartsWith(packageIdPrefix + ".", comparisonType: StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsExactMatch(string package, string packageId)
    {
        return package.Equals(value: packageId, comparisonType: StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsExactMatch(string packageId, PackageVersion packageVersion)
    {
        return IsExactMatch(package: packageVersion.PackageId, packageId: packageId);
    }
}