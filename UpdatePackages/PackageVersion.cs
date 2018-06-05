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
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return string.Equals(this.PackageId, other.PackageId, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;

            return this.Equals((PackageVersion) obj);
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