#pragma warning disable IDE1006

using System.Threading.Tasks;

namespace Feuerzeug;

public abstract class Page : AppComponent
{
    protected bool isInitialized { get; private set; }

    protected virtual Task OnInitAsync()
    {
        return Task.CompletedTask;
    }

    protected sealed override Task OnInitializedAsync() => Run(
        App.BusinessScope.Local,
        async () =>
        {
            await OnInitAsync();

            isInitialized = true;
        }
    );
}
