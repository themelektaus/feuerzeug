using System;
using System.Threading;

using File = System.IO.File;
using Task = System.Threading.Tasks.Task;
using TaskBoolean = System.Threading.Tasks.Task<bool>;

using PInvoke = Windows.Win32.PInvoke;
using HWND = Windows.Win32.Foundation.HWND;
using SHOW_WINDOW_CMD = Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD;

namespace Feuerzeug;

public static class SingleInstance
{
    const string GUID = "7b309fbb-d533-49d3-a30e-cc85a36118b8";
    const string MUTEX_NAME = $"Local\\{GUID}";

    static Mutex mutex;

    public static async TaskBoolean StartAsync()
    {
        mutex = new Mutex(true, MUTEX_NAME, out var createdNew);

        if (createdNew)
        {
            await DeleteShowLockFileAsync();
            return true;
        }

        return false;
    }

    public static async Task StopAsync()
    {
        await DeleteShowLockFileAsync();
        GC.KeepAlive(mutex);
    }

    public static async Task Show(nint handle)
    {
        await DeleteShowLockFileAsync();

        var hWnd = (HWND) handle;
        PInvoke.ShowWindow(hWnd, SHOW_WINDOW_CMD.SW_SHOWNORMAL);
        PInvoke.SetForegroundWindow(hWnd);
    }

    public static void CreateShowLockFile()
    {
        if (!File.Exists(nameof(Show)))
            File.Create(nameof(Show)).Dispose();
    }

    static async Task DeleteShowLockFileAsync()
    {
        var retriesLeft = 10;

    Retry:
        if (File.Exists(nameof(Show)))
        {
            try
            {
                File.Delete(nameof(Show));
            }
            catch
            {
                await Task.Delay(100);
                if (retriesLeft-- > 0)
                    goto Retry;
            }
        }
    }

    //public static void Show()
    //{
    //    PInvoke.PostMessage((HWND) 0xffff, Msg, 0, 0);
    //}

    //const string MESSAGE = $"{nameof(Feuerzeug)},{nameof(SingleInstance)},{nameof(Show)},{GUID}";

    //static uint? message;
    //static uint Msg => message ??= PInvoke.RegisterWindowMessage(MESSAGE);

    //public static void ProcessWindow(nint handle, int msg)
    //{
    //    if (Msg == msg)
    //    {
    //        var n = Environment.NewLine;
    //        Logger.Script(
    //            nameof(SingleInstance), ".", nameof(ProcessWindow), "(", n,
    //            "&nbsp;&nbsp;handle: ", handle.ToString(), ",", n,
    //            "&nbsp;&nbsp;msg: ", msg.ToString(), n,
    //            ")", n,
    //            "MESSAGE: ", MESSAGE
    //        );
    //        Show(handle);
    //    }
    //}
}
