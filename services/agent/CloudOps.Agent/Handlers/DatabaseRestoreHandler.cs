using System.Diagnostics;
using System.Text.Json;
using CloudOps.Agent.Services;

namespace CloudOps.Agent.Handlers;

public class DatabaseRestoreHandler : IJobHandler
{
    private readonly ILogger<DatabaseRestoreHandler> _logger;

    public DatabaseRestoreHandler(ILogger<DatabaseRestoreHandler> logger)
    {
        _logger = logger;
    }

    public string JobType => "database-restore";

    public async Task<JobExecutionResult> ExecuteAsync(JobExecutionContext context, CancellationToken cancellationToken)
    {
        var parameters = context.Job.Parameters ?? new Dictionary<string, object>();
        
        var databaseType = GetParameter(parameters, "databaseType", "postgresql");
        var connectionString = GetParameter(parameters, "connectionString", "");
        var databaseName = GetParameter(parameters, "databaseName", "");
        var backupPath = GetParameter(parameters, "backupPath", "");

        _logger.LogInformation("Starting database restore for {DatabaseName} ({DatabaseType})", databaseName, databaseType);
        
        await context.ProgressCallback(10, "Validating parameters", "Checking backup file...");

        if (string.IsNullOrEmpty(backupPath))
        {
            return new JobExecutionResult
            {
                Success = false,
                ErrorMessage = "backupPath must be provided"
            };
        }

        if (!File.Exists(backupPath))
        {
            return new JobExecutionResult
            {
                Success = false,
                ErrorMessage = $"Backup file not found: {backupPath}"
            };
        }

        try
        {
            await context.ProgressCallback(20, "Preparing restore", $"Restoring {databaseName} from backup...");

            string command;
            string arguments;

            if (databaseType.ToLowerInvariant() == "postgresql" || databaseType.ToLowerInvariant() == "postgres")
            {
                command = "psql";
                arguments = string.IsNullOrEmpty(connectionString) 
                    ? $"-d {databaseName} -f \"{backupPath}\""
                    : $"\"{connectionString}\" -f \"{backupPath}\"";
            }
            else if (databaseType.ToLowerInvariant() == "sqlserver" || databaseType.ToLowerInvariant() == "mssql")
            {
                command = "sqlcmd";
                arguments = $"-Q \"RESTORE DATABASE [{databaseName}] FROM DISK='{backupPath}'\"";
            }
            else
            {
                return new JobExecutionResult
                {
                    Success = false,
                    ErrorMessage = $"Unsupported database type: {databaseType}"
                };
            }

            await context.ProgressCallback(40, "Executing restore", "Running restore command...");

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

            await context.ProgressCallback(90, "Verifying restore", "Checking database state...");

            if (process.ExitCode != 0)
            {
                _logger.LogError("Restore command failed with exit code {ExitCode}: {Error}", process.ExitCode, error);
                return new JobExecutionResult
                {
                    Success = false,
                    ErrorMessage = $"Restore failed: {error}"
                };
            }

            await context.ProgressCallback(100, "Restore complete", $"Database {databaseName} restored successfully");

            _logger.LogInformation("Database restore completed successfully for {DatabaseName}", databaseName);

            return new JobExecutionResult
            {
                Success = true,
                Result = $"Database {databaseName} restored successfully from {backupPath}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database restore");
            return new JobExecutionResult
            {
                Success = false,
                ErrorMessage = $"Restore failed: {ex.Message}"
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
