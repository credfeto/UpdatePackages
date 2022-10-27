using System;

namespace Credfeto.Package.Update.Exceptions;

public sealed class NoPackagesUpdatedException : Exception
{
    public NoPackagesUpdatedException()
    {
    }

    public NoPackagesUpdatedException(string? message)
        : base(message)
    {
    }

    public NoPackagesUpdatedException(string? message, Exception? innerException)
        : base(message: message, innerException: innerException)
    {
    }
}