using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

using System.IO;
using System.Linq;
using System.Diagnostics;

using Console = System.Console;
using Env = System.Environment;

using Feuerzeug;
using Microsoft.Extensions.FileProviders;

var processDirectory = Utils.GetProcessDirectory();
var originDirectory = Env.CurrentDirectory;
Env.CurrentDirectory = Utils.GetWorkingDirectory();

var workingDirectoryInfo
    = $"Process Directory: {processDirectory}{Env.NewLine}"
    + $"Origin Directory: {originDirectory}{Env.NewLine}"
    + $"Working Directory: {Env.CurrentDirectory}";

if (AppInfo.IsInteractive)
{
    Console.WriteLine(workingDirectoryInfo);

    switch (args.FirstOrDefault())
    {
        case "install":
            ProcessHandler.Cmd("sc", "create", AppInfo.SERVICE_NAME,
                "DisplayName=", $"\"{AppInfo.DISPLAY_NAME}\"",
                "binPath=", $"\"{AppInfo.GetBinPath()}\"",
                "start=", "auto");
            break;

        case "uninstall":
            ProcessHandler.Cmd("sc", "delete", AppInfo.SERVICE_NAME);
            break;

        case "start":
            ProcessHandler.Cmd("net", "start", AppInfo.SERVICE_NAME);
            break;

        case "stop":
            ProcessHandler.Cmd("net", "stop", AppInfo.SERVICE_NAME);
            break;
    }

    return;
}

EventLog.WriteEntry(AppInfo.SERVICE_NAME, workingDirectoryInfo);

var builder = WebApplication.CreateBuilder();

builder.Host.UseWindowsService();

var app = builder.Build();

app.MapGet(
    "notepad",
    () => ProcessHandler.Start(Utils.GetWindowsFile("notepad.exe")).Id
);

app.MapGet(
    "ping",
    () => ProcessHandler.Cmd("ping", "1.1.1.1")
);

app.UseDefaultFiles();
app.UseDefaultFiles("/webgl");

Directory.CreateDirectory(
    Path.Combine(processDirectory, "webgl")
);

app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    RequestPath = "/webgl",
    FileProvider = new PhysicalFileProvider(
        Path.Combine(processDirectory, "webgl")
    )
});

app.UseMiddleware<NoCacheMiddleware>();

app.Run("http://0.0.0.0:5180");
