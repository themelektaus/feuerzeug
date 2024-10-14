// Source: https://stackoverflow.com/questions/19776716/c-sharp-windows-service-creates-process-but-doesnt-executes-it

using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;

using Console = System.Console;
using Env = System.Environment;

namespace Feuerzeug;

public static class ProcessHandler
{
    const int GENERIC_ALL_ACCESS = 0x10000000;
    const int STARTF_USESHOWWINDOW = 0x00000001;

    enum CreateProcessFlags
    {
        CREATE_BREAKAWAY_FROM_JOB = 0x01000000,
        CREATE_DEFAULT_ERROR_MODE = 0x04000000,
        CREATE_NEW_CONSOLE = 0x00000010,
        CREATE_NEW_PROCESS_GROUP = 0x00000200,
        CREATE_NO_WINDOW = 0x08000000,
        CREATE_PROTECTED_PROCESS = 0x00040000,
        CREATE_PRESERVE_CODE_AUTHZ_LEVEL = 0x02000000,
        CREATE_SEPARATE_WOW_VDM = 0x00000800,
        CREATE_SHARED_WOW_VDM = 0x00001000,
        CREATE_SUSPENDED = 0x00000004,
        CREATE_UNICODE_ENVIRONMENT = 0x00000400,
        DEBUG_ONLY_THIS_PROCESS = 0x00000002,
        DEBUG_PROCESS = 0x00000001,
        DETACHED_PROCESS = 0x00000008,
        EXTENDED_STARTUPINFO_PRESENT = 0x00080000,
        INHERIT_PARENT_AFFINITY = 0x00010000
    }

    enum SECURITY_IMPERSONATION_LEVEL
    {
        SecurityAnonymous,
        SecurityIdentification,
        SecurityImpersonation,
        SecurityDelegation
    }

    enum TOKEN_TYPE
    {
        TokenPrimary = 1,
        TokenImpersonation
    }

    [StructLayout(LayoutKind.Sequential)]
    struct STARTUPINFO
    {
        public int cb;
        public string lpReserved;
        public string lpDesktop;
        public string lpTitle;
        public int dwX;
        public int dwY;
        public int dwXSize;
        public int dwXCountChars;
        public int dwYCountChars;
        public int dwFillAttribute;
        public int dwFlags;
        public short wShowWindow;
        public short cbReserved2;
        public nint lpReserved2;
        public nint hStdInput;
        public nint hStdOutput;
        public nint hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct PROCESS_INFORMATION
    {
        public nint hProcess;
        public nint hThread;
        public int dwProcessID;
        public int dwThreadID;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct SECURITY_ATTRIBUTES
    {
        public int Length;
        public nint lpSecurityDescriptor;
        public bool bInheritHandle;
    }

    [DllImport(
        dllName: "kernel32.dll",
        EntryPoint = "CloseHandle",
        SetLastError = true,
        CharSet = CharSet.Auto,
        CallingConvention = CallingConvention.StdCall
    )]
    static extern bool CloseHandle(nint handle);

    [DllImport(
        dllName: "advapi32.dll",
        EntryPoint = "CreateProcessAsUser",
        SetLastError = true,
        CharSet = CharSet.Ansi,
        CallingConvention = CallingConvention.StdCall
    )]
    static extern bool CreateProcessAsUser(
        nint hToken,
        string lpApplicationName,
        string lpCommandLine,
        ref SECURITY_ATTRIBUTES lpProcessAttributes,
        ref SECURITY_ATTRIBUTES lpThreadAttributes,
        bool bInheritHandle,
        int dwCreationFlags,
        nint lpEnvrionment,
        string lpCurrentDirectory,
        ref STARTUPINFO lpStartupInfo,
        ref PROCESS_INFORMATION lpProcessInformation
    );

    [DllImport(
        dllName: "advapi32.dll",
        EntryPoint = "DuplicateTokenEx"
    )]
    static extern bool DuplicateTokenEx(
        nint hExistingToken,
        int dwDesiredAccess,
        ref SECURITY_ATTRIBUTES lpThreadAttributes,
        int ImpersonationLevel,
        int dwTokenType,
        ref nint phNewToken
    );

    [DllImport(
        dllName: "Kernel32.dll",
        SetLastError = true
    )]
    static extern nint WTSGetActiveConsoleSessionId();

    [DllImport(
        dllName: "wtsapi32.dll",
        SetLastError = true
    )]
    static extern bool WTSQueryUserToken(uint sessionId, out nint Token);

    static int GetCurrentUserSessionID()
    {
        var sessionId = WTSGetActiveConsoleSessionId();
        var processes = Process.GetProcessesByName("winlogon");
        var process = processes.FirstOrDefault(x => x.SessionId == sessionId);
        return process is null ? -1 : process.SessionId;
    }

    public static Process Start(string filePath, params string[] arguments)
    {
        var args = string.Join(' ', arguments);

        if (Env.UserInteractive)
        {
            Console.WriteLine(filePath + (args.Length == 0 ? "" : $" {args}"));

            return Process.Start(new ProcessStartInfo
            {
                FileName = filePath,
                Arguments = args
            });
        }
        
        var dupedToken = nint.Zero;

        var processInformation = new PROCESS_INFORMATION();

        var securityAttributes = new SECURITY_ATTRIBUTES();
        securityAttributes.Length = Marshal.SizeOf(securityAttributes);

        try
        {
            var startupInfo = new STARTUPINFO();
            startupInfo.cb = Marshal.SizeOf(startupInfo);
            startupInfo.lpDesktop = "";
            startupInfo.dwFlags = STARTF_USESHOWWINDOW;

            var fileDirectory = Path.GetDirectoryName(filePath);
            var fileName = Path.GetFileName(filePath);

            if (!DuplicateTokenEx(
                WindowsIdentity.GetCurrent().Token,
                GENERIC_ALL_ACCESS,
                ref securityAttributes,
                (int) SECURITY_IMPERSONATION_LEVEL.SecurityIdentification,
                (int) TOKEN_TYPE.TokenPrimary,
                ref dupedToken
            ))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            if (!WTSQueryUserToken((uint) GetCurrentUserSessionID(), out dupedToken))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            WindowsIdentity.RunImpersonated(
                WindowsIdentity.GetCurrent().AccessToken,
                () =>
                {
                    var commandLine = string.Format(
                        "\"{0}\" {1}",
                        fileName.Replace("\"", "\"\""),
                        args
                    );

                    if (!CreateProcessAsUser(
                        dupedToken,
                        filePath,
                        commandLine,
                        ref securityAttributes, // process attributes
                        ref securityAttributes, // thread attributes
                        false, // do not inherit handles
                        (int) CreateProcessFlags.CREATE_NEW_CONSOLE, // flags
                        nint.Zero, // environment block
                        fileDirectory,
                        ref startupInfo,
                        ref processInformation
                    ))
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }
                }
            );

            return Process.GetProcessById(processInformation.dwProcessID);
        }
        finally
        {
            if (processInformation.hProcess != nint.Zero)
            {
                CloseHandle(processInformation.hProcess);
            }

            if (processInformation.hThread != nint.Zero)
            {
                CloseHandle(processInformation.hThread);
            }

            if (dupedToken != nint.Zero)
            {
                CloseHandle(dupedToken);
            }
        }
    }

    public static void Cmd(params string[] command)
    {
        var process = Start(
            Utils.GetSystemFile("cmd.exe"),
            @$"/c {string.Join(' ', command)}"
        );
        process.WaitForExit();
    }
}
