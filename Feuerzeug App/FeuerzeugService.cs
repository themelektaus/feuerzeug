using System;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace Feuerzeug;

public static class FeuerzeugService
{
    public static bool IsInstalled() => ServiceController.GetServices().Any(
        x => x.ServiceName == AppInfo.SERVICE_NAME
    );

    static ServiceController CreateController() => new(AppInfo.SERVICE_NAME);

    public static bool IsRunning()
    {
        using var controller = CreateController();
        return controller.Status == ServiceControllerStatus.Running;
    }

    public static async Task<bool> StartAsync()
    {
        try
        {
            using var controller = CreateController();
            await Task.Run(controller.Start);
            await Task.Run(() => controller.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30)));
        }
        catch (Exception ex)
        {
            Logger.Exception(ex);
            return false;
        }
        return true;
    }

    public static async Task<bool> StopAsync()
    {
        try
        {
            using var controller = CreateController();
            await Task.Run(controller.Stop);
            await Task.Run(() => controller.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30)));
        }
        catch (Exception ex)
        {
            Logger.Exception(ex);
            return false;
        }
        return true;
    }
}
