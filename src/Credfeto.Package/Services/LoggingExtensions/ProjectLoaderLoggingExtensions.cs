using System;
using Microsoft.Extensions.Logging;

namespace Credfeto.Package.Services.LoggingExtensions;

internal static partial class ProjectLoaderLoggingExtensions
{
    [LoggerMessage(EventId = 0, Level = LogLevel.Error, Message = "Failed to load {fileName}: {message}")]
    public static partial void FailedToLoad(this ILogger<ProjectLoader> logger, string fileName, string message, Exception exception);
}