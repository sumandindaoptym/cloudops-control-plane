using System.CommandLine;
using CloudOps.Agent.Configuration;
using CloudOps.Agent.Handlers;
using CloudOps.Agent.Models;
using CloudOps.Agent.Services;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Serilog;

var rootCommand = new RootCommand("CloudOps Agent - Self-hosted agent for CloudOps Control Plane");

var runCommand = new Command("run", "Run the agent");
var configureCommand = new Command("configure", "Configure the agent");

var urlOption = new Option<string>("--url", "CloudOps API URL") { IsRequired = true };
var apiKeyOption = new Option<string>("--api-key", "Agent API key") { IsRequired = true };
var poolIdOption = new Option<Guid>("--pool", "Agent pool ID") { IsRequired = true };
var nameOption = new Option<string?>("--name", () => null, "Agent name (defaults to machine name)");
var maxJobsOption = new Option<int>("--max-jobs", () => 2, "Maximum parallel jobs");

runCommand.AddOption(urlOption);
runCommand.AddOption(apiKeyOption);
runCommand.AddOption(poolIdOption);
runCommand.AddOption(nameOption);
runCommand.AddOption(maxJobsOption);

configureCommand.AddOption(urlOption);
configureCommand.AddOption(apiKeyOption);
configureCommand.AddOption(poolIdOption);
configureCommand.AddOption(nameOption);
configureCommand.AddOption(maxJobsOption);

rootCommand.AddCommand(runCommand);
rootCommand.AddCommand(configureCommand);

runCommand.SetHandler(async (string url, string apiKey, Guid poolId, string? name, int maxJobs) =>
{
    await RunAgentAsync(url, apiKey, poolId, name, maxJobs);
}, urlOption, apiKeyOption, poolIdOption, nameOption, maxJobsOption);

configureCommand.SetHandler((string url, string apiKey, Guid poolId, string? name, int maxJobs) =>
{
    var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
    var config = $$"""
    {
      "Agent": {
        "ApiUrl": "{{url}}",
        "ApiKey": "{{apiKey}}",
        "PoolId": "{{poolId}}",
        "AgentName": "{{name ?? Environment.MachineName}}",
        "MaxParallelJobs": {{maxJobs}},
        "HeartbeatIntervalSeconds": 30,
        "JobPollIntervalSeconds": 5,
        "WorkingDirectory": "./work",
        "LogDirectory": "./logs"
      },
      "Serilog": {
        "MinimumLevel": {
          "Default": "Information",
          "Override": {
            "Microsoft": "Warning",
            "System": "Warning"
          }
        }
      }
    }
    """;
    
    File.WriteAllText(configPath, config);
    Console.WriteLine($"Configuration saved to: {configPath}");
    Console.WriteLine("Run 'cloudops-agent run' to start the agent.");
}, urlOption, apiKeyOption, poolIdOption, nameOption, maxJobsOption);

return await rootCommand.InvokeAsync(args);

async Task RunAgentAsync(string url, string apiKey, Guid poolId, string? name, int maxJobs)
{
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
        .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.File(
            Path.Combine("logs", "cloudops-agent-.log"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7)
        .CreateLogger();

    try
    {
        Log.Information("Starting CloudOps Agent v{Version}", 
            typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0");
        Log.Information("API URL: {ApiUrl}", url);
        Log.Information("Pool ID: {PoolId}", poolId);
        Log.Information("Machine: {MachineName}", Environment.MachineName);

        var builder = Host.CreateApplicationBuilder(args);
        
        builder.Services.Configure<AgentOptions>(options =>
        {
            options.ApiUrl = url;
            options.ApiKey = apiKey;
            options.PoolId = poolId;
            options.AgentName = name ?? Environment.MachineName;
            options.MaxParallelJobs = maxJobs;
        });

        builder.Services.AddSerilog();

        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, retryAttempt => 
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

        builder.Services.AddHttpClient<ICloudOpsApiClient, CloudOpsApiClient>()
            .AddPolicyHandler(retryPolicy);

        builder.Services.AddSingleton<JobExecutionService>();
        builder.Services.AddTransient<DatabaseBackupHandler>();
        builder.Services.AddTransient<DatabaseRestoreHandler>();
        builder.Services.AddTransient<ScriptExecutionHandler>();
        builder.Services.AddTransient<PodRestartHandler>();
        builder.Services.AddTransient<GenericJobHandler>();

        builder.Services.AddHostedService<AgentStartupService>();
        builder.Services.AddHostedService<HeartbeatService>();
        builder.Services.AddHostedService<JobPollingService>();

        var host = builder.Build();
        await host.RunAsync();
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Agent terminated unexpectedly");
    }
    finally
    {
        await Log.CloseAndFlushAsync();
    }
}

public class AgentStartupService : BackgroundService
{
    private readonly ICloudOpsApiClient _apiClient;
    private readonly ILogger<AgentStartupService> _logger;
    private readonly IHostApplicationLifetime _lifetime;

    public AgentStartupService(
        ICloudOpsApiClient apiClient,
        ILogger<AgentStartupService> logger,
        IHostApplicationLifetime lifetime)
    {
        _apiClient = apiClient;
        _logger = logger;
        _lifetime = lifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Registering agent with CloudOps API...");
        
        var agentInfo = AgentInfo.Collect();
        _logger.LogInformation("System Info: OS={OS}, Arch={Arch}, Cores={Cores}, Memory={Memory}GB",
            agentInfo.OperatingSystem, agentInfo.Architecture, agentInfo.LogicalCores, agentInfo.MemoryGb);

        var retries = 0;
        const int maxRetries = 5;
        
        while (retries < maxRetries && !stoppingToken.IsCancellationRequested)
        {
            var agentId = await _apiClient.RegisterAgentAsync(agentInfo, stoppingToken);
            
            if (agentId != null)
            {
                _logger.LogInformation("Agent registered successfully! Agent ID: {AgentId}", agentId);
                return;
            }
            
            retries++;
            var delay = TimeSpan.FromSeconds(Math.Pow(2, retries));
            _logger.LogWarning("Registration failed, retrying in {Delay}s (attempt {Attempt}/{MaxRetries})", 
                delay.TotalSeconds, retries, maxRetries);
            
            await Task.Delay(delay, stoppingToken);
        }
        
        _logger.LogError("Failed to register agent after {MaxRetries} attempts. Shutting down.", maxRetries);
        _lifetime.StopApplication();
    }
}
