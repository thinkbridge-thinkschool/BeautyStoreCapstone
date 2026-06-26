using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Azure.Storage.Blobs;
using BeautyStore.Api.Auth;
using BeautyStore.Api.Admin;
using BeautyStore.Api.Catalog;
using BeautyStore.Api.Storage;
using BeautyStore.Api.Data;
using BeautyStore.Api.Middleware;
using BeautyStore.Api.Orders;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;
using System.Threading.RateLimiting;

// ── Builder ───────────────────────────────────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);

// ── JWT key (required — set Jwt__Key env var in Container App) ────────────────
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is required. Set it in appsettings.Development.json locally or as Jwt__Key env var in the Container App.");

// ── JWT Bearer authentication ─────────────────────────────────────────────────
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Keep JWT claim names as-is (sub, email, …) — no silent .NET type-map.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer           = !string.IsNullOrWhiteSpace(builder.Configuration["Jwt:Issuer"]),
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidateAudience         = !string.IsNullOrWhiteSpace(builder.Configuration["Jwt:Audience"]),
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            ValidateLifetime         = true,
            ClockSkew                = TimeSpan.Zero,
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSingleton<JwtService>();

// ── CORS ──────────────────────────────────────────────────────────────────────
// AllowedOrigins is a comma-separated list set in Container App env vars.
// dev.bicepparam  → "http://localhost:4200"
// prod.bicepparam → "https://beautystore.azurestaticapps.net"

var allowedOrigins = (builder.Configuration["AllowedOrigins"] ?? "http://localhost:4200")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()));

// ── Database (EF Core + SQL Server — Managed Identity) ───────────────────────
// Connection string uses "Authentication=Active Directory Managed Identity".
// No User ID or Password — the Container App System-Assigned MSI authenticates.
// Grant db_datareader + db_datawriter to the MSI via T-SQL after first deploy.
// Omit locally to skip DB registration — catalog endpoint is hardcoded.

var connectionString = builder.Configuration.GetConnectionString("BeautyStoreDb");
var dbAvailable      = !string.IsNullOrWhiteSpace(connectionString);

if (dbAvailable)
{
    var isSqlite = connectionString!.TrimStart().StartsWith("Data Source", StringComparison.OrdinalIgnoreCase);
    builder.Services.AddDbContext<BeautyStoreDbContext>(options =>
    {
        if (isSqlite)
            options.UseSqlite(connectionString);
        else
            options.UseSqlServer(connectionString, sql =>
            {
                sql.CommandTimeout(30);
                sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null);
            });
    });

    // ── ASP.NET Core Identity ─────────────────────────────────────────────────
    // AddIdentityCore omits cookie auth — we issue our own JWTs, so no cookie scheme.
    builder.Services
        .AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequireDigit           = true;
            options.Password.RequiredLength         = 8;
            options.Password.RequireNonAlphanumeric = false;
            options.User.RequireUniqueEmail         = true;
        })
        .AddRoles<ApplicationRole>()
        .AddEntityFrameworkStores<BeautyStoreDbContext>()
        .AddDefaultTokenProviders()
        .AddSignInManager();
}

// ── Azure Service Bus (Managed Identity) ─────────────────────────────────────
// ServiceBus:Namespace is the FQDN: "beautystore-dev-sb-xxxxx.servicebus.windows.net"
// DefaultAzureCredential uses the Container App System-Assigned MSI in Azure,
// and developer credentials (Azure CLI / VS Code) locally.

var sbNamespace = builder.Configuration["ServiceBus:Namespace"];
if (!string.IsNullOrWhiteSpace(sbNamespace))
{
    builder.Services.AddSingleton(_ => new ServiceBusClient(sbNamespace, new DefaultAzureCredential()));
}

// ── Azure Blob Storage (Managed Identity) ────────────────────────────────────
// Storage:AccountName is the storage account name (e.g. "beautystoredev5npzse").
// DefaultAzureCredential uses the Container App MSI in Azure.
// Omit locally to skip blob registration — upload endpoint returns 503.

var storageAccount = builder.Configuration["Storage:AccountName"];
if (!string.IsNullOrWhiteSpace(storageAccount))
{
    var blobUri = new Uri($"https://{storageAccount}.blob.core.windows.net");
    builder.Services.AddSingleton(_ => new BlobServiceClient(blobUri, new DefaultAzureCredential()));
    builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();
}

// ── OpenTelemetry + Azure Monitor ────────────────────────────────────────────
// UseAzureMonitor() reads APPLICATIONINSIGHTS_CONNECTION_STRING automatically.
// Skipped locally when the connection string is absent — throws otherwise.

var aiConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
if (!string.IsNullOrWhiteSpace(aiConnectionString))
{
    builder.Services
        .AddOpenTelemetry()
        .UseAzureMonitor()
        .WithTracing(tracing => tracing
            .AddSource("Microsoft.EntityFrameworkCore"));
}

builder.Services.AddHostedService<OutboxRelayWorker>();

// ── Request hardening ─────────────────────────────────────────────────────────
// 1 MB body cap prevents request-body bombs on any endpoint.
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 1_048_576);

// Fixed-window: 100 req / IP / minute. Container Apps terminates TLS and
// forwards the real client IP in X-Forwarded-For, but Kestrel sees the
// YARP proxy IP for the partition key — good enough for blast protection.
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                Window      = TimeSpan.FromMinutes(1),
                PermitLimit = 100,
                QueueLimit  = 0,
            }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// ── Health Checks ─────────────────────────────────────────────────────────────
// Probes in api.bicep call GET /health.
//   Liveness  — restarts the container if it stops responding.
//   Readiness — holds traffic until startup + DB connectivity are confirmed.

var healthChecks = builder.Services.AddHealthChecks();
if (dbAvailable) 
{
    healthChecks.AddDbContextCheck<BeautyStoreDbContext>("sql", tags: ["ready"]);
}

// ── OpenAPI / Scalar ──────────────────────────────────────────────────────────
// Scalar replaces Swagger UI in .NET 9/10.
// Available at /scalar/v1 in Development.

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title   = "BeautyStore API";
        document.Info.Version = "v1";
        document.Info.Description =
            "Nykaa-style beauty & skincare marketplace — Catalog, Orders, Inventory, Payments, Shipping.";
        return Task.CompletedTask;
    });
});

// ── ProblemDetails ────────────────────────────────────────────────────────────
// Registers IProblemDetailsService used by Results.Problem() and the exception
// middleware. Required for consistent RFC 7807 error shapes across the API.
builder.Services.AddProblemDetails();

// ── Application ───────────────────────────────────────────────────────────────

var app = builder.Build();

// ── Exception handling — must be FIRST so it wraps every downstream middleware ─
app.UseMiddleware<ExceptionHandlingMiddleware>();

// ── Status-code pages — converts empty 401/403/404/405/429 bodies to JSON ────
// Fires only when the response body is empty AND ContentType is not already set.
// This covers: JWT 401 challenges, auth 403, routing 404, rate-limit 429.
app.UseStatusCodePages(async ctx =>
{
    var res = ctx.HttpContext.Response;
    if (res.ContentType?.Contains("application/problem") == true) return;

    var traceId = ctx.HttpContext.TraceIdentifier;
    var (title, type) = res.StatusCode switch
    {
        401 => ("Unauthorized — valid credentials required", "https://httpstatuses.io/401"),
        403 => ("Forbidden — you do not have permission",    "https://httpstatuses.io/403"),
        404 => ("The requested resource was not found",      "https://httpstatuses.io/404"),
        405 => ("HTTP method not allowed",                   "https://httpstatuses.io/405"),
        429 => ("Too many requests — rate limit exceeded",   "https://httpstatuses.io/429"),
        _   => ("An error occurred",                         $"https://httpstatuses.io/{res.StatusCode}"),
    };

    await res.WriteAsJsonAsync(
        new { type, title, status = res.StatusCode, traceId },
        options: (System.Text.Json.JsonSerializerOptions?)null,
        contentType: "application/problem+json");
});

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";

    context.Response.Headers["Strict-Transport-Security"] =
        "max-age=31536000; includeSubDomains";

    context.Response.Headers["Cross-Origin-Resource-Policy"] =
        "same-origin";

    context.Response.Headers["Cache-Control"] =
        "no-store, no-cache, must-revalidate";

    await next();
});

app.UseHttpsRedirection();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers["Cross-Origin-Resource-Policy"] = "cross-origin";
        ctx.Context.Response.Headers["Cache-Control"]                = "public, max-age=86400";
    }
});
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// ── Health endpoint ───────────────────────────────────────────────────────────
// /health          — liveness probe  (always respond, even during startup)
// /health/ready    — readiness probe (only ready once DB is reachable)

// /health      — liveness (no DB check — must not hang)
// /health/ready — readiness (includes SQL; only "ready"-tagged checks)
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = check => !check.Tags.Contains("ready"),
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
});

app.MapGet("/", () => Results.Ok(new
{
    Name    = "BeautyStore API",
    Version = "v1",
    Status  = "Running",
    Health  = "/health"
}));

// ── OpenAPI ───────────────────────────────────────────────────────────────────

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "BeautyStore API";
        options.Theme = ScalarTheme.Purple;
    });
}

// ── Domain route groups ───────────────────────────────────────────────────────

// Catalog is intentionally public — product browsing requires no login.
var catalogGroup   = app.MapGroup("/api/catalog").WithTags("Catalog");
var ordersGroup    = app.MapGroup("/api/orders").WithTags("Orders")      .RequireAuthorization();
var inventoryGroup = app.MapGroup("/api/inventory").WithTags("Inventory").RequireAuthorization();
var paymentsGroup  = app.MapGroup("/api/payments").WithTags("Payments")  .RequireAuthorization();
var shippingGroup  = app.MapGroup("/api/shipping").WithTags("Shipping")  .RequireAuthorization();

catalogGroup.MapCatalogEndpoints();

var adminGroup = app.MapGroup("/api/admin").WithTags("Admin")
    .RequireAuthorization(policy => policy.RequireRole("Admin"));
adminGroup.MapAdminEndpoints();
adminGroup.MapStorageEndpoints();

// ── Startup migrations ────────────────────────────────────────────────────────
// MigrateAsync is idempotent — safe to run on every startup in all environments.
// For zero-downtime deploys in the future, move this to a pre-deploy job.
// SQLite (used in integration-test environments) does not support the SQL Server
// migration SQL; EnsureCreatedAsync creates the schema directly from the model.

if (dbAvailable)
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<BeautyStoreDbContext>();
    if (db.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
        await db.Database.EnsureCreatedAsync();
    else
        await db.Database.MigrateAsync();
}

// ── Seed Identity roles ───────────────────────────────────────────────────────
// Runs on every startup; RoleExistsAsync makes it idempotent.
if (dbAvailable)
{
    await using var seedScope   = app.Services.CreateAsyncScope();
    var roleManager = seedScope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
    foreach (var role in new[] { "Admin", "Customer" })
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new ApplicationRole(role));
    }
}

// ── Seed catalog (categories + products) ─────────────────────────────────────
// Idempotent: skips if Categories table already has rows.
if (dbAvailable)
{
    await using var catalogSeedScope = app.Services.CreateAsyncScope();
    var catalogDb = catalogSeedScope.ServiceProvider.GetRequiredService<BeautyStoreDbContext>();
    await ProductSeeder.SeedAsync(catalogDb);
}

app.MapAuthEndpoints();
ordersGroup.MapOrderEndpoints();

// ── Fallback — unknown routes return JSON 404, not HTML ───────────────────────
app.MapFallback(() => Results.Problem(
    title:      "The requested resource was not found",
    statusCode: StatusCodes.Status404NotFound,
    type:       "https://httpstatuses.io/404"));

app.Run();

// ── Public partial for integration-test hosts ─────────────────────────────────
public partial class Program { }

// ── OutboxRelayWorker ─────────────────────────────────────────────────────────
// Background service that polls the DB every 30 s, creating an EF Core SQL span
// each tick so the Application Map shows an API → SQL dependency edge.

sealed class OutboxRelayWorker(IServiceScopeFactory scopeFactory, ILogger<OutboxRelayWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("OutboxRelayWorker started");
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
                await PollAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown — not an error.
        }
    }

    private async Task PollAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetService<BeautyStoreDbContext>();
            if (db is null)
            {
                logger.LogDebug("Outbox relay skipped — database not configured");
                return;
            }
            await db.Database.ExecuteSqlRawAsync("SELECT 1", ct);
            logger.LogInformation("Outbox relay tick complete. DB reachable: {DbReachable}", true);
        }
        catch (OperationCanceledException)
        {
            throw; // let the timer loop handle shutdown
        }
        catch (Exception ex)
        {
            // Log and continue — a transient SQL failure must not kill the host.
            logger.LogWarning(ex,
                "Outbox relay tick failed. Will retry in 30 s. Reason: {Message}", ex.Message);
        }
    }
}

