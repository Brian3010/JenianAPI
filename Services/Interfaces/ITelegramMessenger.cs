namespace JenianAPI.Services.Interfaces
{
  public interface ITelegramMessenger
  {
    Task SendMessageAsync(long chatId, string text, CancellationToken ct = default);
  }
}
