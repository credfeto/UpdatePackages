using System.Collections.Generic;
using System.Threading.Tasks;

namespace Credfeto.Package;

public interface IPackageUpdater
{
    Task<int> UpdateAsync(string basePath, PackageUpdateConfiguration configuration, IReadOnlyList<string> packageSources);
}