using BeautyStore.Api.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace BeautyStore.Api.Storage;

public static class StorageEndpoints
{
    private static readonly string[] AllowedTypes = ["image/jpeg", "image/png", "image/webp"];
    private const long MaxBytes = 5 * 1024 * 1024; // 5 MB

    public static void MapStorageEndpoints(this RouteGroupBuilder group)
    {
        // ── POST /api/admin/images/upload ─────────────────────────────────────
        group.MapPost("/images/upload", async (
            HttpRequest request,
            [FromServices] IBlobStorageService storage,
            CancellationToken ct) =>
        {
            if (!request.HasFormContentType || request.Form.Files.Count == 0)
                throw new ValidationException("Request must be multipart/form-data with a file.",
                    new Dictionary<string, string[]> { ["file"] = ["A file is required."] });

            var file = request.Form.Files[0];

            if (file.Length == 0)
                throw new ValidationException("File is empty.",
                    new Dictionary<string, string[]> { ["file"] = ["File cannot be empty."] });

            if (file.Length > MaxBytes)
                throw new ValidationException("File too large.",
                    new Dictionary<string, string[]> { ["file"] = ["Maximum size is 5 MB."] });

            if (!AllowedTypes.Contains(file.ContentType.ToLowerInvariant()))
                throw new ValidationException("Unsupported file type.",
                    new Dictionary<string, string[]> { ["file"] = ["Only JPEG, PNG, and WebP are allowed."] });

            await using var stream = file.OpenReadStream();
            var url = await storage.UploadAsync(stream, file.FileName, file.ContentType, ct);

            return Results.Ok(new { url });
        })
        .WithName("UploadProductImage")
        .WithSummary("Uploads a product image (JPEG/PNG/WebP, max 5 MB). Returns the public blob URL.")
        .DisableAntiforgery();
    }
}
