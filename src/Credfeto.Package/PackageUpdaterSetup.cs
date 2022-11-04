using System.Diagnostics.CodeAnalysis;
using Credfeto.Package.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Credfeto.Package;

[ExcludeFromCodeCoverage]
public static class PackageUpdaterSetup
{
    public static IServiceCollection AddPackageUpdater(this IServiceCollection services)
    {
        return services.AddSingleton<IPackageCache, PackageCache>()
                       .AddSingleton<IProjectLoader, ProjectLoader>()
                       .AddSingleton<IPackageUpdater, PackageUpdater>()
                       .AddSingleton<IPackageRegistry, PackageRegistry>();
    }
}