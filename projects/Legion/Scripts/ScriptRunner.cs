using System.Diagnostics;
using Legion.Package;
using Legion.Workspace;

namespace Legion.Scripts;

public class ScriptRunner
{
    private readonly string _workingDirectory;
    private readonly Dictionary<string, string> _environmentVariables;

    public ScriptRunner(string workingDirectory, Dictionary<string, string>? environmentVariables = null)
    {
        _workingDirectory = workingDirectory;
        _environmentVariables = environmentVariables ?? new Dictionary<string, string>();
    }

    public async Task<ScriptResult> RunAsync(string script)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var (command, arguments) = ParseCommand(script);

            var processInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                WorkingDirectory = _workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            foreach (var env in _environmentVariables)
            {
                processInfo.Environment[env.Key] = env.Value;
            }

            using var process = new Process();
            process.StartInfo = processInfo;

            var outputBuilder = new System.Text.StringBuilder();
            var errorBuilder = new System.Text.StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data is not null)
                {
                    outputBuilder.AppendLine(e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data is not null)
                {
                    errorBuilder.AppendLine(e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await Task.Run(() => process.WaitForExit());

            stopwatch.Stop();

            return new ScriptResult
            {
                Success = process.ExitCode == 0,
                ExitCode = process.ExitCode,
                Output = outputBuilder.ToString(),
                Error = errorBuilder.ToString(),
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            return new ScriptResult
            {
                Success = false,
                ExitCode = -1,
                Output = string.Empty,
                Error = ex.Message,
                Duration = stopwatch.Elapsed
            };
        }
    }

    public async Task<ScriptResult> RunScriptAsync(LegionManifest manifest, string scriptName)
    {
        if (!manifest.HasScript(scriptName))
        {
            return new ScriptResult
            {
                Success = false,
                ExitCode = -1,
                Error = $"脚本 '{scriptName}' 在 legion.von 中未定义"
            };
        }

        string script = manifest.GetScript(scriptName);
        Console.WriteLine($"> legion run {scriptName}");
        Console.WriteLine($"> {script}");

        return await RunAsync(script);
    }

    public async Task<ScriptResult> RunScriptAsync(LegionsWorkspace workspace, string scriptName)
    {
        if (!workspace.HasScript(scriptName))
        {
            return new ScriptResult
            {
                Success = false,
                ExitCode = -1,
                Error = $"脚本 '{scriptName}' 在 voa.workspace.v 中未定义"
            };
        }

        string script = workspace.GetScript(scriptName);
        Console.WriteLine($"> legion run {scriptName}");
        Console.WriteLine($"> {script}");

        return await RunAsync(script);
    }

    public async Task<ScriptResult> RunLifecycleHookAsync(string hookName, LegionManifest manifest)
    {
        if (!manifest.HasScript(hookName))
        {
            return new ScriptResult
            {
                Success = true,
                ExitCode = 0,
                Output = $"生命周期钩子 '{hookName}' 未定义，跳过"
            };
        }

        Console.WriteLine($"> 执行生命周期钩子: {hookName}");
        return await RunScriptAsync(manifest, hookName);
    }

    public List<string> ListScripts(LegionManifest manifest)
    {
        return manifest.Scripts.Keys.ToList();
    }

    public List<string> ListScripts(LegionsWorkspace workspace)
    {
        return workspace.Scripts.Keys.ToList();
    }

    private (string command, string arguments) ParseCommand(string script, string? shell = null)
    {
        script = script.Trim();

        if (script.StartsWith("legion "))
        {
            string legionCommand = script.Substring(7).Trim();
            return ("legion", legionCommand);
        }

        if (script.StartsWith("vcc "))
        {
            string vccCommand = script.Substring(4).Trim();
            return ("vcc", vccCommand);
        }

        if (!string.IsNullOrEmpty(shell))
        {
            return shell.ToLowerInvariant() switch
            {
                "powershell" or "pwsh" => ("pwsh", $"-Command \"{script.Replace("\"", "\\\"")}\""),
                "bash" => ("/bin/bash", $"-c \"{script.Replace("\"", "\\\"")}\""),
                "sh" => ("/bin/sh", $"-c \"{script.Replace("\"", "\\\"")}\""),
                "zsh" => ("/bin/zsh", $"-c \"{script.Replace("\"", "\\\"")}\""),
                "cmd" => ("cmd", $"/c {script}"),
                _ => ("cmd", $"/c {script}")
            };
        }

        if (OperatingSystem.IsWindows())
        {
            return ("cmd", $"/c {script}");
        }
        else
        {
            return ("/bin/sh", $"-c \"{script.Replace("\"", "\\\"")}\"");
        }
    }
}