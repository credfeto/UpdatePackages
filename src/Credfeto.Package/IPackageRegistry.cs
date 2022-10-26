using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Credfeto.Package;

public interface IPackageRegistry
{
    Task<IReadOnlyList<PackageVersion>> FindPackagesAsync(IReadOnlyList<string> packageIds, IReadOnlyList<string> packageSources, CancellationToken cancellationToken);
}