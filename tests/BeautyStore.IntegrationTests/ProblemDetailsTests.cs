namespace BeautyStore.IntegrationTests;

/// <summary>
/// Verifies that every error path returns RFC 7807 ProblemDetails JSON
/// with the correct Content-Type, status, and required fields.
/// </summary>
[Collection("Integration")]
public sealed class ProblemDetailsTests(BeautyStoreWebApplicationFactory factory)
    : IClassFixture<BeautyStoreWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    // ── 401 Unauthorized ─────────────────────────────────────────────────────

    [Fact]
    public async Task Unauthenticated_ProtectedRoute_Returns401ProblemDetails()
    {
        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType
            .Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Unauthenticated_OrderRoute_Returns401ProblemDetails()
    {
        var response = await _client.GetAsync("/api/orders/my");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType
            .Should().Be("application/problem+json");
    }

    // ── 404 Not Found ─────────────────────────────────────────────────────────

    [Fact]
    public async Task UnknownRoute_Returns404ProblemDetails()
    {
        var response = await _client.GetAsync("/api/does-not-exist");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType
            .Should().Be("application/problem+json");
    }

    [Fact]
    public async Task NonExistentProduct_Returns404ProblemDetails_WithTypeAndTitle()
    {
        var response = await _client.GetAsync("/api/catalog/products/88888");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        doc.RootElement.TryGetProperty("type",   out var typeEl).Should().BeTrue();
        doc.RootElement.TryGetProperty("title",  out var titleEl).Should().BeTrue();
        doc.RootElement.TryGetProperty("status", out var statusEl).Should().BeTrue();

        typeEl.GetString().Should().Contain("404");
        statusEl.GetInt32().Should().Be(404);
    }

    // ── 400 Validation ────────────────────────────────────────────────────────

    [Fact]
    public async Task PlaceOrder_QuantityZero_Returns400ProblemDetails()
    {
        var token = await factory.RegisterAsync(_client);
        using var client = factory.CreateAuthenticatedClient(token);

        var response = await client.PostAsJsonAsync("/api/orders",
            new { ProductId = 1, Quantity = 0 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType
            .Should().Be("application/problem+json");

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("status", out var statusEl).Should().BeTrue();
        statusEl.GetInt32().Should().Be(400);
    }

    [Fact]
    public async Task Register_WeakPassword_Returns400ProblemDetailsOrArray()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            FullName = "Weak",
            Email    = $"weak-{Guid.NewGuid():N}@test.com",
            Password = "x",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        // AuthEndpoints returns Results.BadRequest(errors) — body should not be empty
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotBeNullOrWhiteSpace();
    }

    // ── Security headers ──────────────────────────────────────────────────────

    [Fact]
    public async Task AllResponses_IncludeXContentTypeOptionsHeader()
    {
        var response = await _client.GetAsync("/api/catalog/products");

        response.Headers.TryGetValues("X-Content-Type-Options", out var values)
            .Should().BeTrue();
        values!.Should().ContainSingle().Which.Should().Be("nosniff");
    }

    [Fact]
    public async Task AllResponses_IncludeCacheControlHeader()
    {
        var response = await _client.GetAsync("/api/catalog/products");

        response.Headers.TryGetValues("Cache-Control", out var values)
            .Should().BeTrue();
        values!.First().Should().Contain("no-store");
    }

    // ── Root endpoint ─────────────────────────────────────────────────────────

    [Fact]
    public async Task RootEndpoint_Returns200WithApiInfo()
    {
        var response = await _client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("name").GetString().Should().Be("BeautyStore API");
        doc.RootElement.GetProperty("status").GetString().Should().Be("Running");
    }

    // ── Health endpoints ──────────────────────────────────────────────────────

    [Fact]
    public async Task HealthLiveness_Returns200()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
