using System;
using System.IO;
using System.Reflection;

namespace Feuerzeug;

public static class AppInfo
{
    const string NAME = nameof(Feuerzeug);
    const string SERVICE = "Service";

    public const string SERVICE_NAME = $"{NAME}{SERVICE}";
    public const string DISPLAY_NAME = $"{NAME} {SERVICE}";

    public static string GetBinPath()
    {
        var binName = $"{DISPLAY_NAME}.exe";

        var path = Path.GetFullPath(binName);
        if (File.Exists(path))
        {
            return path;
        }

        var assembly = Assembly.GetExecutingAssembly();
        if (assembly is not null)
        {
            path = Path.Combine(Path.GetDirectoryName(assembly.Location), binName);
            if (File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    public static bool IsInteractive => Environment.UserInteractive;
}
