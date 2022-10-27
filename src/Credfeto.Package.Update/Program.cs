using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Credfeto.Package.Update.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace Credfeto.Package.Update;

internal static class Program
{
    private const int SUCCESS = 0;
    private const int ERROR = 1;

    private static async Task<int> Main(string[] args)
    {
        Console.WriteLine($"{typeof(Program).Namespace} {ExecutableVersionInformation.ProgramVersion()}");
        Console.WriteLine();

        try
        {
            ParserResult<Options> parser = await Parser.Default.ParseArguments<Options>(args)
                                                       .WithNotParsed(NotParsed)
                                                       .WithParsedAsync(ParsedOkAsync);

            return parser.Tag == ParserResultType.Parsed
                ? SUCCESS
                : ERROR;
        }
        catch (PackageUpdateException exception)
        {
            Console.WriteLine(exception.Message);

            return ERROR;
        }
        catch (Exception exception)
        {
            Console.WriteLine($"ERROR: {exception.Message}");

            if (exception.StackTrace != null)
            {
                Console.WriteLine(exception.StackTrace);
            }

            return ERROR;
        }
    }

    private static void NotParsed(IEnumerable<Error> errors)
    {
        Console.WriteLine("Errors:");

        foreach (Error error in errors)
        {
            Console.WriteLine($" * {error.Tag}");
        }
    }

    private static async Task ParsedOkAsync(Options options)
    {
        if (!string.IsNullOrWhiteSpace(options.Folder) && !string.IsNullOrWhiteSpace(options.PackageId))
        {
            IServiceProvider services = ApplicationSetup.Setup(false);

            IDiagnosticLogger logging = services.GetRequiredService<IDiagnosticLogger>();
            IPackageUpdater packageUpdater = services.GetRequiredService<IPackageUpdater>();

            PackageUpdateConfiguration config = BuildConfiguration(packageId: options.PackageId, options.Exclude?.ToArray() ?? Array.Empty<string>());
            IReadOnlyList<PackageVersion> updatesMade = await packageUpdater.UpdateAsync(basePath: options.Folder,
                                                                                         configuration: config,
                                                                                         options.Source?.ToArray() ?? Array.Empty<string>(),
                                                                                         cancellationToken: CancellationToken.None);

            if (logging.IsErrored)
            {
                throw new PackageUpdateException();
            }

            Console.WriteLine($"Total updates: {updatesMade.Count}");

            if (updatesMade.Count != 0)
            {
                OutputPackageUpdateTags(updatesMade);

                return;
            }

            return;
        }

        throw new InvalidOptionsException();
    }

    private static PackageUpdateConfiguration BuildConfiguration(string packageId, IReadOnlyList<string> exclude)
    {
        PackageMatch packageMatch = ExtractSearchPackage(packageId);
        Console.WriteLine($"Including {packageMatch.PackageId} (Using Prefix match: {packageMatch.Prefix})");

        IReadOnlyList<PackageMatch> excludedPackages = GetExcludedPackages(exclude);

        return new(PackageMatch: packageMatch, ExcludedPackages: excludedPackages);
    }

    private static void OutputPackageUpdateTags(IReadOnlyList<PackageVersion> updated)
    {
        foreach (PackageVersion package in updated)
        {
            Console.WriteLine($"echo ::set-env name={package.PackageId}::{package.Version}");
        }
    }

    private static IReadOnlyList<PackageMatch> GetExcludedPackages(IReadOnlyList<string> excludes)
    {
        if (excludes.Count == 0)
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
}