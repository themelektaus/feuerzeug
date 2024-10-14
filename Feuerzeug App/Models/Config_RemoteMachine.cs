using System.Threading.Tasks;

namespace Feuerzeug.Models;

public class Config_RemoteMachine
{
    public string Hostname { get; set; } = "localhost";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    public async Task<PowerShellSession> GetSessionAsync()
    {
        return await PowerShellSessionManager.GetSessionAsync(this);
    }
}
