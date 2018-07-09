using System;

namespace UpdatePackages
{
    public sealed class PackageVersion : IEquatable<PackageVersion>
    {
        public PackageVersion(string packageId, string version)
        {
            this.PackageId = packageId;
            this.Version = version;
        }

        public string PackageId { get; }

        public string Version { get; }

        public bool Equals(PackageVersion other)
        {
            return !ReferenceEquals(objA: null, objB: other) && (ReferenceEquals(this, other) || string.Equals(this.PackageId, other.PackageId, StringComparison.OrdinalIgnoreCase));
        }

        public override bool Equals(object obj)
        {
            return !ReferenceEquals(objA: null, objB: obj) && (ReferenceEquals(this, obj) || obj.GetType() == this.GetType() && this.Equals((PackageVersion) obj));
        }

        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(this.PackageId);
        }

        public static bool operator ==(PackageVersion left, PackageVersion right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(PackageVersion left, PackageVersion right)
        {
            return !Equals(left, right);
        }
    }
}