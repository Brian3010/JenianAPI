namespace JenianAPI.Services.Interfaces
{
  public interface IParserService
  {

    Task<string> ExtractTextFromPhotoAsync(byte[] fileByte, CancellationToken cancellationToken);


    Task<string> ExtractShiftAsync(string orcText, string staffName, CancellationToken cancellationToken);
  }
}
