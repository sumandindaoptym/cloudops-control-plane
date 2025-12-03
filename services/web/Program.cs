using CloudOps.Web.Data;
using CloudOps.Web.Hubs;
using CloudOps.Web.Models;
using CloudOps.Web.Services;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Azure.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

var builder = WebApplication.CreateBuilder(args);

var azureTenantId = Environment.GetEnvironmentVariable("AZURE_AD_TENANT_ID") ?? builder.Configuration["AzureAd:TenantId"];
var azureClientId = Environment.GetEnvironmentVariable("AZURE_AD_CLIENT_ID") ?? builder.Configuration["AzureAd:ClientId"];
var azureClientSecret = Environment.GetEnvironmentVariable("AZURE_AD_CLIENT_SECRET") ?? builder.Configuration["AzureAd:ClientSecret"];

builder.Configuration["AzureAd:TenantId"] = azureTenantId;
builder.Configuration["AzureAd:ClientId"] = azureClientId;
builder.Configuration["AzureAd:ClientSecret"] = azureClientSecret;

builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(options =>
    {
        builder.Configuration.Bind("AzureAd", options);
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.Scope.Add("offline_access");
        options.Scope.Add("https://management.azure.com/user_impersonation");
        options.ResponseType = "code";
        options.SignedOutRedirectUri = "/GoodBye";
        options.Events = new OpenIdConnectEvents
        {
            OnRedirectToIdentityProvider = context =>
            {
                if (context.HttpContext.Request.Headers.ContainsKey("X-Forwarded-Proto"))
                {
                    context.ProtocolMessage.RedirectUri = context.ProtocolMessage.RedirectUri?.Replace("http://", "https://");
                }
                return Task.CompletedTask;
            },
            OnTicketReceived = context =>
            {
                if (string.IsNullOrEmpty(context.Properties.RedirectUri) || context.Properties.RedirectUri == "/")
                {
                    context.Properties.RedirectUri = "/Dashboard";
                }
                return Task.CompletedTask;
            },
            OnRedirectToIdentityProviderForSignOut = context =>
            {
                if (context.HttpContext.Request.Headers.ContainsKey("X-Forwarded-Proto"))
                {
                    context.ProtocolMessage.PostLogoutRedirectUri = context.ProtocolMessage.PostLogoutRedirectUri?.Replace("http://", "https://");
                }
                return Task.CompletedTask;
            }
        };
    })
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

builder.Services.AddAuthorization();

builder.Services.AddControllersWithViews();

builder.Services.AddRazorPages()
    .AddMicrosoftIdentityUI();

builder.Services.AddHttpClient<IAzureSubscriptionService, AzureSubscriptionService>();
builder.Services.AddScoped<CloudOps.Web.Services.IServiceBusResourceService, CloudOps.Web.Services.ServiceBusResourceService>();
builder.Services.AddScoped<CloudOps.Web.Services.IServiceBusRuntimeService, CloudOps.Web.Services.ServiceBusRuntimeService>();

var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrEmpty(databaseUrl))
{
    var connectionString = ConvertDatabaseUrlToConnectionString(databaseUrl);
    builder.Services.AddDbContext<ActivityDbContext>(options =>
        options.UseNpgsql(connectionString));
}

static string ConvertDatabaseUrlToConnectionString(string databaseUrl)
{
    if (databaseUrl.StartsWith("postgresql://") || databaseUrl.StartsWith("postgres://"))
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':');
        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 5432;
        var database = uri.AbsolutePath.TrimStart('/');
        var user = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : "";
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
        
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        var sslMode = query["sslmode"] ?? "require";
        
        return $"Host={host};Port={port};Database={database};Username={user};Password={password};SSL Mode={MapSslMode(sslMode)}";
    }
    return databaseUrl;
}

static string MapSslMode(string sslMode)
{
    return sslMode.ToLowerInvariant() switch
    {
        "disable" => "Disable",
        "allow" => "Allow",
        "prefer" => "Prefer",
        "require" => "Require",
        "verify-ca" => "VerifyCA",
        "verify-full" => "VerifyFull",
        _ => "Require"
    };
}
builder.Services.AddScoped<IActivityService, ActivityService>();
builder.Services.AddScoped<IAgentService, AgentService>();

builder.Services.AddSingleton<IPurgeQueue, PurgeQueue>();
builder.Services.AddSingleton<IRunningJobsTracker, RunningJobsTracker>();
builder.Services.AddHostedService<PurgeBackgroundService>();
builder.Services.AddHostedService<AgentStatusService>();

var azureSignalRConnectionString = Environment.GetEnvironmentVariable("AZURE_SIGNALR_CONNECTION_STRING");
if (!string.IsNullOrEmpty(azureSignalRConnectionString))
{
    builder.Services.AddSignalR().AddAzureSignalR(options =>
    {
        options.ConnectionString = azureSignalRConnectionString;
    });
}
else
{
    builder.Services.AddSignalR();
}

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5000);
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetService<ActivityDbContext>();
    if (dbContext != null)
    {
        await dbContext.Database.EnsureCreatedAsync();
    }
}

app.UseForwardedHeaders();

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/MicrosoftIdentity/Account/SignedOut") ||
        context.Request.Path.StartsWithSegments("/Account/SignedOut"))
    {
        context.Response.Redirect("/GoodBye");
        return;
    }
    await next();
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/subscriptions", async (
    HttpContext httpContext, 
    IAzureSubscriptionService subscriptionService, 
    Microsoft.Identity.Web.ITokenAcquisition tokenAcquisition,
    ILogger<Program> logger) =>
{
    if (!httpContext.User.Identity?.IsAuthenticated ?? true)
    {
        return Results.Unauthorized();
    }

    try
    {
        var accessToken = await tokenAcquisition.GetAccessTokenForUserAsync(new[] { "https://management.azure.com/user_impersonation" });
        var subscriptions = await subscriptionService.GetSubscriptionsAsync(accessToken);
        return Results.Ok(subscriptions);
    }
    catch (Microsoft.Identity.Web.MicrosoftIdentityWebChallengeUserException)
    {
        logger.LogWarning("User needs to consent to Azure Management API scope");
        return Results.Json(
            new { error = "Please sign out and sign back in to grant Azure subscription access" },
            statusCode: 403
        );
    }
    catch (HttpRequestException ex)
    {
        logger.LogError(ex, "Azure API request failed");
        return Results.Json(
            new { error = $"Failed to fetch subscriptions from Azure: {ex.Message}" },
            statusCode: 502
        );
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unexpected error fetching subscriptions");
        return Results.Json(
            new { error = "An unexpected error occurred while fetching subscriptions" },
            statusCode: 500
        );
    }
}).RequireAuthorization();

app.MapGet("/api/azure/servicebus/namespaces", async (
    HttpContext httpContext,
    string subscriptionId,
    CloudOps.Web.Services.IServiceBusResourceService serviceBusService,
    Microsoft.Identity.Web.ITokenAcquisition tokenAcquisition,
    ILogger<Program> logger) =>
{
    if (!httpContext.User.Identity?.IsAuthenticated ?? true)
    {
        return Results.Unauthorized();
    }

    try
    {
        var accessToken = await tokenAcquisition.GetAccessTokenForUserAsync(new[] { "https://management.azure.com/user_impersonation" });
        var namespaces = await serviceBusService.GetNamespacesAsync(subscriptionId, accessToken);
        return Results.Ok(namespaces);
    }
    catch (Microsoft.Identity.Web.MicrosoftIdentityWebChallengeUserException)
    {
        logger.LogWarning("User needs to consent to Azure Management API scope");
        return Results.Json(
            new { error = "Please sign out and sign back in to grant Azure subscription access" },
            statusCode: 403
        );
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error fetching Service Bus namespaces");
        return Results.Json(
            new { error = "Failed to fetch Service Bus namespaces" },
            statusCode: 500
        );
    }
}).RequireAuthorization();

app.MapGet("/api/azure/servicebus/queues", async (
    HttpContext httpContext,
    string subscriptionId,
    string resourceGroup,
    string namespaceName,
    CloudOps.Web.Services.IServiceBusResourceService serviceBusService,
    Microsoft.Identity.Web.ITokenAcquisition tokenAcquisition,
    ILogger<Program> logger) =>
{
    if (!httpContext.User.Identity?.IsAuthenticated ?? true)
    {
        return Results.Unauthorized();
    }

    try
    {
        var accessToken = await tokenAcquisition.GetAccessTokenForUserAsync(new[] { "https://management.azure.com/user_impersonation" });
        var queues = await serviceBusService.GetQueuesAsync(subscriptionId, resourceGroup, namespaceName, accessToken);
        return Results.Ok(queues);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error fetching queues");
        return Results.Json(
            new { error = "Failed to fetch queues" },
            statusCode: 500
        );
    }
}).RequireAuthorization();

app.MapGet("/api/azure/servicebus/topics", async (
    HttpContext httpContext,
    string subscriptionId,
    string resourceGroup,
    string namespaceName,
    CloudOps.Web.Services.IServiceBusResourceService serviceBusService,
    Microsoft.Identity.Web.ITokenAcquisition tokenAcquisition,
    ILogger<Program> logger) =>
{
    if (!httpContext.User.Identity?.IsAuthenticated ?? true)
    {
        return Results.Unauthorized();
    }

    try
    {
        var accessToken = await tokenAcquisition.GetAccessTokenForUserAsync(new[] { "https://management.azure.com/user_impersonation" });
        var topics = await serviceBusService.GetTopicsAsync(subscriptionId, resourceGroup, namespaceName, accessToken);
        return Results.Ok(topics);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error fetching topics");
        return Results.Json(
            new { error = "Failed to fetch topics" },
            statusCode: 500
        );
    }
}).RequireAuthorization();

app.MapGet("/api/azure/servicebus/topics/{topicName}/subscriptions", async (
    HttpContext httpContext,
    string topicName,
    string subscriptionId,
    string resourceGroup,
    string namespaceName,
    CloudOps.Web.Services.IServiceBusResourceService serviceBusService,
    Microsoft.Identity.Web.ITokenAcquisition tokenAcquisition,
    ILogger<Program> logger) =>
{
    if (!httpContext.User.Identity?.IsAuthenticated ?? true)
    {
        return Results.Unauthorized();
    }

    try
    {
        var accessToken = await tokenAcquisition.GetAccessTokenForUserAsync(new[] { "https://management.azure.com/user_impersonation" });
        var subscriptions = await serviceBusService.GetSubscriptionsAsync(subscriptionId, resourceGroup, namespaceName, topicName, accessToken);
        return Results.Ok(subscriptions);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error fetching topic subscriptions");
        return Results.Json(
            new { error = "Failed to fetch topic subscriptions" },
            statusCode: 500
        );
    }
}).RequireAuthorization();

app.MapPost("/api/azure/servicebus/dlq/count", async (
    HttpContext httpContext,
    CloudOps.Web.Models.DlqCountRequest request,
    CloudOps.Web.Services.IServiceBusRuntimeService runtimeService,
    Microsoft.Identity.Web.ITokenAcquisition tokenAcquisition,
    ILogger<Program> logger) =>
{
    if (!httpContext.User.Identity?.IsAuthenticated ?? true)
    {
        return Results.Unauthorized();
    }

    try
    {
        var accessToken = await tokenAcquisition.GetAccessTokenForUserAsync(new[] { "https://servicebus.azure.net/user_impersonation" });
        var result = await runtimeService.GetDlqCountAsync(request, accessToken);
        return Results.Ok(result);
    }
    catch (Microsoft.Identity.Web.MicrosoftIdentityWebChallengeUserException)
    {
        logger.LogWarning("User needs to consent to Service Bus scope");
        return Results.Json(
            new { error = "Please sign out and sign back in to grant Service Bus access" },
            statusCode: 403
        );
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error getting DLQ count");
        return Results.Json(
            new { error = "Failed to get DLQ count" },
            statusCode: 500
        );
    }
}).RequireAuthorization();

app.MapPost("/api/azure/servicebus/dlq/purge", async (
    HttpContext httpContext,
    CloudOps.Web.Models.PurgeRequest request,
    IPurgeQueue purgeQueue,
    Microsoft.Identity.Web.ITokenAcquisition tokenAcquisition,
    ILogger<Program> logger) =>
{
    if (!httpContext.User.Identity?.IsAuthenticated ?? true)
    {
        return Results.Unauthorized();
    }

    try
    {
        var accessToken = await tokenAcquisition.GetAccessTokenForUserAsync(new[] { "https://servicebus.azure.net/user_impersonation" });
        
        var purgeId = Guid.NewGuid().ToString("N")[..12];
        
        var userId = httpContext.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value 
            ?? httpContext.User.FindFirst("oid")?.Value 
            ?? string.Empty;
        var userEmail = httpContext.User.FindFirst("preferred_username")?.Value 
            ?? httpContext.User.FindFirst("email")?.Value 
            ?? string.Empty;
        
        await purgeQueue.QueuePurgeAsync(new PurgeJob
        {
            PurgeId = purgeId,
            Request = request,
            AccessToken = accessToken,
            UserId = userId,
            UserEmail = userEmail,
            SubscriptionName = request.SubscriptionName ?? "Unknown"
        });

        logger.LogInformation("Queued purge job {PurgeId} for {EntityType} {EntityName}", 
            purgeId, request.EntityType, request.EntityName);

        return Results.Ok(new { purgeId, status = "queued" });
    }
    catch (Microsoft.Identity.Web.MicrosoftIdentityWebChallengeUserException)
    {
        logger.LogWarning("User needs to consent to Service Bus scope");
        return Results.Json(
            new { error = "Please sign out and sign back in to grant Service Bus access" },
            statusCode: 403
        );
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error queuing purge job");
        return Results.Json(
            new { error = $"Failed to queue purge: {ex.Message}" },
            statusCode: 500
        );
    }
}).RequireAuthorization();

app.MapHub<PurgeHub>("/hubs/purge");

app.MapGet("/api/activities", async (
    HttpContext httpContext,
    IActivityService activityService,
    IRunningJobsTracker jobsTracker,
    ILogger<Program> logger) =>
{
    if (!httpContext.User.Identity?.IsAuthenticated ?? true)
    {
        return Results.Unauthorized();
    }

    try
    {
        var userId = httpContext.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value 
            ?? httpContext.User.FindFirst("oid")?.Value 
            ?? string.Empty;
        
        var activities = await activityService.GetUserActivitiesAsync(userId);
        
        var reconciledActivities = new List<object>();
        foreach (var activity in activities)
        {
            var isActuallyRunning = activity.Status == "Running" && jobsTracker.IsActivityRunning(activity.Id);
            var wasAborted = activity.Status == "Running" && !jobsTracker.IsActivityRunning(activity.Id);
            
            if (wasAborted)
            {
                await activityService.UpdateActivityAsync(activity.Id, "Cancelled", null, "Job was interrupted (server restart or unexpected termination)");
                activity.Status = "Cancelled";
                activity.EndTime = DateTime.UtcNow;
                activity.ErrorMessage = "Job was interrupted (server restart or unexpected termination)";
            }
            
            var runningJob = isActuallyRunning ? jobsTracker.GetJobByActivityId(activity.Id) : null;
            
            reconciledActivities.Add(new 
            {
                activity.Id,
                activity.UserId,
                activity.UserEmail,
                activity.TaskName,
                activity.TaskType,
                activity.SubscriptionName,
                activity.SubscriptionId,
                activity.ResourceName,
                activity.SubResourceName,
                activity.Status,
                activity.ItemsProcessed,
                activity.ErrorMessage,
                activity.StartTime,
                activity.EndTime,
                IsActuallyRunning = isActuallyRunning,
                PurgeId = runningJob?.PurgeId
            });
        }
        
        return Results.Ok(reconciledActivities);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error fetching activities");
        return Results.Json(
            new { error = "Failed to fetch activities" },
            statusCode: 500
        );
    }
}).RequireAuthorization();

app.MapGet("/api/running-jobs", (
    HttpContext httpContext,
    IRunningJobsTracker jobsTracker,
    ILogger<Program> logger) =>
{
    if (!httpContext.User.Identity?.IsAuthenticated ?? true)
    {
        return Results.Unauthorized();
    }

    try
    {
        var userId = httpContext.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value 
            ?? httpContext.User.FindFirst("oid")?.Value 
            ?? string.Empty;
        
        var jobs = jobsTracker.GetJobsForUser(userId).Select(j => new
        {
            j.PurgeId,
            j.ActivityId,
            j.NamespaceName,
            j.EntityName,
            j.EntityType,
            j.TopicSubscriptionName,
            j.Status,
            j.Progress,
            j.TotalPurged,
            j.StartTime,
            j.EndTime,
            LogCount = j.Logs.Count
        });
        
        return Results.Ok(jobs);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error fetching running jobs");
        return Results.Json(new { error = "Failed to fetch running jobs" }, statusCode: 500);
    }
}).RequireAuthorization();

app.MapGet("/api/running-jobs/{purgeId}/logs", (
    string purgeId,
    HttpContext httpContext,
    IRunningJobsTracker jobsTracker,
    int? skip,
    ILogger<Program> logger) =>
{
    if (!httpContext.User.Identity?.IsAuthenticated ?? true)
    {
        return Results.Unauthorized();
    }

    try
    {
        var userId = httpContext.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value 
            ?? httpContext.User.FindFirst("oid")?.Value 
            ?? string.Empty;
            
        var job = jobsTracker.GetJob(purgeId);
        if (job == null)
        {
            return Results.NotFound(new { error = "Job not found" });
        }
        
        if (job.UserId != userId)
        {
            return Results.Forbid();
        }
        
        var logs = jobsTracker.GetLogs(purgeId, skip ?? 0).Select(l => new
        {
            l.Timestamp,
            l.Message,
            l.Level
        });
        
        return Results.Ok(new
        {
            job.PurgeId,
            job.Status,
            job.Progress,
            job.TotalPurged,
            Logs = logs
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error fetching job logs");
        return Results.Json(new { error = "Failed to fetch job logs" }, statusCode: 500);
    }
}).RequireAuthorization();

app.MapGet("/api/agentpools", async (
    HttpContext httpContext,
    IAgentService agentService,
    ILogger<Program> logger) =>
{
    if (!httpContext.User.Identity?.IsAuthenticated ?? true)
    {
        return Results.Unauthorized();
    }

    try
    {
        var pools = await agentService.GetAllPoolsAsync();
        var response = pools.Select(p => new AgentPoolResponse
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            IsHosted = p.IsHosted,
            TotalAgents = p.Agents.Count,
            OnlineAgents = p.Agents.Count(a => a.Status == "Online" || a.Status == "Busy"),
            OfflineAgents = p.Agents.Count(a => a.Status == "Offline"),
            RunningJobs = p.Agents.Sum(a => a.CurrentRunningJobs),
            CreatedAt = p.CreatedAt
        });
        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error fetching agent pools");
        return Results.Json(new { error = "Failed to fetch agent pools" }, statusCode: 500);
    }
}).RequireAuthorization();

app.MapGet("/api/agentpools/{id:guid}", async (
    Guid id,
    HttpContext httpContext,
    IAgentService agentService,
    ILogger<Program> logger) =>
{
    if (!httpContext.User.Identity?.IsAuthenticated ?? true)
    {
        return Results.Unauthorized();
    }

    try
    {
        var pool = await agentService.GetPoolByIdAsync(id);
        if (pool == null) return Results.NotFound();
        
        return Results.Ok(new AgentPoolResponse
        {
            Id = pool.Id,
            Name = pool.Name,
            Description = pool.Description,
            IsHosted = pool.IsHosted,
            TotalAgents = pool.Agents.Count,
            OnlineAgents = pool.Agents.Count(a => a.Status == "Online" || a.Status == "Busy"),
            OfflineAgents = pool.Agents.Count(a => a.Status == "Offline"),
            RunningJobs = pool.Agents.Sum(a => a.CurrentRunningJobs),
            CreatedAt = pool.CreatedAt
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error fetching agent pool");
        return Results.Json(new { error = "Failed to fetch agent pool" }, statusCode: 500);
    }
}).RequireAuthorization();

app.MapPost("/api/agentpools", async (
    HttpContext httpContext,
    AgentPoolRequest request,
    IAgentService agentService,
    ILogger<Program> logger) =>
{
    if (!httpContext.User.Identity?.IsAuthenticated ?? true)
    {
        return Results.Unauthorized();
    }

    try
    {
        var userId = httpContext.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value 
            ?? httpContext.User.FindFirst("oid")?.Value 
            ?? string.Empty;
        var userEmail = httpContext.User.FindFirst("preferred_username")?.Value 
            ?? httpContext.User.FindFirst("email")?.Value 
            ?? httpContext.User.Identity?.Name 
            ?? string.Empty;

        var pool = await agentService.CreatePoolAsync(userId, userEmail, request);
        return Results.Ok(new AgentPoolResponse
        {
            Id = pool.Id,
            Name = pool.Name,
            Description = pool.Description,
            IsHosted = pool.IsHosted,
            TotalAgents = 0,
            OnlineAgents = 0,
            OfflineAgents = 0,
            RunningJobs = 0,
            CreatedAt = pool.CreatedAt
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error creating agent pool");
        return Results.Json(new { error = "Failed to create agent pool" }, statusCode: 500);
    }
}).RequireAuthorization();

app.MapPut("/api/agentpools/{id:guid}", async (
    Guid id,
    HttpContext httpContext,
    AgentPoolRequest request,
    IAgentService agentService,
    ILogger<Program> logger) =>
{
    if (!httpContext.User.Identity?.IsAuthenticated ?? true)
    {
        return Results.Unauthorized();
    }

    try
    {
        var pool = await agentService.UpdatePoolAsync(id, request);
        if (pool == null) return Results.NotFound();
        
        return Results.Ok();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error updating agent pool");
        return Results.Json(new { error = "Failed to update agent pool" }, statusCode: 500);
    }
}).RequireAuthorization();

app.MapDelete("/api/agentpools/{id:guid}", async (
    Guid id,
    HttpContext httpContext,
    IAgentService agentService,
    ILogger<Program> logger) =>
{
    if (!httpContext.User.Identity?.IsAuthenticated ?? true)
    {
        return Results.Unauthorized();
    }

    try
    {
        var success = await agentService.DeletePoolAsync(id);
        return success ? Results.Ok() : Results.NotFound();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error deleting agent pool");
        return Results.Json(new { error = "Failed to delete agent pool" }, statusCode: 500);
    }
}).RequireAuthorization();

app.MapGet("/api/agents", async (
    HttpContext httpContext,
    IAgentService agentService,
    ILogger<Program> logger,
    Guid? poolId = null) =>
{
    if (!httpContext.User.Identity?.IsAuthenticated ?? true)
    {
        return Results.Unauthorized();
    }

    try
    {
        var agents = poolId.HasValue 
            ? await agentService.GetAgentsByPoolAsync(poolId.Value)
            : await agentService.GetAllAgentsAsync();
        var response = agents.Select(a => new AgentResponse
        {
            Id = a.Id,
            PoolId = a.PoolId,
            PoolName = a.Pool?.Name,
            Name = a.Name,
            Description = a.Description,
            Status = a.Status,
            MaxParallelJobs = a.MaxParallelJobs,
            CurrentRunningJobs = a.CurrentRunningJobs,
            HostName = a.HostName,
            IpAddress = a.IpAddress,
            OperatingSystem = a.OperatingSystem,
            AgentVersion = a.AgentVersion,
            MachineName = a.MachineName,
            Architecture = a.Architecture,
            CpuModel = a.CpuModel,
            LogicalCores = a.LogicalCores,
            MemoryGb = a.MemoryGb,
            DiskSpaceGb = a.DiskSpaceGb,
            CreatedAt = a.CreatedAt,
            LastHeartbeat = a.LastHeartbeat,
            Labels = a.Labels.Select(l => new AgentLabelResponse
            {
                Id = l.Id,
                Name = l.Name,
                Value = l.Value
            }).ToList()
        });
        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error fetching agents");
        return Results.Json(new { error = "Failed to fetch agents" }, statusCode: 500);
    }
}).RequireAuthorization();

app.MapPost("/api/agents", async (
    HttpContext httpContext,
    AgentRegistrationRequest request,
    IAgentService agentService,
    ILogger<Program> logger) =>
{
    if (!httpContext.User.Identity?.IsAuthenticated ?? true)
    {
        return Results.Unauthorized();
    }

    try
    {
        var userId = httpContext.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value 
            ?? httpContext.User.FindFirst("oid")?.Value 
            ?? string.Empty;
        var userEmail = httpContext.User.FindFirst("preferred_username")?.Value 
            ?? httpContext.User.FindFirst("email")?.Value 
            ?? httpContext.User.Identity?.Name 
            ?? string.Empty;

        var agent = await agentService.CreateAgentAsync(userId, userEmail, request);
        var agentWithLabels = await agentService.GetAgentByIdAsync(agent.Id);
        return Results.Ok(new AgentResponse
        {
            Id = agent.Id,
            PoolId = agent.PoolId,
            Name = agent.Name,
            Description = agent.Description,
            Status = agent.Status,
            MaxParallelJobs = agent.MaxParallelJobs,
            CurrentRunningJobs = agent.CurrentRunningJobs,
            HostName = agent.HostName,
            IpAddress = agent.IpAddress,
            OperatingSystem = agent.OperatingSystem,
            AgentVersion = agent.AgentVersion,
            MachineName = agent.MachineName,
            Architecture = agent.Architecture,
            CpuModel = agent.CpuModel,
            LogicalCores = agent.LogicalCores,
            MemoryGb = agent.MemoryGb,
            DiskSpaceGb = agent.DiskSpaceGb,
            CreatedAt = agent.CreatedAt,
            LastHeartbeat = agent.LastHeartbeat,
            ApiKey = agent.ApiKey,
            Labels = agentWithLabels?.Labels.Select(l => new AgentLabelResponse
            {
                Id = l.Id,
                Name = l.Name,
                Value = l.Value
            }).ToList() ?? new List<AgentLabelResponse>()
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error creating agent");
        return Results.Json(new { error = "Failed to create agent" }, statusCode: 500);
    }
}).RequireAuthorization();

app.MapDelete("/api/agents/{id:guid}", async (
    Guid id,
    HttpContext httpContext,
    IAgentService agentService,
    ILogger<Program> logger) =>
{
    if (!httpContext.User.Identity?.IsAuthenticated ?? true)
    {
        return Results.Unauthorized();
    }

    try
    {
        var success = await agentService.DeleteAgentAsync(id);
        return success ? Results.Ok() : Results.NotFound();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error deleting agent");
        return Results.Json(new { error = "Failed to delete agent" }, statusCode: 500);
    }
}).RequireAuthorization();

app.MapPost("/api/agents/{id:guid}/heartbeat", async (
    Guid id,
    AgentHeartbeatRequest request,
    HttpContext httpContext,
    IAgentService agentService,
    ILogger<Program> logger) =>
{
    var apiKey = httpContext.Request.Headers["X-Agent-ApiKey"].FirstOrDefault();
    if (string.IsNullOrEmpty(apiKey))
    {
        return Results.Unauthorized();
    }

    try
    {
        var agent = await agentService.GetAgentByApiKeyAsync(apiKey);
        if (agent == null || agent.Id != id)
        {
            return Results.Unauthorized();
        }

        var updated = await agentService.UpdateHeartbeatAsync(id, request);
        return updated != null ? Results.Ok() : Results.NotFound();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error updating agent heartbeat");
        return Results.Json(new { error = "Failed to update heartbeat" }, statusCode: 500);
    }
});

app.MapGet("/api/agents/{id:guid}/jobs", async (
    Guid id,
    HttpContext httpContext,
    IAgentService agentService,
    ILogger<Program> logger) =>
{
    var apiKey = httpContext.Request.Headers["X-Agent-ApiKey"].FirstOrDefault();
    if (string.IsNullOrEmpty(apiKey))
    {
        return Results.Unauthorized();
    }

    try
    {
        var agent = await agentService.GetAgentByApiKeyAsync(apiKey);
        if (agent == null || agent.Id != id)
        {
            return Results.Unauthorized();
        }

        var jobs = await agentService.GetPendingJobsForAgentAsync(id);
        var response = jobs.Select(j => new JobResponse
        {
            Id = j.Id,
            AgentId = j.AgentId,
            JobType = j.JobType,
            JobName = j.JobName,
            Description = j.Description,
            Parameters = j.Parameters,
            Status = j.Status,
            Progress = j.Progress,
            Priority = j.Priority,
            CreatedAt = j.CreatedAt
        });
        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error fetching jobs for agent");
        return Results.Json(new { error = "Failed to fetch jobs" }, statusCode: 500);
    }
});

app.MapPost("/api/agents/{id:guid}/jobs/{jobId:guid}/claim", async (
    Guid id,
    Guid jobId,
    HttpContext httpContext,
    IAgentService agentService,
    ILogger<Program> logger) =>
{
    var apiKey = httpContext.Request.Headers["X-Agent-ApiKey"].FirstOrDefault();
    if (string.IsNullOrEmpty(apiKey))
    {
        return Results.Unauthorized();
    }

    try
    {
        var agent = await agentService.GetAgentByApiKeyAsync(apiKey);
        if (agent == null || agent.Id != id)
        {
            return Results.Unauthorized();
        }

        var job = await agentService.ClaimJobAsync(id, jobId);
        if (job == null)
        {
            return Results.Json(new { error = "Job not available or already claimed" }, statusCode: 409);
        }

        return Results.Ok(new JobResponse
        {
            Id = job.Id,
            AgentId = job.AgentId,
            JobType = job.JobType,
            JobName = job.JobName,
            Description = job.Description,
            Parameters = job.Parameters,
            Status = job.Status,
            Progress = job.Progress,
            Priority = job.Priority,
            CreatedAt = job.CreatedAt,
            StartedAt = job.StartedAt
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error claiming job");
        return Results.Json(new { error = "Failed to claim job" }, statusCode: 500);
    }
});

app.MapPost("/api/agents/{id:guid}/jobs/{jobId:guid}/progress", async (
    Guid id,
    Guid jobId,
    JobProgressRequest request,
    HttpContext httpContext,
    IAgentService agentService,
    ILogger<Program> logger) =>
{
    var apiKey = httpContext.Request.Headers["X-Agent-ApiKey"].FirstOrDefault();
    if (string.IsNullOrEmpty(apiKey))
    {
        return Results.Unauthorized();
    }

    try
    {
        var agent = await agentService.GetAgentByApiKeyAsync(apiKey);
        if (agent == null || agent.Id != id)
        {
            return Results.Unauthorized();
        }

        var job = await agentService.UpdateJobProgressAsync(id, jobId, request);
        return job != null ? Results.Ok() : Results.NotFound();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error updating job progress");
        return Results.Json(new { error = "Failed to update progress" }, statusCode: 500);
    }
});

app.MapPost("/api/agents/{id:guid}/jobs/{jobId:guid}/complete", async (
    Guid id,
    Guid jobId,
    JobCompleteRequest request,
    HttpContext httpContext,
    IAgentService agentService,
    ILogger<Program> logger) =>
{
    var apiKey = httpContext.Request.Headers["X-Agent-ApiKey"].FirstOrDefault();
    if (string.IsNullOrEmpty(apiKey))
    {
        return Results.Unauthorized();
    }

    try
    {
        var agent = await agentService.GetAgentByApiKeyAsync(apiKey);
        if (agent == null || agent.Id != id)
        {
            return Results.Unauthorized();
        }

        var job = await agentService.CompleteJobAsync(id, jobId, request);
        return job != null ? Results.Ok() : Results.NotFound();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error completing job");
        return Results.Json(new { error = "Failed to complete job" }, statusCode: 500);
    }
});

app.MapGet("/api/jobs", async (
    HttpContext httpContext,
    IAgentService agentService,
    ILogger<Program> logger,
    Guid? poolId = null) =>
{
    if (!httpContext.User.Identity?.IsAuthenticated ?? true)
    {
        return Results.Unauthorized();
    }

    try
    {
        var jobs = await agentService.GetJobsAsync(null, poolId);
        var response = jobs.Select(j => new JobResponse
        {
            Id = j.Id,
            AgentId = j.AgentId,
            AgentName = j.Agent?.Name,
            PoolId = j.PoolId,
            JobType = j.JobType,
            JobName = j.JobName,
            Description = j.Description,
            Parameters = j.Parameters,
            Status = j.Status,
            Progress = j.Progress,
            Result = j.Result,
            ErrorMessage = j.ErrorMessage,
            Priority = j.Priority,
            CreatedAt = j.CreatedAt,
            StartedAt = j.StartedAt,
            CompletedAt = j.CompletedAt
        });
        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error fetching jobs");
        return Results.Json(new { error = "Failed to fetch jobs" }, statusCode: 500);
    }
}).RequireAuthorization();

app.MapPost("/api/jobs", async (
    HttpContext httpContext,
    CreateJobRequest request,
    IAgentService agentService,
    ILogger<Program> logger) =>
{
    if (!httpContext.User.Identity?.IsAuthenticated ?? true)
    {
        return Results.Unauthorized();
    }

    try
    {
        var userId = httpContext.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value 
            ?? httpContext.User.FindFirst("oid")?.Value 
            ?? string.Empty;
        var userEmail = httpContext.User.FindFirst("preferred_username")?.Value 
            ?? httpContext.User.FindFirst("email")?.Value 
            ?? httpContext.User.Identity?.Name 
            ?? string.Empty;

        var job = await agentService.CreateJobAsync(userId, userEmail, request);
        return Results.Ok(new JobResponse
        {
            Id = job.Id,
            AgentId = job.AgentId,
            JobType = job.JobType,
            JobName = job.JobName,
            Description = job.Description,
            Parameters = job.Parameters,
            Status = job.Status,
            Progress = job.Progress,
            Priority = job.Priority,
            CreatedAt = job.CreatedAt
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error creating job");
        return Results.Json(new { error = "Failed to create job" }, statusCode: 500);
    }
}).RequireAuthorization();

app.MapPost("/api/jobs/{id:guid}/cancel", async (
    Guid id,
    HttpContext httpContext,
    IAgentService agentService,
    ILogger<Program> logger) =>
{
    if (!httpContext.User.Identity?.IsAuthenticated ?? true)
    {
        return Results.Unauthorized();
    }

    try
    {
        var success = await agentService.CancelJobAsync(id);
        return success ? Results.Ok() : Results.NotFound();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error cancelling job");
        return Results.Json(new { error = "Failed to cancel job" }, statusCode: 500);
    }
}).RequireAuthorization();

app.MapControllers();
app.MapRazorPages();

app.Run();
