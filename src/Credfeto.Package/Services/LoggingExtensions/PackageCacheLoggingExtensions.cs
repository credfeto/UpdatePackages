using Microsoft.Extensions.Logging;

namespace Credfeto.Package.Services.LoggingExtensions;

internal static partial class PackageCacheLoggingExtensions
{
    [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Loading cache from {FileName}")]
    public static partial void LoadingCache(this ILogger<PackageCache> logger, string fileName);
}