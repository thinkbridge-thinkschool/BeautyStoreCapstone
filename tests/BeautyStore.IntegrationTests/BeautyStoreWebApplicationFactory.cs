using BeautyStore.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BeautyStore.IntegrationTests;

/// <summary>
/// Shared WebApplicationFactory for integration tests.
/// Replaces SQL Server with an isolated SQLite database (per factory instance)
/// and removes background services that are irrelevant in tests.
/// </summary>
public sealed class BeautyStoreWebApplicationFactory
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Unique file per factory instance — each test class gets its own fresh DB.
    private readonly string _dbPath = $"bstest-{Guid.NewGuid():N}.db";

    // WebApplication.CreateBuilder loads OS environment variables BEFORE the
    // factory's ConfigureAppConfiguration callbacks run, so Program.cs reads
    // Jwt:Key (line 27) and ConnectionStrings:BeautyStoreDb (line 73) from
    // actual env vars. The static constructor runs before any factory instance
    // is created, guaranteeing the vars are present when Program.cs executes.
    static BeautyStoreWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("Jwt__Key",
            "integration-test-jwt-secret-key-minimum-32-chars!!");
        // Placeholder value — just needs to be non-empty so dbAvailable = true
        // in Program.cs. ConfigureServices below replaces the actual DbContext
        // registration with the per-instance SQLite path (_dbPath).
        Environment.SetEnvironmentVariable("ConnectionStrings__BeautyStoreDb",
            "Data Source=test-placeholder.db");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // ── Override configuration ────────────────────────────────────────────
        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Required — matches the null-check in Program.cs
                ["Jwt:Key"]           = "integration-test-jwt-secret-key-minimum-32-chars!!",
                ["Jwt:Issuer"]        = "https://test.beautystore.local",
                ["Jwt:Audience"]      = "beautystore-integration-tests",
                ["Jwt:ExpiryMinutes"] = "60",

                // Non-empty string → dbAvailable = true in Program.cs.
                // The SQL Server registration is then overridden below with SQLite.
                ["ConnectionStrings:BeautyStoreDb"] = $"Data Source={_dbPath}",

                // No Service Bus or Blob Storage in tests — skipped by Program.cs conditionals.
                ["AllowedOrigins"] = "http://localhost:4200",
            });
        });

        // ── Override services ─────────────────────────────────────────────────
        builder.ConfigureServices(services =>
        {
            // Remove the SQL Server DbContext registered in Program.cs.
            var sqlDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<BeautyStoreDbContext>));
            if (sqlDescriptor is not null) services.Remove(sqlDescriptor);

            // Register SQLite — EnsureCreatedAsync is called in Program.cs startup
            // when the provider name contains "Sqlite" (see the patched migration block).
            services.AddDbContext<BeautyStoreDbContext>(options =>
                options.UseSqlite($"Data Source={_dbPath}"));

            // Remove all hosted background services (OutboxRelayWorker etc.)
            // to prevent DB polling and noisy logs during tests.
            foreach (var hostedSvc in services
                .Where(d => d.ServiceType == typeof(IHostedService))
                .ToList())
            {
                services.Remove(hostedSvc);
            }
        });

        // "Test" environment: not Production (detail exposed), not Development (no scalar/openapi).
        builder.UseEnvironment("Test");
    }

    // ── Helpers for test code ─────────────────────────────────────────────────

    /// <summary>
    /// Registers a new user and returns the access token.
    /// Every call uses a unique email so tests don't collide.
    /// </summary>
    public async Task<string> RegisterAsync(
        HttpClient client,
        string?    email    = null,
        string?    fullName = null,
        string?    password = null)
    {
        var uniqueEmail = email ?? $"user-{Guid.NewGuid():N}@beautystore.test";
        var payload = new
        {
            FullName = fullName ?? "Test User",
            Email    = uniqueEmail,
            Password = password ?? "TestPass1!",
        };
        var response = await client.PostAsJsonAsync("/api/auth/register", payload);
        response.EnsureSuccessStatusCode();

        var json  = await response.Content.ReadAsStringAsync();
        using var doc   = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("accessToken").GetString()!;
    }

    /// <summary>
    /// Logs in with the supplied credentials and returns the access token.
    /// </summary>
    public async Task<string> LoginAsync(
        HttpClient client,
        string     email,
        string     password = "TestPass1!")
    {
        var payload  = new { Email = email, Password = password };
        var response = await client.PostAsJsonAsync("/api/auth/login", payload);
        response.EnsureSuccessStatusCode();

        var json  = await response.Content.ReadAsStringAsync();
        using var doc   = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("accessToken").GetString()!;
    }

    /// <summary>
    /// Creates a client with the Authorization header preset for the given token.
    /// </summary>
    public HttpClient CreateAuthenticatedClient(string accessToken)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        // Release pooled SQLite connections so the file handle is freed
        // before we attempt to delete it (otherwise IOException on Windows).
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }
}
