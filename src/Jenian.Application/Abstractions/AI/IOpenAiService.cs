namespace Jenian.Application.Abstractions.AI
{
  public interface IOpenAiService
  {
    Task<string> DeliveryFormatter(string ocrText, CancellationToken ct = default);
    Task<string> RosterQuery(string ocrText, string staffName, CancellationToken ct = default);
  }
}
