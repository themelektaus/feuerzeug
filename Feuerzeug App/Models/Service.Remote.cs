using System.Management.Automation;

namespace Feuerzeug.Models;

public abstract partial class Service
{
    public class Remote : Service
    {
        public string StartType { get; private set; }
        public string Status { get; set; }

        public override string StartTypeString => StartType;
        public override string StatusString => Status;

        public override void SetObject(PSObject @object)
        {
            Object = @object;
            StartType = @object.Get<string>(nameof(StartType));
            Status = @object.Get<string>(nameof(Status));
        }

        public override object ToPowershellArgument() => Object;

        public static Remote From(PSObject x)
        {
            return new()
            {
                Object = x,
                Name = x.Get<string>(nameof(Name)),
                DisplayName = x.Get<string>(nameof(DisplayName)),
                StartType = x.Get<string>(nameof(StartType)),
                Status = x.Get<string>(nameof(Status))
            };
        }
    }
}
