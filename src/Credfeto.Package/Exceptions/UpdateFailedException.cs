using System;

namespace Credfeto.Package.Exceptions;

public sealed class UpdateFailedException : Exception
{
    public UpdateFailedException()
        : this("Update failed")
    {
    }

    public UpdateFailedException(string message)
        : base(message)
    {
    }

    public UpdateFailedException(string message, Exception innerException)
        : base(message: message, innerException: innerException)
    {
    }
}