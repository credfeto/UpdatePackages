using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Credfeto.Package.Services;

public sealed class PackageRegistry : IPackageRegistry
{
    public Task<IReadOnlyList<PackageVersion>> FindPackagesAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}