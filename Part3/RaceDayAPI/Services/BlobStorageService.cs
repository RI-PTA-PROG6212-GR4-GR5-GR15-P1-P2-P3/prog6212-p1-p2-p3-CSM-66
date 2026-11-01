using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDayAPI.Services;

// the only place in the whole solution that talks to Azure
public interface IBlobStorageService
{
    bool IsConfigured { get; }
    Task<string> UploadImageAsync(Stream content, string blobName, string contentType);
    Task DeleteIfExistsAsync(string? blobUrl);
}

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient? _container;

    public BlobStorageService(IConfiguration config)
    {
        // the connection string is a secret, it comes from user secrets or an environment variable, never from the repo
        var connectionString = config["AzureBlob:ConnectionString"];
        var containerName = config["AzureBlob:ContainerName"] ?? "raceday-images";

        if (!string.IsNullOrWhiteSpace(connectionString))
            _container = new BlobContainerClient(connectionString, containerName);
    }

    public bool IsConfigured => _container != null;

    public async Task<string> UploadImageAsync(Stream content, string blobName, string contentType)
    {
        if (_container == null)
            throw new InvalidOperationException("Blob storage is not configured.");

        var blob = _container.GetBlobClient(blobName);
        await blob.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        });

        return blob.Uri.ToString();
    }

    public async Task DeleteIfExistsAsync(string? blobUrl)
    {
        if (_container == null || string.IsNullOrWhiteSpace(blobUrl))
            return;

        // only ever delete blobs that live in our own container
        var prefix = _container.Uri.ToString().TrimEnd('/') + "/";
        if (!blobUrl.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return;

        var blobName = Uri.UnescapeDataString(blobUrl[prefix.Length..]);

        try
        {
            await _container.DeleteBlobIfExistsAsync(blobName);
        }
        catch (RequestFailedException)
        {
            // cleaning up the old image is nice to have, it shouldn't break the upload
        }
    }
}