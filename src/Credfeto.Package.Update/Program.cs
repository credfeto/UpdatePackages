using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Package.Update.Helpers;
using Credfeto.Package.Update.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Credfeto.Package.Update;

internal static class Program
{
    private const int SUCCESS = 0;
    private const int ERROR = 1;

    private static async Task<int> Main(string[] args)
    {
        Console.WriteLine($"{typeof(Program).Namespace} {ExecutableVersionInformation.ProgramVersion()}");

        try
        {
            return await LookForUpdatesAsync(args);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"ERROR: {exception.Message}");

            return ERROR;
        }
    }

    private static async Task<int> LookForUpdatesAsync(string[] args)
    {
        IConfigurationRoot configuration = ConfigurationLoader.LoadConfiguration(args);

        string folder = configuration.GetValue<string>(key: @"Folder");

        if (string.IsNullOrEmpty(folder))
        {
            Console.WriteLine("ERROR: folder not specified");

            return ERROR;
        }

        string source = configuration.GetValue<string>(key: @"source");

        PackageUpdateConfiguration config = GetUpdateConfiguration(configuration);

        IServiceProvider services = Setup(false);

        IPackageUpdater packageUpdater = services.GetRequiredService<IPackageUpdater>();

        List<string> packageSources = new();

        if (!string.IsNullOrEmpty(source))
        {
            packageSources.Add(source);
        }

        IDiagnosticLogger logging = services.GetRequiredService<IDiagnosticLogger>();

        IReadOnlyList<PackageVersion> updatesMade =
            await packageUpdater.UpdateAsync(basePath: folder, configuration: config, packageSources: packageSources, cancellationToken: CancellationToken.None);
        Console.WriteLine();

        if (logging.IsErrored)
        {
            return ERROR;
        }

        if (updatesMade.Count != 0)
        {
            Console.WriteLine($"Total Updates: {updatesMade.Count}");

            OutputPackageUpdateTags(updatesMade);

            return SUCCESS;
        }

        Console.WriteLine(value: "No updates made.");

        return SUCCESS;
    }

    private static void OutputPackageUpdateTags(IReadOnlyList<PackageVersion> updated)
    {
        foreach (PackageVersion package in updated)
        {
            Console.WriteLine($"echo ::set-env name={package.PackageId}::{package.Version}");
        }
    }

    private static PackageUpdateConfiguration GetUpdateConfiguration(IConfigurationRoot configuration)
    {
        string packageId = configuration.GetValue<string>(key: @"packageid");

        IReadOnlyList<PackageMatch> excludedPackages = GetExcludedPackages(configuration);

        return new(ExtractSearchPackage(packageId), ExcludedPackages: excludedPackages);
    }

    private static IReadOnlyList<PackageMatch> GetExcludedPackages(IConfigurationRoot configuration)
    {
        string[]? excludes = configuration.GetValue<string[]>("excludes");

        if (excludes == null || excludes.Length == 0)
        {
            return Array.Empty<PackageMatch>();
        }

        List<PackageMatch> excludedPackages = new();

        foreach (string exclude in excludes)
        {
            PackageMatch packageMatch = ExtractSearchPackage(exclude);

            excludedPackages.Add(packageMatch);

            Console.WriteLine($"Excluding {packageMatch.PackageId} (Using Prefix match: {packageMatch.Prefix})");
        }

        return excludedPackages;
    }

    private static PackageMatch ExtractSearchPackage(string exclude)
    {
        string[] parts = exclude.Split(separator: ':');

        return parts.Length == 2
            ? new(parts[0], StringComparer.InvariantCultureIgnoreCase.Equals(parts[1], y: "prefix"))
            : new(parts[0], Prefix: false);
    }

    private static IServiceProvider Setup(bool warningsAsErrors)
    {
        DiagnosticLogger logger = new(warningsAsErrors);

        return new ServiceCollection().AddSingleton<ILogger>(logger)
                                      .AddSingleton<IDiagnosticLogger>(logger)
                                      .AddSingleton(typeof(ILogger<>), typeof(LoggerProxy<>))
                                      .AddPackageUpdater()
                                      .BuildServiceProvider();
    }
}