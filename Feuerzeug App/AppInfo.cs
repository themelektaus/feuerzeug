using System;

using Process = System.Diagnostics.Process;

namespace Feuerzeug;

public static class AppInfo
{
    public const string SERVICE_NAME = "FeuerzeugService";

    public static readonly string name = AppDomain.CurrentDomain.FriendlyName;
    public static readonly string version
        = typeof(AppInfo).Assembly.GetName()?.Version?.ToString() ?? "0.0.0.0";

    static readonly Process process = Process.GetCurrentProcess();
    public static readonly string currentProcessName = process.ProcessName;
    public static readonly string currentProcessExeName = process.MainModule.ModuleName;

    public static readonly string hostPage = "wwwroot/index.html";
}
