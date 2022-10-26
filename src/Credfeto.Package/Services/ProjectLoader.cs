using System;
using System.Xml;
using Microsoft.Extensions.Logging;

namespace Credfeto.Package.Services;

public sealed class ProjectLoader : IProjectLoader
{
    private readonly ILogger<ProjectLoader> _logger;

    public ProjectLoader(ILogger<ProjectLoader> logger)
    {
        this._logger = logger;
    }

    public IProject? Load(string path)
    {
        try
        {
            XmlDocument doc = new();

            doc.Load(path);

            return new Project(doc);
        }
        catch (Exception exception)
        {
            this._logger.LogError(new(exception.HResult), exception: exception, $"Failed to load {path}: {exception.Message}");

            return null;
        }
    }
}