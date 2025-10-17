using System.Collections.Concurrent;

namespace JenianAPI.Helpers
{
  public class TelegramSession
  {
    private static ConcurrentDictionary<long, DateTime> _cache = new();
    public static void Open(long chatId, TimeSpan TTL) {
      _cache[chatId] = DateTime.UtcNow.Add(TTL);
    }

    public static bool Consume(long chatId) {

      if (_cache.TryGetValue(chatId, out var deadline)) {

        // check if time still valid
        if (DateTime.UtcNow <= deadline) {

          _ = _cache.TryRemove(chatId, out _);
          return true;
        }
        _ = _cache.TryRemove(chatId, out _);


      }

      return false;
    }

  }
}
