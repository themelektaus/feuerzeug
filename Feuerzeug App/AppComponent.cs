#pragma warning disable IDE1006

using Microsoft.AspNetCore.Components;

using System.Threading.Tasks;

namespace Feuerzeug;

using Web;

public abstract class AppComponent : ComponentBase
{
    protected static App app => App.Instance;

    protected static Config_RemoteMachine remoteMachine
        => Config.Instance.remoteMachine;

    protected static async Task GotoAsync<T>() where T : Page
    {
        await Root.Instance.GotoAsync(typeof(T));
    }

    protected static void Refresh()
    {
        Root.Instance.Refresh();
    }

    protected async Task Run(
        App.BusinessScope scope,
        System.Func<Task> task
    )
    {
        await App.Instance.Run(scope, task);

        this.RenderLater();
    }

    protected async Task Run(
        App.BusinessScope scope,
        System.Func<CT, Task> task,
        System.Func<Task> cancelTask
    )
    {
        await App.Instance.Run(scope, task, cancelTask);

        this.RenderLater();
    }

}
