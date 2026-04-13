using AuditorPRO.Domain.Interfaces;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AuditorPRO.Infrastructure.Services;

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<BlobStorageService> _logger;

    private readonly bool _configured;

    public BlobStorageService(IConfiguration config, ILogger<BlobStorageService> logger)
    {
        _logger = logger;
        var connectionString = config["AzureStorage:ConnectionString"] ?? "";

        _configured = !string.IsNullOrWhiteSpace(connectionString)
            && connectionString.StartsWith("DefaultEndpointsProtocol", StringComparison.OrdinalIgnoreCase);

        if (_configured)
            _blobServiceClient = new BlobServiceClient(connectionString);
        else
        {
            _blobServiceClient = null!;
            _logger.LogWarning("AzureStorage no está configurado. Las operaciones de Blob retornarán errores controlados.");
        }
    }

    private void EnsureConfigured()
    {
        if (!_configured)
            throw new InvalidOperationException("Azure Storage no está configurado en este entorno.");
    }

    public async Task<string> UploadAsync(Stream content, string fileName, string contentType, string container, CancellationToken ct = default)
    {
        EnsureConfigured();
        var containerClient = _blobServiceClient.GetBlobContainerClient(container);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);

        var blobName = $"{Guid.NewGuid()}/{fileName}";
        var blobClient = containerClient.GetBlobClient(blobName);

        await blobClient.UploadAsync(content, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: ct);
        return blobClient.Uri.ToString();
    }

    public async Task<Stream> DownloadAsync(string blobUrl, CancellationToken ct = default)
    {
        EnsureConfigured();
        var blobClient = new BlobClient(new Uri(blobUrl));
        var response = await blobClient.DownloadStreamingAsync(cancellationToken: ct);
        return response.Value.Content;
    }

    public async Task DeleteAsync(string blobUrl, CancellationToken ct = default)
    {
        EnsureConfigured();
        var blobClient = new BlobClient(new Uri(blobUrl));
        await blobClient.DeleteIfExistsAsync(cancellationToken: ct);
    }

    public Task<string> GenerateSasTokenAsync(string blobUrl, TimeSpan expiry, CancellationToken ct = default)
    {
        var blobClient = new BlobClient(new Uri(blobUrl));
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = blobClient.BlobContainerName,
            BlobName = blobClient.Name,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.Add(expiry)
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        var sasUri = blobClient.GenerateSasUri(sasBuilder);
        return Task.FromResult(sasUri.ToString());
    }
}
