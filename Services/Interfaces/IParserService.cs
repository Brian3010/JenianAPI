namespace JenianAPI.Services.Interfaces
{
  public interface IParserService
  {
    //Task<ShiftInfoDto> ParseShiftFromPhotoAsync(MemoryStream? fileStream, string? base64DataUrl, CancellationToken? cancellationToken = default);

    Task ParseShiftFromPhotoAsync(MemoryStream fileStream, CancellationToken cancellationToken);
  }
}
