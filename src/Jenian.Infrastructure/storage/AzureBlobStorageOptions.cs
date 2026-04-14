namespace Jenian.Infrastructure.Storage;

public sealed class AzureBlobStorageOptions
{
  public string AccountUrl { get; set; } = string.Empty;
  public string ContainerName { get; set; } = string.Empty;
}