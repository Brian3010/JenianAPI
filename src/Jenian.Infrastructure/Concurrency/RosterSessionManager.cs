using OpenCvSharp.Internal.Vectors;
using System.Collections.Concurrent;

namespace Jenian.Infrastructure.Concurrency
{


  public class RosterSessionManager
  {

    private readonly ConcurrentDictionary<long, PendingRosterSession> _session = new();

    public void StartOrReplace(long chatId, int timeoutSeconds = 60) {
      var expiresAt = DateTime.UtcNow.AddSeconds(timeoutSeconds);
      _session[chatId] = new PendingRosterSession { ChatId = chatId, ExpiresAtUtc = expiresAt };
    }


    public bool TryConsume(long chatId) {

      // Nothing to consume
      if (!_session.TryGetValue(chatId, out var session))
        return false;

      // Session expired, remove it and return false
      if (session.ExpiresAtUtc < DateTime.UtcNow) {
        _session.TryRemove(chatId, out _);
        return false;
      }


      // Valid session, consume it by removing and return true
      return _session.TryRemove(chatId, out _);
    }

    // Check if there's an active session for the given chatId
    public bool HasActiveSession(long chatId) {
      if (!_session.TryGetValue(chatId, out var session))
        return false;
      if (session.ExpiresAtUtc < DateTime.UtcNow) {
        _session.TryRemove(chatId, out _);
        return false;
      }
      return true;
    }




  }

  internal class PendingRosterSession
  {
    public long ChatId { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
  }
}
