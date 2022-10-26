namespace Credfeto.Package;

public interface IProjectLoader
{
    IProject? Load(string path);
}