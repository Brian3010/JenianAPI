using Jenian.Application.Features.Telegram.Dtos;
using System.Collections.Concurrent;

namespace Jenian.Infrastructure.Services.Telegram
{
  public class StateStore
  {

    public ConcurrentDictionary<long, TaskCompletionSource<TelegramMessage>> Items { get; } = new();
  }
}
