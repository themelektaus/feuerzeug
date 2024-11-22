using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Feuerzeug;

using Web;

public class App : IDisposable
{
    public static App Instance { get; private set; }

    public MainForm mainForm;

    public Update Update { get; private set; }

    readonly KeyboardHook keyboardHook;

    Task updateCheckTask;
    int nextUpdateCheckCountdown;

    public CTS CTS { get; private set; }

    public App()
    {
        Instance = this;

        mainForm = new();
        mainForm.FormClosing += (sender, e) =>
        {
            Logger.Pending("Closing");
        };

        keyboardHook = new();
        keyboardHook.KeyPressed += async (sender, e) =>
        {
            if (USer_IsRunning())
            {
                await USer_Stop();
            }
            else
            {
                await USer_Start();
            }
        };
        keyboardHook.RegisterHotKey(KeyboardHook.ModifierKeys.None, Keys.Scroll);

        updateCheckTask = Task.Run(async () =>
        {
            while (Root.Instance is null)
            {
                await Task.Delay(10);
            }

        Loop:
            await Task.Delay(1000);
            await CheckForUpdates();

            nextUpdateCheckCountdown = 900;

            while (nextUpdateCheckCountdown > 0)
            {
                if (updateCheckTask is null)
                {
                    return;
                }

                nextUpdateCheckCountdown--;
                await Task.Delay(1000);
            }

            goto Loop;
        });
    }

    public void Dispose()
    {
        keyboardHook.Dispose();

        var task = updateCheckTask;

        updateCheckTask = null;

        PowerShellSessionManager.Dispose();

        Config.Instance.Save();

        task.Wait();
    }

    public enum BusinessScope { Local, Global }

    readonly Dictionary<BusinessScope, int> businessScopes = new() {
        { BusinessScope.Local, 0 },
        { BusinessScope.Global, 0 }
    };

    public bool IsBusyLocally() => businessScopes[BusinessScope.Local] > 0;
    public bool IsBusyGlobally() => businessScopes[BusinessScope.Global] > 0;

    public async Task Run(BusinessScope scope, Func<Task> task)
    {
        await IncreaseBusinessAsync(scope);
        await task();
        await DecreaseBusinessAsync(scope);
    }

    public async Task Run(BusinessScope scope, Func<CT, Task> task, Func<Task> cancelTask)
    {
        await IncreaseBusinessAsync(scope);
        using (CTS = new())
            await task(CTS.Token);
        if (CTS.IsCancellationRequested)
            if (cancelTask is not null)
                await cancelTask();
        CTS = null;
        await DecreaseBusinessAsync(scope);
    }

    public async Task Cancel()
    {
        if (CTS is not null)
        {
            if (!CTS.IsCancellationRequested)
                CTS.Cancel();
            await Utils.WaitForAsync(() => CTS is null);
        }
    }

    public async Task IncreaseBusinessAsync(BusinessScope scope)
    {
        businessScopes[scope]++;
        await (Root.Instance?.RenderLaterAsync() ?? Task.CompletedTask);
    }

    public async Task DecreaseBusinessAsync(BusinessScope scope)
    {
        businessScopes[scope]--;
        await (Root.Instance?.RenderLaterAsync() ?? Task.CompletedTask);
    }

    public async Task CheckForUpdates()
    {
        var updateAvailable = Update?.available ?? false;

        if (Root.Instance is null)
        {
            Update = null;
            return;
        }

        Update = await Update.Check();

        if (updateAvailable != Update.available)
        {
            await Root.Instance.RenderLaterAsync();
        }
    }

    public async Task PerformUpdate()
    {
        await IncreaseBusinessAsync(BusinessScope.Global);

        Logger.Pending($"Downloading v{Update.remoteVersion}");

        await Update.Prepare();

        await FeuerzeugService.StopAsync();

        Utils.StartAsAdmin("Update.bat", createNoWindow: true);

        Logger.Info($"The application is going to be closed now");

        await Task.Delay(3000);

        mainForm.Dispose();
    }

    public bool LogViewVisible { get; set; }

    public async Task ToggleLogViewAsync()
    {
        LogViewVisible = !LogViewVisible;
        await (Root.Instance?.RenderLaterAsync() ?? Task.CompletedTask);
    }

    public static async Task<string> ShowOpenFolderDialog(string folder)
    {
        using var dialog = Utils.CreateOpenFolderDialog(folder);
        return (await dialog.ShowDialogAsync()) == DialogResult.OK
            ? dialog.SelectedPath
            : null;
    }

    public static bool USer_IsRunning()
    {
        return Process.GetProcessesByName("USer").Length > 0;
    }

    public static async Task USer_Start()
    {
        await USer_Stop();

        Utils.Open(Path.Combine("Resources", "USer.exe"), createNoWindow: true);

        await Utils.WaitForAsync(() => USer_IsRunning(), TimeSpan.FromSeconds(10));

        if (Root.Instance?.Page == typeof(Web.Pages.Page_AdminHub))
        {
            Root.Instance.Refresh();
        }
    }

    public static async Task USer_Stop()
    {
        foreach (var process in Process.GetProcessesByName("USer"))
        {
            process.Kill();
            process.WaitForExit();
        }

        await Utils.WaitForAsync(() => !USer_IsRunning(), TimeSpan.FromSeconds(10));

        if (Root.Instance?.Page == typeof(Web.Pages.Page_AdminHub))
        {
            Root.Instance.Refresh();
        }
    }

}
