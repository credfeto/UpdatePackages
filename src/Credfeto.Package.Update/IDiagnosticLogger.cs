using Microsoft.Extensions.Logging;

namespace Credfeto.Package.Update;

public interface IDiagnosticLogger : ILogger
{
    long Errors { get; }

    bool IsErrored { get; }
}