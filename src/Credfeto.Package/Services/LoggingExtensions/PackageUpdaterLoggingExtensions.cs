using Microsoft.Extensions.Logging;

namespace Credfeto.Package.Services.LoggingExtensions;

internal static partial class PackageUpdaterLoggingExtensions
{
    [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "No matching packages installed")]
    public static partial void NoMatchingPackagesInstalled(this ILogger<PackageUpdater> logger);

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "No matching packages found in event source")]
    public static partial void NoMatchingPackagesInEventSource(this ILogger<PackageUpdater> logger);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "All installed packages are up to date")]
    public static partial void AllInstalledPackagesAreUpToDate(this ILogger<PackageUpdater> logger);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Saving {fileName}")]
    public static partial void SavingProject(this ILogger<PackageUpdater> logger, string fileName);
}