using FunFair.Test.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Credfeto.Package.Test;

public sealed class DependencyInjectionTests : DependencyInjectionTestsBase
{
    public DependencyInjectionTests(ITestOutputHelper output)
        : base(output: output, dependencyInjectionRegistration: Configure) { }

    [Fact]
    public void ProjectLoaderMustBeRegistered()
    {
        this.RequireService<IProjectLoader>();
    }

    [Fact]
    public void PackageUpdaterMustBeRegistered()
    {
        this.RequireService<IPackageUpdater>();
    }

    [Fact]
    public void PackageRegistryMustBeRegistered()
    {
        this.RequireService<IPackageRegistry>();
    }

    private static IServiceCollection Configure(IServiceCollection services)
    {
        return services.AddPackageUpdater();
    }
}
