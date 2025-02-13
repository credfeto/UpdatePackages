using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Credfeto.Package;

public interface IPackageCache
{
    ValueTask LoadAsync(string fileName, CancellationToken cancellationToken);

    ValueTask SaveAsync(string fileName, CancellationToken cancellationToken);

    IReadOnlyList<PackageVersion> GetAll();

    IReadOnlyList<PackageVersion> GetVersions(IReadOnlyList<string> packageIds);

    void SetVersions(IReadOnlyList<PackageVersion> matching);

    void Reset();
}
