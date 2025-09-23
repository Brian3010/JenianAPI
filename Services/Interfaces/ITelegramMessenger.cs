namespace JenianAPI.Services.Interfaces
{
  public interface ITelegramMessenger
  {
    Task SafeSendMessageAsync(long chatId, string text, CancellationToken ct = default);
  }
}
