#pragma warning disable IDE1006

using System.Management.Automation;

namespace Feuerzeug.Models;

public abstract partial class Service
{
    public PSObject Object { get; set; }
    public string Name { get; private set; }
    public string DisplayName { get; private set; }

    public abstract string StartTypeString { get; }
    public abstract string StatusString { get; }

    public bool isDisabled => StartTypeString == "Disabled";
    public bool isRunning => StatusString == "Running";
    public bool isStopped => StatusString == "Stopped";

    public bool isStartable => !isDisabled && isStopped;

    public abstract void SetObject(PSObject @object);
    public abstract object ToPowershellArgument();
}
