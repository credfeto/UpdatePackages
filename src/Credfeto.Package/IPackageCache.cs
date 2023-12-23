using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Credfeto.Package;

public interface IPackageCache
{
    Task LoadAsync(string fileName, CancellationToken cancellationToken);

    Task SaveAsync(string fileName, CancellationToken cancellationToken);

    IReadOnlyList<PackageVersion> GetAll();

    IReadOnlyList<PackageVersion> GetVersions(IReadOnlyList<string> packageIds);

    void SetVersions(IReadOnlyList<PackageVersion> matching);
}