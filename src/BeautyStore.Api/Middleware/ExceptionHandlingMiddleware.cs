using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using BeautyStore.Api.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace BeautyStore.Api.Middleware;

/// <summary>
/// Outermost middleware — catches every unhandled exception in the pipeline,
/// logs structured context, and returns RFC 7807 ProblemDetails JSON.
/// Never exposes stack traces or internal detail in Production.
/// </summary>
public sealed class ExceptionHandlingMiddleware(
    RequestDelegate   next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IHostEnvironment  env)
{
    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception ex)
    {
        // If the response body has already started streaming we cannot change headers/status.
        if (context.Response.HasStarted)
        {
            logger.LogWarning(
                "Response already started — cannot rewrite error for TraceId {TraceId}",
                context.TraceIdentifier);
            return;
        }

        // ── Resolve status code and title ─────────────────────────────────────
        var (status, title, level) = ex switch
        {
            NotFoundException      => (404, "Resource not found",               LogLevel.Warning),
            ValidationException    => (400, "Validation failed",                LogLevel.Warning),
            ConflictException      => (409, "Conflict",                         LogLevel.Warning),
            ForbiddenException     => (403, "Forbidden",                        LogLevel.Warning),
            BusinessRuleException  => (422, "Business rule violation",          LogLevel.Warning),
            DbUpdateException      => (503, "Database temporarily unavailable", LogLevel.Error),
            TimeoutException       => (503, "Service temporarily unavailable",  LogLevel.Error),
            OperationCanceledException => (499, "Request cancelled",            LogLevel.Information),
            _                      => (500, "Unexpected server error",          LogLevel.Error),
        };

        // ── Structured log — captured by Application Insights automatically ──
        var traceId = context.TraceIdentifier;
        var userId  = context.User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? "anonymous";

        logger.Log(
            level, ex,
            "Exception {ExceptionType} | {Method} {Path} | Status {Status} | User {UserId} | TraceId {TraceId}",
            ex.GetType().Name,
            context.Request.Method,
            context.Request.Path,
            status,
            userId,
            traceId);

        // ── Build ProblemDetails payload ──────────────────────────────────────
        var problem = new Dictionary<string, object?>
        {
            ["type"]    = $"https://httpstatuses.io/{status}",
            ["title"]   = title,
            ["status"]  = status,
            ["traceId"] = traceId,
        };

        // Field-level errors for ValidationException
        if (ex is ValidationException ve && ve.Errors is { Count: > 0 })
            problem["errors"] = ve.Errors;

        // Detail visible only outside Production — safe for local/dev debugging
        if (!env.IsProduction())
            problem["detail"] = ex.Message;

        // ── Write response ────────────────────────────────────────────────────
        context.Response.StatusCode  = status;
        context.Response.ContentType = "application/problem+json";

        // Ensure minimal security headers are present even on error responses.
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["Cache-Control"]          = "no-store";

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(problem, JsonOpts));
    }
}
