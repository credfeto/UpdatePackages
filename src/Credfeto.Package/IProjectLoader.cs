using System.Threading;
using System.Threading.Tasks;

namespace Credfeto.Package;

public interface IProjectLoader
{
    Task<IProject?> LoadAsync(string path, CancellationToken cancellationToken);
}