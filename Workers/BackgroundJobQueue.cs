
using System.Threading.Channels;

namespace JenianAPI.Workers
{
  public class BackgroundJobQueue<T> : IBackgroundJobQueue<T>
  {

    private readonly Channel<T> _channel;
    public BackgroundJobQueue(int capacity = 200) {
      _channel = Channel.CreateBounded<T>(new BoundedChannelOptions(capacity) {
        FullMode = BoundedChannelFullMode.Wait
      });

    }

    public ValueTask EnqueueAsync(T item, CancellationToken ct = default) =>
      _channel.Writer.WriteAsync(item, ct);


    public ValueTask<T> DequeueAsync(CancellationToken ct) => _channel.Reader.ReadAsync(ct);


  }
}
