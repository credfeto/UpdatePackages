using Microsoft.Extensions.Logging;
using NuGet.Versioning;

namespace Credfeto.Package.Services.LoggingExtensions;

internal static partial class PackageRegistryLoggingExtensions
{
    [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Found package {packageId} version {version} in {source}")]
    public static partial void FoundPackageInSource(this ILogger<PackageRegistry> logger, string packageId, NuGetVersion version, string source);

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Enumerating matching package versions for {packageId}...")]
    public static partial void EnumeratingPackageVersions(this ILogger<PackageRegistry> logger, string packageId);
}