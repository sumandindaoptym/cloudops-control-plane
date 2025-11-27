using System.Diagnostics;
using System.Text.Json;
using CloudOps.Agent.Services;

namespace CloudOps.Agent.Handlers;

public class DatabaseBackupHandler : IJobHandler
{
    private readonly ILogger<DatabaseBackupHandler> _logger;

    public DatabaseBackupHandler(ILogger<DatabaseBackupHandler> logger)
    {
        _logger = logger;
    }

    public string JobType => "database-backup";

    public async Task<JobExecutionResult> ExecuteAsync(JobExecutionContext context, CancellationToken cancellationToken)
    {
        var parameters = context.Job.Parameters ?? new Dictionary<string, object>();
        
        var databaseType = GetParameter(parameters, "databaseType", "postgresql");
        var connectionString = GetParameter(parameters, "connectionString", "");
        var databaseName = GetParameter(parameters, "databaseName", "");
        var outputPath = Path.Combine(context.WorkingDirectory, $"backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}.sql");

        _logger.LogInformation("Starting database backup for {DatabaseName} ({DatabaseType})", databaseName, databaseType);
        
        await context.ProgressCallback(10, "Preparing backup", "Validating connection parameters...");

        if (string.IsNullOrEmpty(connectionString) && string.IsNullOrEmpty(databaseName))
        {
            return new JobExecutionResult
            {
                Success = false,
                ErrorMessage = "Either connectionString or databaseName must be provided"
            };
        }

        try
        {
            await context.ProgressCallback(20, "Connecting to database", $"Connecting to {databaseName}...");

            string command;
            string arguments;

            if (databaseType.ToLowerInvariant() == "postgresql" || databaseType.ToLowerInvariant() == "postgres")
            {
                command = "pg_dump";
                arguments = string.IsNullOrEmpty(connectionString) 
                    ? $"-d {databaseName} -f \"{outputPath}\""
                    : $"\"{connectionString}\" -f \"{outputPath}\"";
                    
                if (!await CommandExistsAsync(command))
                {
                    return new JobExecutionResult
                    {
                        Success = false,
                        ErrorMessage = $"Required command '{command}' not found. Please install PostgreSQL client tools."
                    };
                }
            }
            else if (databaseType.ToLowerInvariant() == "sqlserver" || databaseType.ToLowerInvariant() == "mssql")
            {
                command = "sqlcmd";
                arguments = $"-Q \"BACKUP DATABASE [{databaseName}] TO DISK='{outputPath}'\"";
                
                if (!await CommandExistsAsync(command))
                {
                    return new JobExecutionResult
                    {
                        Success = false,
                        ErrorMessage = $"Required command '{command}' not found. Please install SQL Server command line tools."
                    };
                }
            }
            else
            {
                return new JobExecutionResult
                {
                    Success = false,
                    ErrorMessage = $"Unsupported database type: {databaseType}"
                };
            }

            await context.ProgressCallback(40, "Executing backup", "Running backup command...");

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

            await context.ProgressCallback(80, "Verifying backup", "Checking backup file...");

            if (process.ExitCode != 0)
            {
                _logger.LogError("Backup command failed with exit code {ExitCode}: {Error}", process.ExitCode, error);
                return new JobExecutionResult
                {
                    Success = false,
                    ErrorMessage = $"Backup failed: {error}"
                };
            }

            if (!File.Exists(outputPath))
            {
                return new JobExecutionResult
                {
                    Success = false,
                    ErrorMessage = "Backup file was not created"
                };
            }

            var fileInfo = new FileInfo(outputPath);
            await context.ProgressCallback(100, "Backup complete", $"Backup created: {fileInfo.Length / 1024}KB");

            _logger.LogInformation("Database backup completed successfully: {OutputPath} ({Size}KB)", 
                outputPath, fileInfo.Length / 1024);

            return new JobExecutionResult
            {
                Success = true,
                Result = $"Backup created successfully at {outputPath}",
                Artifacts = new List<string> { outputPath }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database backup");
            return new JobExecutionResult
            {
                Success = false,
                ErrorMessage = $"Backup failed: {ex.Message}"
            };
        }
    }

    private static async Task<bool> CommandExistsAsync(string command)
    {
        try
        {
            var whichCommand = OperatingSystem.IsWindows() ? "where" : "which";
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = whichCommand,
                    Arguments = command,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
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
