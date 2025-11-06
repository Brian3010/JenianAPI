using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace JenianAPI.Concurrency
{
  public class LatestRequestRunner
  {
    private ConcurrentDictionary<long, CancellationTokenSource> _map = new();

    private readonly ILogger<LatestRequestRunner> _logger;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMemoryCache _memoryCache;



    public LatestRequestRunner(ILogger<LatestRequestRunner> logger, IServiceScopeFactory scopeFactory, IMemoryCache memoryCache) {
      _logger = logger;
      _scopeFactory = scopeFactory;
      _memoryCache = memoryCache;
    }

    public void StartOrRestart(long chatId, Func<IServiceProvider, CancellationToken, Task> hanldeFunc) {
      _logger.LogInformation("Map in LatestRequestRunner {0}", _map);
      // check if there is a previous job and cancel if yes
      if (_memoryCache.TryGetValue(chatId, out CancellationTokenSource? oldCache)) {
        try {
          oldCache?.Cancel();

        } finally {
          oldCache?.Dispose();
          _memoryCache.Remove(chatId);
        }
      }

      //  Create CTS for this new job and remember it
      var cts = new CancellationTokenSource();
      _memoryCache.Set(chatId, cts);

      //run the job off thread
      _ = Task.Run(async () => {
        _logger.LogInformation("Runner: starting work for chatId={ChatId}", chatId);
        try {
          await using var scope = _scopeFactory.CreateAsyncScope();
          await hanldeFunc(scope.ServiceProvider, cts.Token);
          _logger.LogInformation("Runner: completed chatId={ChatId}", chatId);
        } catch (OperationCanceledException) {
          _logger.LogInformation("Runner: cancelled chatId={ChatId}", chatId);
        } catch (Exception ex) {
          _logger.LogError(ex, "Runner: unhandled error chatId={ChatId}", chatId);
        } finally {
          //Only remove if THIS is still the current CTS (avoid races)
          if (_memoryCache.TryGetValue(chatId, out var cur) && ReferenceEquals(cur, cts))
            _memoryCache.Remove(chatId);
          cts.Dispose();
        }

      });


    }


    /*
    public void StartOrRestart(long chatId, Func<IServiceProvider, CancellationToken, Task> hanldeFunc) {
      _logger.LogInformation("Map in LatestRequestRunner {0}", _map);
      // check if there is a previous job and cancel if yes
      if (_map.TryRemove(chatId, out var old)) {
        try { old.Cancel(); } finally { old.Dispose(); }
      }

      //  Create CTS for this new job and remember it
      var cts = new CancellationTokenSource();
      _map[chatId] = cts;

      //run the job off thread
      _ = Task.Run(async () => {
        _logger.LogInformation("Runner: starting work for chatId={ChatId}", chatId);
        try {
          await using var scope = _scopeFactory.CreateAsyncScope();
          await hanldeFunc(scope.ServiceProvider, cts.Token);
          _logger.LogInformation("Runner: completed chatId={ChatId}", chatId);
        } catch (OperationCanceledException) {
          _logger.LogInformation("Runner: cancelled chatId={ChatId}", chatId);
        } catch (Exception ex) {
          _logger.LogError(ex, "Runner: unhandled error chatId={ChatId}", chatId);
        } finally {
          //Only remove if THIS is still the current CTS (avoid races)
          if (_map.TryGetValue(chatId, out var cur) && ReferenceEquals(cur, cts))
            _map.TryRemove(chatId, out _);
          cts.Dispose();
        }

      });


    }
    */

    public void Cancel(long chatId) {
      if (_map.TryRemove(chatId, out var cts)) {
        try { cts.Cancel(); } finally { cts.Dispose(); }
      }
    }


  }


}
