using JenianAPI.Dtos.TelegramDtos;
using System.Collections.Concurrent;

namespace JenianAPI.Services
{
  public class StateStore
  {

    public ConcurrentDictionary<long, TaskCompletionSource<TelegramMessage>> Items { get; } = new();
  }
}
