using System;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Extensions.Logging;
using NonBlocking;

namespace Credfeto.Package.Services;

public sealed class ProjectLoader : IProjectLoader
{
    private readonly ConcurrentDictionary<string, IProject> _loadedProjects;
    private readonly ILogger<ProjectLoader> _logger;

    public ProjectLoader(ILogger<ProjectLoader> logger)
    {
        this._logger = logger;
        this._loadedProjects = new(StringComparer.Ordinal);
    }

    public async Task<IProject?> LoadAsync(string path)
    {
        if (this._loadedProjects.TryGetValue(key: path, out IProject? project))
        {
            return project;
        }

        try
        {
            XmlDocument doc = new();

            // TODO: work out how to load the doc async from disk
            await Task.CompletedTask;

            doc.Load(path);

            project = new Project(fileName: path, doc: doc);

            return this._loadedProjects.GetOrAdd(key: path, value: project);
        }
        catch (Exception exception)
        {
            this._logger.LogError(new(exception.HResult), exception: exception, $"Failed to load {path}: {exception.Message}");

            return null;
        }
    }
}