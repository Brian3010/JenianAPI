namespace JenianAPI.Services.Interfaces
{
  public interface IParserService
  {

    Task<string> ExtractTextFromPhotoAsync(MemoryStream fileStream, CancellationToken cancellationToken);


    Task<string> ExtractShiftAsync(string orcText, string staffName, CancellationToken cancellationToken);
  }
}
