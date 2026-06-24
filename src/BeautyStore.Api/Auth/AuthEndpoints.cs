using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BeautyStore.Api.Auth.Dtos;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BeautyStore.Api.Auth;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        // ── POST /api/auth/register ───────────────────────────────────────────
        group.MapPost("/register", async (
            RegisterRequest                          req,
            [FromServices] UserManager<ApplicationUser> userManager,
            [FromServices] JwtService                   jwt) =>
        {
            var user = new ApplicationUser
            {
                UserName = req.Email,
                Email    = req.Email,
                FullName = req.FullName,
            };

            var result = await userManager.CreateAsync(user, req.Password);
            if (!result.Succeeded)
                return Results.BadRequest(result.Errors.Select(e => e.Description));

            await userManager.AddToRoleAsync(user, "Customer");

            var roles   = await userManager.GetRolesAsync(user);
            var access  = jwt.GenerateAccessToken(user, roles);
            var refresh = JwtService.GenerateRefreshToken();

            await userManager.SetAuthenticationTokenAsync(
                user, "BeautyStore", "RefreshToken", refresh);

            return Results.Ok(new AuthResponse(access, refresh, user.Email!, user.FullName, roles));
        })
        .WithName("Register")
        .WithSummary("Register a new customer account and receive JWT + refresh token.")
        .AllowAnonymous();

        // ── POST /api/auth/login ──────────────────────────────────────────────
        group.MapPost("/login", async (
            LoginRequest                                    req,
            [FromServices] UserManager<ApplicationUser>     userManager,
            [FromServices] SignInManager<ApplicationUser>   signInManager,
            [FromServices] JwtService                       jwt) =>
        {
            var user = await userManager.FindByEmailAsync(req.Email);
            if (user is null) return Results.Unauthorized();

            // CheckPasswordSignInAsync validates the hash and respects lockout.
            var result = await signInManager.CheckPasswordSignInAsync(
                user, req.Password, lockoutOnFailure: false);

            if (!result.Succeeded) return Results.Unauthorized();

            var roles   = await userManager.GetRolesAsync(user);
            var access  = jwt.GenerateAccessToken(user, roles);
            var refresh = JwtService.GenerateRefreshToken();

            await userManager.SetAuthenticationTokenAsync(
                user, "BeautyStore", "RefreshToken", refresh);

            return Results.Ok(new AuthResponse(access, refresh, user.Email!, user.FullName, roles));
        })
        .WithName("Login")
        .WithSummary("Sign in with email and password.")
        .AllowAnonymous();

        // ── GET /api/auth/me ──────────────────────────────────────────────────
        // MapInboundClaims = false → claim names stay as raw JWT names (sub, email, …).
        group.MapGet("/me", async (
            ClaimsPrincipal                             principal,
            [FromServices] UserManager<ApplicationUser> userManager) =>
        {
            var userId = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (userId is null) return Results.Unauthorized();

            var user = await userManager.FindByIdAsync(userId);
            if (user is null) return Results.Unauthorized();

            var roles = await userManager.GetRolesAsync(user);

            return Results.Ok(new
            {
                user.Email,
                user.FullName,
                Roles = roles,
            });
        })
        .WithName("Me")
        .WithSummary("Returns the authenticated user's profile.")
        .RequireAuthorization();
    }
}
