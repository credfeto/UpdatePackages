using System.Xml;

namespace Credfeto.Package.Update.Services;

public sealed class ProjectLoader : IProjectLoader
{
    public IProject? Load(string path)
    {
        XmlDocument? doc = ProjectHelpers.TryLoadDocument(path);

        if (doc == null)
        {
            return null;
        }

        return new Project(doc);
    }
}