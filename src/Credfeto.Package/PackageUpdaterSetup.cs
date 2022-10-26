using System.Diagnostics.CodeAnalysis;
using Credfeto.Package.Update.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Credfeto.Package;

/// <summary>
///     Configures the ethereum abi services.
/// </summary>
[ExcludeFromCodeCoverage]
public static class PackageUpdaterSetup
{
    /// <summary>
    ///     Configures the ethereum client services.
    /// </summary>
    /// <param name="services">The services to add things to.</param>
    public static IServiceCollection AddAbiTypeFactory(this IServiceCollection services)
    {
        return services.AddSingleton<IProjectLoader, ProjectLoader>();
    }
}