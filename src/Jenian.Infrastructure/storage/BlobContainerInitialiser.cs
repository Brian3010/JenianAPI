
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Jenian.Infrastructure.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jenian.Infrastructure.storage
{
  public class BlobContainerInitialiser : IHostedService
  {

    private readonly BlobServiceClient _blobServiceClient;
    private readonly AzureBlobStorageOptions _options;
    private readonly ILogger<BlobContainerInitialiser> _logger;


    public BlobContainerInitialiser(
        BlobServiceClient blobServiceClient,
        IOptions<AzureBlobStorageOptions> options,
        ILogger<BlobContainerInitialiser> logger
      ) {
      _blobServiceClient = blobServiceClient;
      _options = options.Value;
      _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken) {
      if (string.IsNullOrWhiteSpace(_options.ContainerName))
        throw new InvalidOperationException("AzureBlobStorage:ContainerName is missing.");


      var containerClient = _blobServiceClient.GetBlobContainerClient(_options.ContainerName);


      try {
        await containerClient.CreateIfNotExistsAsync(
         PublicAccessType.None,
         cancellationToken: cancellationToken);
      } catch (Exception ex) {
        _logger.LogError(ex, "Failed to initialise blob container");
      }


      _logger.LogInformation(
          "Verified blob container {ContainerName} exists.",
          _options.ContainerName);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

  }
}
