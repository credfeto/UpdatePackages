using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Credfeto.Package;

public interface IPackageRegistry
{
    ValueTask<IReadOnlyList<PackageVersion>> FindPackagesAsync(IReadOnlyList<string> packageIds, IReadOnlyList<string> packageSources, CancellationToken cancellationToken);
}