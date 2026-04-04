namespace Jenian.Application.Abstractions.BackgroundJobs
{
  public interface IBackgroundJobQueue<T>
  {
    ValueTask EnqueueAsync(T item, CancellationToken ct = default);
    ValueTask<T> DequeueAsync(CancellationToken ct);
  }
}
