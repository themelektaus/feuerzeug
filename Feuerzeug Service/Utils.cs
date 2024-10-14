using System;
using System.IO;

namespace Feuerzeug;

public static class Utils
{
    public static string GetProcessDirectory()
    {
        return Path.GetDirectoryName(Environment.ProcessPath);
    }

    public static string GetWorkingDirectory()
    {
        var path = GetProcessDirectory();
#if DEBUG
        var current = new DirectoryInfo(path);
        do
        {
            if (File.Exists(Path.Combine(current.FullName, $"{AppInfo.DISPLAY_NAME}.csproj")))
            {
                path = current.FullName;
                break;
            }
            current = current.Parent;
        }
        while (current is not null);
#endif
        return path;
    }

    public static string GetWindowsFile(string fileName)
    {
        var path = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        return Path.Combine(path, fileName);
    }

    public static string GetSystemFile(string fileName)
    {
        var path = Environment.GetFolderPath(Environment.SpecialFolder.System);
        return Path.Combine(path, fileName);
    }
}
