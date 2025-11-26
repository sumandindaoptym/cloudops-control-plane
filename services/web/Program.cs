using CloudOps.Web.Data;
using CloudOps.Web.Hubs;
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

builder.Services.AddSingleton<IPurgeQueue, PurgeQueue>();
builder.Services.AddHostedService<PurgeBackgroundService>();

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
        return Results.Ok(activities);
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

app.MapControllers();
app.MapRazorPages();

app.Run();
