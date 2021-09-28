using System;
using System.Diagnostics;
using NuGet.Versioning;

namespace Credfeto.Package.Update
{
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
            return !ReferenceEquals(objA: null, objB: other) && (ReferenceEquals(this, objB: other) ||
                                                                 string.Equals(a: this.PackageId, b: other.PackageId, comparisonType: StringComparison.OrdinalIgnoreCase));
        }

        public override bool Equals(object? obj)
        {
            return !ReferenceEquals(objA: null, objB: obj) && (ReferenceEquals(this, objB: obj) || obj.GetType() == this.GetType() && this.Equals((PackageVersion)obj));
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
}