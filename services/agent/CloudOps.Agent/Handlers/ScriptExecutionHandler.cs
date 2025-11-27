using System.Diagnostics;
using System.Text.Json;
using CloudOps.Agent.Services;

namespace CloudOps.Agent.Handlers;

public class ScriptExecutionHandler : IJobHandler
{
    private readonly ILogger<ScriptExecutionHandler> _logger;

    public ScriptExecutionHandler(ILogger<ScriptExecutionHandler> logger)
    {
        _logger = logger;
    }

    public string JobType => "script";

    public async Task<JobExecutionResult> ExecuteAsync(JobExecutionContext context, CancellationToken cancellationToken)
    {
        var parameters = context.Job.Parameters ?? new Dictionary<string, object>();
        
        var scriptType = GetParameter(parameters, "scriptType", "bash");
        var script = GetParameter(parameters, "script", "");
        var scriptPath = GetParameter(parameters, "scriptPath", "");
        var arguments = GetParameter(parameters, "arguments", "");
        var timeoutMinutes = GetParameterInt(parameters, "timeoutMinutes", 30);

        _logger.LogInformation("Starting script execution ({ScriptType})", scriptType);
        
        await context.ProgressCallback(10, "Preparing script", "Setting up execution environment...");

        string command;
        string commandArgs;
        string? tempScriptPath = null;

        try
        {
            if (!string.IsNullOrEmpty(script))
            {
                var extension = scriptType.ToLowerInvariant() switch
                {
                    "powershell" or "pwsh" => ".ps1",
                    "python" => ".py",
                    _ => ".sh"
                };
                
                tempScriptPath = Path.Combine(context.WorkingDirectory, $"script_{Guid.NewGuid():N}{extension}");
                await File.WriteAllTextAsync(tempScriptPath, script, cancellationToken);
                scriptPath = tempScriptPath;
                
                if (scriptType.ToLowerInvariant() == "bash" && OperatingSystem.IsLinux())
                {
                    Process.Start("chmod", $"+x \"{tempScriptPath}\"")?.WaitForExit();
                }
            }

            if (string.IsNullOrEmpty(scriptPath))
            {
                return new JobExecutionResult
                {
                    Success = false,
                    ErrorMessage = "Either 'script' or 'scriptPath' must be provided"
                };
            }

            if (!File.Exists(scriptPath))
            {
                return new JobExecutionResult
                {
                    Success = false,
                    ErrorMessage = $"Script file not found: {scriptPath}"
                };
            }

            switch (scriptType.ToLowerInvariant())
            {
                case "powershell" or "pwsh":
                    command = OperatingSystem.IsWindows() ? "powershell" : "pwsh";
                    commandArgs = $"-ExecutionPolicy Bypass -File \"{scriptPath}\" {arguments}";
                    break;
                case "python":
                    command = "python3";
                    commandArgs = $"\"{scriptPath}\" {arguments}";
                    break;
                default:
                    command = OperatingSystem.IsWindows() ? "bash" : "/bin/bash";
                    commandArgs = $"\"{scriptPath}\" {arguments}";
                    break;
            }

            await context.ProgressCallback(30, "Executing script", $"Running {scriptType} script...");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = commandArgs,
                    WorkingDirectory = context.WorkingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            var outputBuilder = new System.Text.StringBuilder();
            var errorBuilder = new System.Text.StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    outputBuilder.AppendLine(e.Data);
                    _logger.LogDebug("[SCRIPT] {Output}", e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    errorBuilder.AppendLine(e.Data);
                    _logger.LogWarning("[SCRIPT ERROR] {Error}", e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(timeoutMinutes));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            try
            {
                await process.WaitForExitAsync(linkedCts.Token);
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                process.Kill(true);
                return new JobExecutionResult
                {
                    Success = false,
                    ErrorMessage = $"Script execution timed out after {timeoutMinutes} minutes"
                };
            }

            await context.ProgressCallback(90, "Processing results", "Script execution completed");

            var output = outputBuilder.ToString();
            var error = errorBuilder.ToString();

            if (process.ExitCode != 0)
            {
                _logger.LogError("Script failed with exit code {ExitCode}: {Error}", process.ExitCode, error);
                return new JobExecutionResult
                {
                    Success = false,
                    ErrorMessage = $"Script failed with exit code {process.ExitCode}: {error}",
                    Result = output
                };
            }

            await context.ProgressCallback(100, "Complete", "Script executed successfully");

            _logger.LogInformation("Script executed successfully");

            return new JobExecutionResult
            {
                Success = true,
                Result = output
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during script execution");
            return new JobExecutionResult
            {
                Success = false,
                ErrorMessage = $"Script execution failed: {ex.Message}"
            };
        }
        finally
        {
            if (tempScriptPath != null && File.Exists(tempScriptPath))
            {
                try { File.Delete(tempScriptPath); } catch { }
            }
        }
    }

    private static string GetParameter(Dictionary<string, object> parameters, string key, string defaultValue)
    {
        if (parameters.TryGetValue(key, out var value))
        {
            if (value is JsonElement jsonElement)
            {
                return jsonElement.GetString() ?? defaultValue;
            }
            return value?.ToString() ?? defaultValue;
        }
        return defaultValue;
    }

    private static int GetParameterInt(Dictionary<string, object> parameters, string key, int defaultValue)
    {
        if (parameters.TryGetValue(key, out var value))
        {
            if (value is JsonElement jsonElement)
            {
                return jsonElement.TryGetInt32(out var intValue) ? intValue : defaultValue;
            }
            if (int.TryParse(value?.ToString(), out var parsed))
            {
                return parsed;
            }
        }
        return defaultValue;
    }
}
