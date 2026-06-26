using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace BeautyStore.UnitTests;

public sealed class JwtServiceTests
{
    private static IConfiguration BuildConfig(
        string key    = "unit-test-jwt-secret-minimum-32-chars!!",
        string issuer = "https://test.beautystore.local",
        string audience = "beautystore-test",
        string expiryMinutes = "60")
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"]           = key,
                ["Jwt:Issuer"]        = issuer,
                ["Jwt:Audience"]      = audience,
                ["Jwt:ExpiryMinutes"] = expiryMinutes,
            })
            .Build();

    private static ApplicationUser MakeUser(
        string id       = "user-123",
        string email    = "alice@beautystore.test",
        string fullName = "Alice Test")
        => new()
        {
            Id       = id,
            Email    = email,
            UserName = email,
            FullName = fullName,
        };

    // ── GenerateAccessToken ───────────────────────────────────────────────────

    [Fact]
    public void GenerateAccessToken_IncludesSubClaim_EqualToUserId()
    {
        var jwt  = new JwtService(BuildConfig());
        var user = MakeUser();

        var token = jwt.GenerateAccessToken(user, ["Customer"]);

        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);
        parsed.Claims.Should().Contain(c =>
            c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id);
    }

    [Fact]
    public void GenerateAccessToken_IncludesEmailClaim()
    {
        var jwt  = new JwtService(BuildConfig());
        var user = MakeUser(email: "bob@example.com");

        var token  = jwt.GenerateAccessToken(user, []);
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

        parsed.Claims.Should().Contain(c =>
            c.Type == JwtRegisteredClaimNames.Email && c.Value == user.Email);
    }

    [Fact]
    public void GenerateAccessToken_IncludesNameClaim()
    {
        var jwt  = new JwtService(BuildConfig());
        var user = MakeUser(fullName: "Charlotte Tilbury");

        var token  = jwt.GenerateAccessToken(user, []);
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

        parsed.Claims.Should().Contain(c =>
            c.Type == JwtRegisteredClaimNames.Name && c.Value == user.FullName);
    }

    [Fact]
    public void GenerateAccessToken_IncludesJtiClaim_AsNonEmptyGuid()
    {
        var jwt  = new JwtService(BuildConfig());
        var user = MakeUser();

        var token  = jwt.GenerateAccessToken(user, []);
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

        var jti = parsed.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti);
        jti.Should().NotBeNull();
        Guid.TryParse(jti!.Value, out _).Should().BeTrue("jti must be a valid GUID");
    }

    [Fact]
    public void GenerateAccessToken_IncludesAllRoleClaims()
    {
        var jwt   = new JwtService(BuildConfig());
        var user  = MakeUser();
        var roles = new List<string> { "Admin", "Customer" };

        var token  = jwt.GenerateAccessToken(user, roles);
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

        var roleClaims = parsed.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        roleClaims.Should().BeEquivalentTo(roles);
    }

    [Fact]
    public void GenerateAccessToken_SingleRole_AppearsOnce()
    {
        var jwt  = new JwtService(BuildConfig());
        var user = MakeUser();

        var token  = jwt.GenerateAccessToken(user, ["Customer"]);
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

        parsed.Claims.Count(c => c.Type == ClaimTypes.Role).Should().Be(1);
    }

    [Fact]
    public void GenerateAccessToken_NoRoles_HasNoRoleClaims()
    {
        var jwt  = new JwtService(BuildConfig());
        var user = MakeUser();

        var token  = jwt.GenerateAccessToken(user, []);
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

        parsed.Claims.Should().NotContain(c => c.Type == ClaimTypes.Role);
    }

    [Fact]
    public void GenerateAccessToken_TokenSignature_ValidatesWithCorrectKey()
    {
        var config = BuildConfig(key: "unit-test-jwt-secret-minimum-32-chars!!");
        var jwt    = new JwtService(config);
        var user   = MakeUser();

        var token = jwt.GenerateAccessToken(user, []);

        var handler = new JwtSecurityTokenHandler();
        var result  = handler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("unit-test-jwt-secret-minimum-32-chars!!")),
            ValidateIssuer   = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew        = TimeSpan.Zero,
        }, out _);

        result.Should().NotBeNull();
    }

    [Fact]
    public void GenerateAccessToken_TokenExpiry_MatchesConfiguredMinutes()
    {
        var config = BuildConfig(expiryMinutes: "30");
        var before = DateTime.UtcNow;
        var jwt    = new JwtService(config);
        var user   = MakeUser();

        var token  = jwt.GenerateAccessToken(user, []);
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var after  = DateTime.UtcNow;

        // Token expiry should be approximately 30 minutes from now
        parsed.ValidTo.Should().BeCloseTo(before.AddMinutes(30), TimeSpan.FromSeconds(5));
        parsed.ValidTo.Should().BeBefore(after.AddMinutes(30).AddSeconds(5));
    }

    [Fact]
    public void GenerateAccessToken_TwoCalls_ProduceDifferentJti()
    {
        var jwt  = new JwtService(BuildConfig());
        var user = MakeUser();

        var token1 = jwt.GenerateAccessToken(user, []);
        var token2 = jwt.GenerateAccessToken(user, []);

        var jti1 = new JwtSecurityTokenHandler().ReadJwtToken(token1)
            .Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        var jti2 = new JwtSecurityTokenHandler().ReadJwtToken(token2)
            .Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

        jti1.Should().NotBe(jti2, "every token must have a unique jti");
    }

    // ── GenerateRefreshToken ──────────────────────────────────────────────────

    [Fact]
    public void GenerateRefreshToken_ReturnsNonEmptyString()
    {
        JwtService.GenerateRefreshToken().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GenerateRefreshToken_IsValidBase64()
    {
        var token = JwtService.GenerateRefreshToken();
        var bytes = Convert.TryFromBase64String(token, new byte[256], out _);
        bytes.Should().BeTrue("refresh token must be base64-encoded");
    }

    [Fact]
    public void GenerateRefreshToken_Encodes64Bytes()
    {
        var token = JwtService.GenerateRefreshToken();
        var bytes = Convert.FromBase64String(token);
        bytes.Should().HaveCount(64);
    }

    [Fact]
    public void GenerateRefreshToken_TwoCalls_ProduceDifferentValues()
    {
        var t1 = JwtService.GenerateRefreshToken();
        var t2 = JwtService.GenerateRefreshToken();
        t1.Should().NotBe(t2, "each refresh token must be cryptographically unique");
    }
}
