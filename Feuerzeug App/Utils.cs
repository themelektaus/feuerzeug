using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Feuerzeug;

public static class Utils
{
    public static bool IsAdmin()
    {
        var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public static void StartAsAdmin(string fileName, bool createNoWindow)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = true,
            Verb = "runas",
            CreateNoWindow = createNoWindow,
            WindowStyle = createNoWindow
                ? ProcessWindowStyle.Hidden
                : ProcessWindowStyle.Normal
        });
    }

    public static void Open(string fileName, string arguments = null, bool createNoWindow = false)
    {
        var process = new Process()
        {
            StartInfo = new(fileName, arguments)
            {
                UseShellExecute = true,
                CreateNoWindow = createNoWindow,
                WindowStyle = createNoWindow
                    ? ProcessWindowStyle.Hidden
                    : ProcessWindowStyle.Normal
            }
        };
        process.Start();
    }

    public static FolderBrowserDialog CreateOpenFolderDialog(string folder)
    {
        var dialog = new FolderBrowserDialog();

        if (!string.IsNullOrEmpty(folder))
        {
            var info = new DirectoryInfo(folder);

            if (info.Exists)
            {
                dialog.InitialDirectory = info.FullName;
            }
        }

        return dialog;
    }

    public static async Task WaitForAsync(TimeSpan timeout, CT ct)
    {
        await WaitForAsync(() => false, timeout, ct);
    }

    public static async Task WaitForAsync(Func<bool> condition)
    {
        await WaitForAsync(condition, TimeSpan.MaxValue, CT.None);
    }

    public static async Task WaitForAsync(Func<bool> condition, TimeSpan timeout)
    {
        await WaitForAsync(condition, timeout, CT.None);
    }

    public static async Task WaitForAsync(Func<bool> condition, TimeSpan timeout, CT ct)
    {
        await Task.Delay(100);

        var start = DateTime.Now;

        while (DateTime.Now - start < timeout && !condition() && !ct.IsCancellationRequested)
        {
            await Task.Delay(100);
        }
    }

    public static Task Promise(Action<Action> callback)
    {
        var taskSource = new TaskCompletionSource();
        callback(taskSource.SetResult);
        return taskSource.Task;
    }
}
