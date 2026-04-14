using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Jenian.Application.Abstractions.Storage;
using Jenian.Infrastructure.Storage;
using Microsoft.Extensions.Options;
using OpenAI.Containers;

namespace Jenian.Infrastructure.storage
{
  public class AzureBlobStorageService : IBlobStorageService
  {
    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<AzureBlobStorageService> _logger;

    public AzureBlobStorageService(
        BlobServiceClient blobServiceClient,
        IOptions<AzureBlobStorageOptions> options,
        ILogger<AzureBlobStorageService> logger
      ) {
      var value = options.Value;

      if (string.IsNullOrWhiteSpace(value.ContainerName))
        throw new InvalidOperationException("AzureBlobStorage:ContainerName is missing.");

      _containerClient = blobServiceClient.GetBlobContainerClient(value.ContainerName);
      _logger = logger;



    }

    public async Task<string> UploadAsync(
        Stream fileStream,
        string originalFileName,
        string? contentType,
        CancellationToken cancellationToken = default) {
      if (fileStream is null)
        throw new ArgumentNullException(nameof(fileStream));

      if (!fileStream.CanRead)
        throw new ArgumentException("File stream must be readable.", nameof(fileStream));

      if (string.IsNullOrWhiteSpace(originalFileName))
        throw new ArgumentException("Original file name is required.", nameof(originalFileName));

      if (fileStream.CanSeek)
        fileStream.Position = 0;

      var blobName = BuildBlobName(originalFileName);
      var blobClient = _containerClient.GetBlobClient(blobName);

      var uploadOptions = new BlobUploadOptions {
        HttpHeaders = new BlobHttpHeaders {
          ContentType = string.IsNullOrWhiteSpace(contentType)
                  ? "application/octet-stream"
                  : contentType
        },
        Metadata = new Dictionary<string, string> {
          ["originalFileName"] = Path.GetFileName(originalFileName),
          ["uploadedAtUtc"] = DateTime.UtcNow.ToString("O"),
          ["category"] = "ocr-temp"
        }
      };

      try {
        await blobClient.UploadAsync(fileStream, uploadOptions, cancellationToken);
        _logger.LogInformation("Uploaded blob {BlobName}", blobName);
        return blobName;
      } catch (RequestFailedException ex) {
        _logger.LogError(ex, "Failed to upload blob {BlobName}", blobName);
        throw;
      }
    }

    public async Task<Stream> OpenReadAsync(
        string blobName,
        CancellationToken cancellationToken = default) {
      if (string.IsNullOrWhiteSpace(blobName))
        throw new ArgumentException("Blob name is required.", nameof(blobName));

      var blobClient = _containerClient.GetBlobClient(blobName);

      try {
        var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
        return response.Value.Content;
      } catch (RequestFailedException ex) {
        _logger.LogError(ex, "Failed to open blob {BlobName}", blobName);
        throw;
      }
    }

    public async Task DeleteIfExistsAsync(
        string blobName,
        CancellationToken cancellationToken = default) {
      if (string.IsNullOrWhiteSpace(blobName))
        throw new ArgumentException("Blob name is required.", nameof(blobName));

      var blobClient = _containerClient.GetBlobClient(blobName);

      try {
        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
        _logger.LogInformation("Deleted blob {BlobName}", blobName);
      } catch (RequestFailedException ex) {
        _logger.LogError(ex, "Failed to delete blob {BlobName}", blobName);
        throw;
      }
    }

    private static string BuildBlobName(string originalFileName) {
      var extension = Path.GetExtension(originalFileName);

      if (string.IsNullOrWhiteSpace(extension))
        extension = ".bin";

      return $"ocr-temp/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
    }
  }
}