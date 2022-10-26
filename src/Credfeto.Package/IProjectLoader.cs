namespace Credfeto.Package.Update.Services;

public interface IProjectLoader
{
    IProject? Load(string path);
}