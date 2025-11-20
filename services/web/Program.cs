using CloudOps.Web.Services;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
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
            }
        };
    })
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
});

builder.Services.AddControllersWithViews();

builder.Services.AddRazorPages()
    .AddMicrosoftIdentityUI();

builder.Services.AddHttpClient<IAzureSubscriptionService, AzureSubscriptionService>();

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

app.UseForwardedHeaders();

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

app.MapControllers();
app.MapRazorPages();

app.Run();
