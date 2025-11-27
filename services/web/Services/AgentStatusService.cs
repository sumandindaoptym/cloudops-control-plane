namespace CloudOps.Web.Services;

public class AgentStatusService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AgentStatusService> _logger;
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(30);

    public AgentStatusService(IServiceScopeFactory scopeFactory, ILogger<AgentStatusService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Agent Status Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var agentService = scope.ServiceProvider.GetRequiredService<IAgentService>();
                await agentService.UpdateAgentStatusesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating agent statuses");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }

        _logger.LogInformation("Agent Status Service stopped");
    }
}
