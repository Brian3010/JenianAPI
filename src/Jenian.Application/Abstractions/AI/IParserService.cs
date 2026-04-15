namespace Jenian.Application.Abstractions.AI
{
  public interface IParserService
  {

    Task<string> ExtractTextFromRosterPhotoStreamAsync(Stream fileStreams, CancellationToken cancellationToken);
    Task<string> ExtractTextFromDeliveryPhotoStreamAsync(Stream fileStreams, CancellationToken cancellationToken);


    Task<string> ExtractShiftsAsync(string orcText, string staffName, CancellationToken cancellationToken);

  }
}
