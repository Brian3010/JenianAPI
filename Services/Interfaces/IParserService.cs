using JenianAPI.Dtos.TelegramDtos;

namespace JenianAPI.Services.Interfaces
{
  public interface IParserService
  {
    Task<ShiftInfoDto> ParseShiftFromPhotoAsync(string base64DataUrl, CancellationToken cancellationToken = default);

  }
}
