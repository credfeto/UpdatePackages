using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Credfeto.Package;

public interface IPackageUpdater
{
    Task<IReadOnlyList<PackageVersion>> UpdateAsync(string basePath,
                                                    PackageUpdateConfiguration configuration,
                                                    IReadOnlyList<string> packageSources,
                                                    CancellationToken cancellationToken);
}