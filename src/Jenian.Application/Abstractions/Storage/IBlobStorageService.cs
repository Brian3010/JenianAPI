
namespace Jenian.Application.Abstractions.Storage
{
  public interface IBlobStorageService
  {
    Task<string> UploadAsync(
        Stream fileStream,
        string originalFileName,
        string? contentType,
        CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(
        string blobName,
        CancellationToken cancellationToken = default);

    Task DeleteIfExistsAsync(
        string blobName,
        CancellationToken cancellationToken = default);
  }
}
