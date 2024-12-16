using System.Management.Automation;

namespace Feuerzeug.Models;

public class Process
{
    public PSObject Object { get; set; }
    public int Id { get; private set; }
    public string Name { get; private set; }

    public float Cpu { get; set; }

    public static Process From(PSObject x)
    {
        return new()
        {
            Object = x,
            Id = x.Get<int>(nameof(Id)),
            Name = x.Get<string>(nameof(Name))
        };
    }

    public class Usage
    {
        public PSObject Object { get; set; }
        public uint Id { get; private set; }
        public string Name { get; private set; }
        public ulong Value { get; private set; }

        public static Usage From(PSObject x)
        {
            return new()
            {
                Object = x,
                Id = x.Get<uint>("IDProcess"),
                Name = x.Get<string>(nameof(Name)),
                Value = x.Get<ulong>("PercentProcessorTime"),
            };
        }

    }
}
