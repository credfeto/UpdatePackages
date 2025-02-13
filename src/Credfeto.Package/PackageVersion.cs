using System;
using System.Diagnostics;
using NuGet.Versioning;

namespace Credfeto.Package;

[DebuggerDisplay(value: "{PackageId} {Version}")]
public sealed class PackageVersion : IEquatable<PackageVersion>
{
    public PackageVersion(string packageId, NuGetVersion version)
    {
        this.PackageId = packageId;
        this.Version = version;
    }

    public string PackageId { get; }

    public NuGetVersion Version { get; }

    public bool Equals(PackageVersion? other)
    {
        return other is not null && AreEqual(this, right: other);
    }

    private static bool AreEqual(PackageVersion left, PackageVersion right)
    {
        return ReferenceEquals(objA: left, objB: right)
            || StringComparer.InvariantCultureIgnoreCase.Equals(
                x: left.PackageId,
                y: right.PackageId
            );
    }

    public override bool Equals(object? obj)
    {
        return obj is not null
            && (
                ReferenceEquals(this, objB: obj)
                || obj is PackageVersion other && AreEqual(this, right: other)
            );
    }

    public override int GetHashCode()
    {
        return StringComparer.OrdinalIgnoreCase.GetHashCode(this.PackageId);
    }

    public static bool operator ==(PackageVersion? left, PackageVersion? right)
    {
        return Equals(objA: left, objB: right);
    }

    public static bool operator !=(PackageVersion? left, PackageVersion? right)
    {
        return !Equals(objA: left, objB: right);
    }
}
