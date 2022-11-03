using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Credfeto.Package;

public interface IPackageCache
{
    Task LoadAsync(string fileName, CancellationToken none);

    Task SaveAsync(string fileName, CancellationToken none);

    IReadOnlyList<PackageVersion> GetVersions(IReadOnlyList<string> packageIds);

    void SetVersions(IReadOnlyList<PackageVersion> matching);
}