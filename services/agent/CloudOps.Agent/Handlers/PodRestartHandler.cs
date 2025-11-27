using System.Diagnostics;
using System.Text.Json;
using CloudOps.Agent.Services;

namespace CloudOps.Agent.Handlers;

public class PodRestartHandler : IJobHandler
{
    private readonly ILogger<PodRestartHandler> _logger;

    public PodRestartHandler(ILogger<PodRestartHandler> logger)
    {
        _logger = logger;
    }

    public string JobType => "restart-pods";

    public async Task<JobExecutionResult> ExecuteAsync(JobExecutionContext context, CancellationToken cancellationToken)
    {
        var parameters = context.Job.Parameters ?? new Dictionary<string, object>();
        
        var namespace_ = GetParameter(parameters, "namespace", "default");
        var deploymentName = GetParameter(parameters, "deploymentName", "");
        var podSelector = GetParameter(parameters, "podSelector", "");
        var restartStrategy = GetParameter(parameters, "restartStrategy", "rollout");

        _logger.LogInformation("Starting pod restart for {Deployment} in namespace {Namespace}", 
            deploymentName, namespace_);
        
        await context.ProgressCallback(10, "Preparing", "Validating Kubernetes context...");

        try
        {
            var kubectlCheck = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "kubectl",
                    Arguments = "version --client",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            
            kubectlCheck.Start();
            await kubectlCheck.WaitForExitAsync(cancellationToken);
            
            if (kubectlCheck.ExitCode != 0)
            {
                return new JobExecutionResult
                {
                    Success = false,
                    ErrorMessage = "kubectl is not available or not configured"
                };
            }

            await context.ProgressCallback(20, "Checking deployment", $"Verifying deployment {deploymentName}...");

            string command;
            string arguments;

            if (!string.IsNullOrEmpty(deploymentName))
            {
                if (restartStrategy == "rollout")
                {
                    command = "kubectl";
                    arguments = $"rollout restart deployment/{deploymentName} -n {namespace_}";
                }
                else
                {
                    command = "kubectl";
                    arguments = $"delete pods -l app={deploymentName} -n {namespace_}";
                }
            }
            else if (!string.IsNullOrEmpty(podSelector))
            {
                command = "kubectl";
                arguments = $"delete pods -l {podSelector} -n {namespace_}";
            }
            else
            {
                return new JobExecutionResult
                {
                    Success = false,
                    ErrorMessage = "Either deploymentName or podSelector must be provided"
                };
            }

            await context.ProgressCallback(40, "Executing restart", "Sending restart command...");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                _logger.LogError("Pod restart failed with exit code {ExitCode}: {Error}", process.ExitCode, error);
                return new JobExecutionResult
                {
                    Success = false,
                    ErrorMessage = $"Pod restart failed: {error}"
                };
            }

            await context.ProgressCallback(70, "Waiting for rollout", "Checking rollout status...");

            if (!string.IsNullOrEmpty(deploymentName) && restartStrategy == "rollout")
            {
                var statusProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "kubectl",
                        Arguments = $"rollout status deployment/{deploymentName} -n {namespace_} --timeout=300s",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                statusProcess.Start();
                var statusOutput = await statusProcess.StandardOutput.ReadToEndAsync(cancellationToken);
                await statusProcess.WaitForExitAsync(cancellationToken);

                if (statusProcess.ExitCode != 0)
                {
                    return new JobExecutionResult
                    {
                        Success = false,
                        ErrorMessage = "Rollout did not complete successfully within timeout"
                    };
                }
            }

            await context.ProgressCallback(100, "Complete", "Pod restart completed successfully");

            _logger.LogInformation("Pod restart completed successfully for {Deployment}", 
                deploymentName ?? podSelector);

            return new JobExecutionResult
            {
                Success = true,
                Result = $"Successfully restarted pods for {deploymentName ?? podSelector} in namespace {namespace_}. Output: {output}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during pod restart");
            return new JobExecutionResult
            {
                Success = false,
                ErrorMessage = $"Pod restart failed: {ex.Message}"
            };
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
}
