using System;
using System.Windows.Forms;

namespace Feuerzeug;

public static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
#if RELEASE
        if (args.Length >= 4 && args[0] == "publish")
        {
            Update.Publish(
                version: args[1],
                scpTarget: args[2],
                scpPort: args[3]
            );
            return;
        }
#endif

        if (!SingleInstance.StartAsync().Result)
        {
            SingleInstance.CreateShowLockFile();
            return;
        }

#if RELEASE
        if (!Utils.IsAdmin())
        {
            Utils.StartAsAdmin(
                AppInfo.currentProcessExeName,
                createNoWindow: false
            );
            return;
        }
#endif

        ApplicationConfiguration.Initialize();

        using (var app = new App())
        {
#if RELEASE
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Logger.Error(e.ExceptionObject.ToString());
                Environment.Exit(0);
            };
#endif

            Application.Run(app.mainForm);
        }

        SingleInstance.StopAsync().Wait();
    }
}
