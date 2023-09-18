namespace Credfeto.Package.Update;

internal static class ExecutableVersionInformation
{
    public static string ProgramVersion()
    {
        return ThisAssembly.Info.FileVersion;
    }
}