using Microsoft.Extensions.Logging;
using NuGet.Versioning;

namespace Credfeto.Package.Services.LoggingExtensions;

internal static partial class PackageCacheLoggingExtensions
{
    [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Loading cache from {fileName}")]
    public static partial void LoadingCache(this ILogger<PackageCache> logger, string fileName);

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Saving cache to {fileName}")]
    public static partial void SavingCache(this ILogger<PackageCache> logger, string fileName);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Loaded {packageId} {version} from cache")]
    public static partial void LoadedPackageVersionFromCache(this ILogger<PackageCache> logger, string packageId, string version);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Updated cache of {packageId} from {existing} to {version}")]
    public static partial void UpdatingPackageVersion(this ILogger<PackageCache> logger, string packageId, NuGetVersion existing, NuGetVersion version);

    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Adding cache of {packageId} at {version}")]
    public static partial void AddingPackageToCache(this ILogger<PackageCache> logger, string packageId, NuGetVersion version);
}