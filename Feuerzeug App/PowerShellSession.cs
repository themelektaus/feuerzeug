using Microsoft.AspNetCore.Components;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace Feuerzeug;

public class PowerShellSession : IDisposable
{
    static readonly SemaphoreSlim @lock = new(1, 1);

    const string WSMAN_PATH = @"WSMan:\localhost\Client\TrustedHosts";

    public struct Result
    {
        public List<PSObject> returnValue;
        public List<ErrorRecord> errors;
        public Exception exception;

        public bool HasErrors => errors.Count > 0 || exception is not null;

        public static Result Invoke(PowerShell powershell)
            => Invoke(powershell, CT.None);

        public static Result Invoke(PowerShell powershell, CT ct)
        {
            Result result = default;

            try
            {
                var handle = powershell.BeginInvoke();
                WaitHandle.WaitAny([handle.AsyncWaitHandle, ct.WaitHandle ]);
                if (ct.IsCancellationRequested)
                    powershell.Stop();
                ct.ThrowIfCancellationRequested();
                result.returnValue = [.. powershell.EndInvoke(handle) ?? []];
                result.errors = [.. powershell.Streams.Error];
            }
            catch (Exception ex)
            {
                result.exception = ex;
            }

            result.LogResult();
            result.LogErrors();
            result.LogException();

            return result;
        }

        public static async Task<Result> InvokeAsync(Pipeline pipeline, Action<Data> onData)
            => await InvokeAsync(pipeline, onData, CT.None);

        public static async Task<Result> InvokeAsync(Pipeline pipeline, Action<Data> onData, CT ct)
        {
            Result result = default;

            pipeline.Output.DataReady += (_, _) =>
            {
                var @object = pipeline.Output.NonBlockingRead(1).FirstOrDefault();
                if (@object is not null)
                {
                    onData.Invoke(new(DataType.Output, @object.ToString()));
                }
            };

            pipeline.Error.DataReady += (_, _) =>
            {
                var @object = pipeline.Error.NonBlockingRead(1).FirstOrDefault();
                if (@object is not null)
                {
                    onData.Invoke(new(DataType.Error, @object.ToString()));
                }
            };

            void OnStateChanged(Action resolve, PipelineStateInfo info)
            {
                if (info.State == PipelineState.Completed || info.State == PipelineState.Failed)
                {
                    result.exception = info.Reason;
                    resolve();
                }
            }

            var task = Utils.Promise(resolve =>
            {
                pipeline.StateChanged += (_, e) => OnStateChanged(resolve, e.PipelineStateInfo);
                pipeline.InvokeAsync();
            });

            await Task.WhenAny(task, Utils.WaitForAsync(() => ct.IsCancellationRequested));

            if (ct.IsCancellationRequested)
                await Task.Run(pipeline.Stop);

            result.LogException();

            return result;
        }

        void LogResult()
        {
            var stringTypeName = "System.String";

            foreach (var value in returnValue)
            {
                if (value is not null && value.TypeNames.Contains(stringTypeName))
                {
                    Logger.Result(value.ToString());
                }
            }
        }

        void LogErrors()
        {
            foreach (var error in errors)
            {
                if (error.ErrorDetails?.Message is not null)
                {
                    Logger.Error(error.ErrorDetails.Message);
                }
                else if (error.Exception is not null)
                {
                    Logger.Exception(error.Exception);
                }
            }
        }

        void LogException()
        {
            if (exception is not null)
            {
                Logger.Exception(exception);
            }
        }
    }

    readonly Runspace runspace;

    public string Hostname { get; private set; }
    public string Username { get; private set; }

    object session;

    public bool HasSession => session is not null;

    public enum DataType { Output, Error }
    public record Data(DataType Type, string Text);

    public event Action<Data> OnData;

    public PowerShellSession()
    {
        runspace = RunspaceFactory.CreateRunspace();
        runspace.Open();
    }

    public void Dispose()
    {
        if (HasSession)
        {
            EndSession();
        }
        runspace.Close();
    }

    public MarkupString ToMarkupString()
    {
        if (session is null)
        {
            return new(Hostname);
        }

        return new($"<span style=\"opacity: .75\">{Username ?? "guest"}@</span>{Hostname}");
    }

    public bool BeginSession(string hostname, string username, string password)
    {
        Logger.Pending("Starting Session");

        if (HasSession)
        {
            Logger.Warning("There is already an active session");
            return false;
        }

        Hostname = hostname;
        Username = username;

        AddTrustedHost();

        try
        {
            Command command;
            CommandParameterCollection parameters;

            command = new("New-PSSessionOption");
            parameters = command.Parameters;
            parameters.Add("OperationTimeout", 0);
            parameters.Add("IdleTimeout", 1200000);

            var sessionOption = RunCommandInternal(command).returnValue.Single().BaseObject;

            command = new("New-PSSession");
            parameters = command.Parameters;
            parameters.Add("ComputerName", Hostname);
            if (username != string.Empty)
            {
                var securePassword = new SecureString();
                foreach (var character in password.ToCharArray())
                    securePassword.AppendChar(character);
                securePassword.MakeReadOnly();

                parameters.Add("Credential", new PSCredential(username, securePassword));
            }
            parameters.Add("SessionOption", sessionOption);

            session = RunCommandInternal(command).returnValue.Single().BaseObject;
        }
        catch (Exception ex)
        {
            Logger.Exception(ex);
            return false;
        }

        if (session is null)
        {
            return false;
        }

        Logger.Success("Session started");

        return true;
    }

    public void EndSession()
    {
        if (!HasSession)
        {
            return;
        }

        RemoveTrustedHost();

        var sessionCommand = new Command("Remove-PSSession");
        sessionCommand.Parameters.Add("Session", session);

        RunCommandInternal(sessionCommand);

        Hostname = null;
        Username = null;

        session = null;

        Logger.Info("Session closed");
    }

    bool AddTrustedHost()
    {
        var trustedHosts = GetTrustedHosts();

        if (trustedHosts.Contains(Hostname))
        {
            return true;
        }

        if (trustedHosts.FirstOrDefault() == "*")
        {
            return true;
        }

        var value = trustedHosts.Count == 0
            ? Hostname
            : string.Join(',', trustedHosts);

        var command = new Command("Set-Item");
        command.Parameters.Add("Path", WSMAN_PATH);
        command.Parameters.Add("Value", value);
        command.Parameters.Add("Force", true);

        var result = RunCommandInternal(command);
        if (result.HasErrors)
        {
            return false;
        }

        trustedHosts.Add(Hostname);
        return true;
    }

    bool RemoveTrustedHost()
    {
        var trustedHosts = GetTrustedHosts();

        if (!trustedHosts.Contains(Hostname))
        {
            return false;
        }

        var newValue = trustedHosts.Count == 0
            ? string.Empty
            : string.Join(',', trustedHosts);

        var command = new Command("Set-Item");
        command.Parameters.Add("Path", WSMAN_PATH);
        command.Parameters.Add("Value", newValue);
        command.Parameters.Add("Force", true);

        var result = RunCommandInternal(command);
        if (result.HasErrors)
        {
            return false;
        }

        trustedHosts.Remove(Hostname);
        return true;
    }

    List<string> GetTrustedHosts()
    {
        var command = new Command("Get-Item");
        command.Parameters.Add("Path", WSMAN_PATH);
        return RunCommandInternal(command)
            .returnValue
            .FirstOrDefault()?
            .Properties
            .Single(x => x.Name == "Value")?
            .Value
            .ToString()?
            .Split(',')
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList() ?? [];
    }

    public struct Script
    {
        public static implicit operator Script(string @this)
        {
            return new() { text = @this };
        }

        public string text;

        public Options options;

        public struct Options
        {
            public object[] sensitiveArgs;
            public object[] argumentList;
            public enum OutputMode { Default, Json }
            public OutputMode outputMode;
        }
    }

    public Task<Result> RunScriptAsync(
        string scriptText,
        Script.Options scriptOptions
    )
    {
        return RunScriptAsync(
            scriptText,
            scriptOptions,
            CT.None
        );
    }

    public Task<Result> RunScriptAsync(
        string scriptText,
        Script.Options scriptOptions,
        CT ct
    )
    {
        return RunScriptAsync(new()
        {
            text = scriptText,
            options = scriptOptions
        }, ct);
    }

    public async Task<List<PSObject>> GetObjectListAsync(Script script)
    {
        return (await RunScriptAsync(script)).returnValue;
    }

    public async Task<PSObject> GetObjectAsync(Script script)
    {
        return (await GetObjectListAsync(script)).FirstOrDefault();
    }

    public async Task<Result> RunScriptAsync(Script script)
    {
        return await RunScriptAsync(script, CT.None);
    }

    public async Task<Result> RunScriptAsync(Script script, CT ct)
    {
        var scriptText = script.text;

        Logger.Script(scriptText);

        if (script.options.sensitiveArgs?.Length > 0)
        {
            scriptText = string.Format(scriptText, script.options.sensitiveArgs);
        }

        await @lock.WaitAsync();

        using var powershell = CreateShell();

        var scriptBlock = $"{{ {scriptText} }}";

        var args = $" -ScriptBlock ${nameof(scriptBlock)}";

        var proxy = powershell.Runspace.SessionStateProxy;
        var argumentList = script.options.argumentList;
        if (argumentList?.Length > 0)
        {
            args += $" -ArgumentList ${nameof(argumentList)}";
            proxy.SetVariable(nameof(argumentList), argumentList);
        }

        if (session is not null)
        {
            args += $" -Session ${nameof(session)}";
            proxy.SetVariable(nameof(session), session);
        }

        var suffix = string.Empty;
        if (script.options.outputMode == Script.Options.OutputMode.Json)
        {
            suffix += " | ConvertTo-Json -Compress";
        }

        var scriptContents
            = $"${nameof(scriptBlock)} = {scriptBlock}\r\n"
            + $"Invoke-Command {args}{suffix}";

        Result result;

        if (OnData is not null)
        {
            using var pipeline = runspace.CreatePipeline();
            pipeline.Commands.AddScript(scriptContents);
            result = await Result.InvokeAsync(pipeline, OnData, ct);
        }
        else
        {
            powershell.AddScript(scriptContents);
            result = Result.Invoke(powershell, ct);
        }

        @lock.Release();

        return result;
    }

    PowerShell CreateShell()
    {
        var powershell = PowerShell.Create();
        powershell.Runspace = runspace;
        return powershell;
    }

    Result RunCommandInternal(Command command)
    {
        using var powershell = CreateShell();
        powershell.Commands.AddCommand(command);
        return Result.Invoke(powershell);
    }

}
