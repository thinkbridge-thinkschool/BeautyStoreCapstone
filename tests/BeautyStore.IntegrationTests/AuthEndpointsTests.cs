namespace BeautyStore.IntegrationTests;

[Collection("Integration")]
public sealed class AuthEndpointsTests(BeautyStoreWebApplicationFactory factory)
    : IClassFixture<BeautyStoreWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    // ── POST /api/auth/register ───────────────────────────────────────────────

    [Fact]
    public async Task Register_ValidRequest_Returns200WithTokens()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            FullName = "Priya Sharma",
            Email    = $"priya-{Guid.NewGuid():N}@test.com",
            Password = "TestPass1!",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("accessToken").GetString()
            .Should().NotBeNullOrWhiteSpace();
        doc.RootElement.GetProperty("refreshToken").GetString()
            .Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Register_ValidRequest_AssignsCustomerRole()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            FullName = "Role Tester",
            Email    = $"role-{Guid.NewGuid():N}@test.com",
            Password = "TestPass1!",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var roles = doc.RootElement.GetProperty("roles").EnumerateArray()
            .Select(r => r.GetString())
            .ToList();

        roles.Should().Contain("Customer");
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns400()
    {
        var email = $"dup-{Guid.NewGuid():N}@test.com";
        var payload = new { FullName = "Alice", Email = email, Password = "TestPass1!" };

        // First registration succeeds
        var first = await _client.PostAsJsonAsync("/api/auth/register", payload);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        // Second registration with same email fails
        var second = await _client.PostAsJsonAsync("/api/auth/register", payload);
        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WeakPassword_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            FullName = "Weak Pass User",
            Email    = $"weak-{Guid.NewGuid():N}@test.com",
            Password = "abc",   // too short, no digit
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_MissingEmail_Returns400()
    {
        // Send an object without the Email field
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            FullName = "Missing Email",
            Password = "TestPass1!",
            // Email intentionally omitted → deserialized as null
        });

        // ASP.NET Core model binding / Identity will reject null email
        ((int)response.StatusCode).Should().BeOneOf(400, 422);
    }

    // ── POST /api/auth/login ──────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithAccessToken()
    {
        var email    = $"login-{Guid.NewGuid():N}@test.com";
        const string password = "TestPass1!";

        await factory.RegisterAsync(_client, email, password: password);

        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { Email = email, Password = password });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("accessToken").GetString()
            .Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var email = $"wrongpw-{Guid.NewGuid():N}@test.com";
        await factory.RegisterAsync(_client, email);

        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { Email = email, Password = "WrongPassword99!" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_NonExistentEmail_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { Email = "ghost@nowhere.com", Password = "TestPass1!" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── GET /api/auth/me ──────────────────────────────────────────────────────

    [Fact]
    public async Task Me_WithValidJwt_Returns200WithProfile()
    {
        var email = $"me-{Guid.NewGuid():N}@test.com";
        var token = await factory.RegisterAsync(_client, email, "Me Tester");

        using var authed = factory.CreateAuthenticatedClient(token);
        var response = await authed.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("email").GetString().Should().Be(email);
        doc.RootElement.GetProperty("fullName").GetString().Should().Be("Me Tester");
    }

    [Fact]
    public async Task Me_WithoutJwt_Returns401()
    {
        var response = await _client.GetAsync("/api/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithInvalidJwt_Returns401()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "not.a.valid.jwt");

        var response = await client.GetAsync("/api/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_ReturnsCorrectRoles()
    {
        var email = $"roles-{Guid.NewGuid():N}@test.com";
        var token = await factory.RegisterAsync(_client, email);

        using var authed  = factory.CreateAuthenticatedClient(token);
        var response = await authed.GetAsync("/api/auth/me");
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var roles = doc.RootElement.GetProperty("roles").EnumerateArray()
            .Select(r => r.GetString()).ToList();

        roles.Should().Contain("Customer");
    }
}
