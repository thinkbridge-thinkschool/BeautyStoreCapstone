using Azure.Identity;
using Azure.Messaging.ServiceBus;
using BeautyStore.Api.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

// ── Builder ───────────────────────────────────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);

// ── Entra ID Authentication ───────────────────────────────────────────────────
// TenantId and ClientId are NOT secrets — safe in appsettings / env vars.
// Entra ID validates tokens via its public JWKS endpoint; no symmetric key needed.

var tenantId = builder.Configuration["AzureAd:TenantId"] ?? throw new InvalidOperationException("AzureAd:TenantId is required.");
var clientId = builder.Configuration["AzureAd:ClientId"] ?? throw new InvalidOperationException("AzureAd:ClientId is required.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";
        options.Audience  = clientId;
    });

builder.Services.AddAuthorization();

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
    builder.Services.AddDbContext<BeautyStoreDbContext>(options =>
        options.UseSqlServer(connectionString, sql =>
        {
            sql.CommandTimeout(30);
            sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null);
        }));
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

// ── Application ───────────────────────────────────────────────────────────────

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// ── Health endpoint ───────────────────────────────────────────────────────────
// /health          — liveness probe  (always respond, even during startup)
// /health/ready    — readiness probe (only ready once DB is reachable)

app.MapHealthChecks("/health");
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

// ── GET /api/catalog/products ─────────────────────────────────────────────────
catalogGroup.MapGet("/products", () =>
{
    Product[] products =
    [
        new(1, "Pro Filt'r Soft Matte Foundation",    "Fenty Beauty",       "Foundation",     3800m,  4.8f, "https://picsum.photos/seed/fenty-foundation/400/500"),
        new(2, "Pillow Talk Matte Revolution Lipstick","Charlotte Tilbury",  "Lipstick",       2850m,  4.9f, "https://picsum.photos/seed/ct-pillow-talk/400/500"),
        new(3, "Orgasm Blush Powder",                 "NARS Cosmetics",     "Blush",          2200m,  4.7f, "https://picsum.photos/seed/nars-orgasm/400/500"),
        new(4, "Protini Polypeptide Moisturiser",     "Drunk Elephant",     "Moisturiser",    5600m,  4.6f, "https://picsum.photos/seed/drunk-elephant/400/500"),
        new(5, "Facial Treatment Essence",            "SK-II",              "Toner / Essence",12500m, 4.8f, "https://picsum.photos/seed/skii-essence/400/500"),
        new(6, "Rose Gold Eyeshadow Palette",         "Huda Beauty",        "Eyeshadow",      4800m,  4.7f, "https://picsum.photos/seed/huda-rose-gold/400/500"),
    ];
    return Results.Ok(products);
})
.WithName("GetProducts")
.WithSummary("Returns all featured catalog products.");

// ── Startup migrations ────────────────────────────────────────────────────────
// Applies pending EF migrations automatically on startup in Development.
// In Production, run migrations as a separate CI/CD step before deploying.

if (app.Environment.IsDevelopment() && dbAvailable)
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<BeautyStoreDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();

// ── Catalog domain types ──────────────────────────────────────────────────────
// Type declarations must follow all top-level statements (C# compiler rule).
record Product(int Id, string Name, string Brand, string Category, decimal Price, float Rating, string ImageUrl);

// ── Public partial for integration-test hosts ─────────────────────────────────
public partial class Program { }

// ── Stub DbContext ─────────────────────────────────────────────────────────────
// Temporary home until BeautyStore.Infrastructure is scaffolded (Day 26+).
// Move to: src/BeautyStore.Infrastructure/Persistence/BeautyStoreDbContext.cs
// and uncomment the ProjectReference in BeautyStore.Api.csproj.

namespace BeautyStore.Api.Data
{
    using Microsoft.EntityFrameworkCore;

    public sealed class BeautyStoreDbContext(DbContextOptions<BeautyStoreDbContext> options)
        : DbContext(options)
    {
        // Entity DbSets will be added here as Domain entities are defined.
        // Each bounded context uses a schema prefix:
        //   Catalog.*, Orders.*, Inventory.*, Payments.*, Shipping.*
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("dbo");
            base.OnModelCreating(modelBuilder);
        }
    }
}
