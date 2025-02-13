﻿using System;

namespace Credfeto.Package.Update.Exceptions;

public sealed class PackageUpdateException : Exception
{
    public PackageUpdateException() { }

    public PackageUpdateException(string? message)
        : base(message) { }

    public PackageUpdateException(string? message, Exception? innerException)
        : base(message: message, innerException: innerException) { }
}
