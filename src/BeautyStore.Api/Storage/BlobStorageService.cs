using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace BeautyStore.Api.Storage;

public sealed class BlobStorageService(BlobServiceClient client) : IBlobStorageService
{
    private const string ContainerName = "product-images";

    public async Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        var container = client.GetBlobContainerClient(ContainerName);
        await container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: ct);

        var blobName = $"{Guid.NewGuid():N}{Path.GetExtension(fileName)}";
        var blob = container.GetBlobClient(blobName);

        await blob.UploadAsync(content, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: ct);

        return blob.Uri.ToString();
    }

    public async Task DeleteAsync(string blobUrl, CancellationToken ct = default)
    {
        if (!Uri.TryCreate(blobUrl, UriKind.Absolute, out var uri)) return;

        var container = client.GetBlobContainerClient(ContainerName);
        var blobName = Path.GetFileName(uri.LocalPath);
        await container.GetBlobClient(blobName).DeleteIfExistsAsync(cancellationToken: ct);
    }
}
