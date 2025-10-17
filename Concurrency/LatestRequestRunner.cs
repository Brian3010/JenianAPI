using System.Collections.Concurrent;

namespace JenianAPI.Concurrency
{
  public class LatestRequestRunner
  {
    private ConcurrentDictionary<long, CancellationTokenSource> _map = new();

    public void StartOrRestart(long chatId, Func<CancellationToken, Task> hanldeFunc) {

      // check if there is a previous job and cancel if yes
      if (_map.TryRemove(chatId, out var old)) {
        try { old.Cancel(); } finally { old.Dispose(); }
      }

      //  Create CTS for this new job and remember it
      var cts = new CancellationTokenSource();
      _map[chatId] = cts;

      // run the job off thread
      _ = Task.Run(async () => {
        try {
          await hanldeFunc(cts.Token);
        } catch (OperationCanceledException) { /* expected on restart */ } finally {
          //Only remove if THIS is still the current CTS (avoid races)
          if (_map.TryGetValue(chatId, out var cur) && ReferenceEquals(cur, cts))
            _map.TryRemove(chatId, out _);
          cts.Dispose();
        }

      });


    }


    public void Cancel(long chatId) {
      if (_map.TryRemove(chatId, out var cts)) {
        try { cts.Cancel(); } finally { cts.Dispose(); }
      }
    }


  }


}
