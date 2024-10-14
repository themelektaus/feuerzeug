using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Feuerzeug;

public static class PowerShellSessionManager
{
    static readonly List<PowerShellSession> sessions = [];

    public static async Task<PowerShellSession> GetSessionAsync(
        Config_RemoteMachine remoteMachine
    )
    {
        var session = sessions.FirstOrDefault(x
            => x.Hostname == remoteMachine.Hostname
            && x.Username == remoteMachine.Username
        );

        if (session is null)
        {
            session = new();

            bool sessionStarted;

            if (
                remoteMachine.Hostname == string.Empty ||
                remoteMachine.Hostname == "localhost"
            )
            {
                sessionStarted = true;
            }
            else
            {
                sessionStarted = await Task.Run(
                    () => session.BeginSession(
                        remoteMachine.Hostname,
                        remoteMachine.Username,
                        remoteMachine.Password
                    )
                );
            }

            if (sessionStarted)
            {
                sessions.Add(session);
            }
        }

        return session;

    }

    public static void Dispose()
    {
        foreach (var session in sessions)
        {
            session.EndSession();
        }

        sessions.Clear();
    }
}
