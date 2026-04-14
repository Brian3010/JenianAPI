namespace Jenian.Application.Abstractions.AI
{
  public interface IParserService
  {

    Task<string> ExtractTextFromPhotoStreamAsync(Stream fileStreams, CancellationToken cancellationToken, bool? isPoligon = true);

    Task<string> ExtractShiftsAsync(string orcText, string staffName, CancellationToken cancellationToken);

  }
}
