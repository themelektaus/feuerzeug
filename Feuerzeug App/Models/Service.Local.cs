using System.Management.Automation;
using System.ServiceProcess;

namespace Feuerzeug.Models;

public abstract partial class Service
{
    public class Local : Service
    {
        public ServiceStartMode StartType { get; private set; }
        public ServiceControllerStatus Status { get; set; }

        public override string StartTypeString => StartType.ToString();
        public override string StatusString => Status.ToString();

        public override void SetObject(PSObject @object)
        {
            Object = @object;
            StartType = @object.Get<ServiceStartMode>(nameof(StartType));
            Status = @object.Get<ServiceControllerStatus>(nameof(Status));
        }

        public override object ToPowershellArgument() => Name;

        public static Local From(PSObject x)
        {
            return new()
            {
                Object = x,
                Name = x.Get<string>(nameof(Name)),
                DisplayName = x.Get<string>(nameof(DisplayName)),
                StartType = x.Get<ServiceStartMode>(nameof(StartType)),
                Status = x.Get<ServiceControllerStatus>(nameof(Status))
            };
        }
    }
}
