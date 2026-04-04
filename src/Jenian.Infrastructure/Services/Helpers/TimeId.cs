namespace Jenian.Infrastructure.Services.Helpers
{
  public class TimeId
  {
    private static long _last;

    public static long UniqueTicks() {
      while (true) {
        long now = DateTime.UtcNow.Ticks;
        long last = Volatile.Read(ref _last);
        long next = Math.Max(now, last + 1);
        if (Interlocked.CompareExchange(ref _last, next, last) == last)
          return next;
      }
    }
  }
}
