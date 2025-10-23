namespace JenianAPI.Concurrency
{
  public class SelectionGate
  {
    private readonly ILogger<SelectionGate> _logger;
    // Dictionary to store Id and time
    // open method accepting an id and TimeToLive
    // inside open -> store the id with accosiated TTL in the dictionary
    // Invoker method accepting id
    // inside Invoker -> compare the current time with TTL and the dictionary

    // run true if current is still within the TTL timeframe.
    // remove the TTL with the id before running the function

    // if not, remove the TTL and return false


    public SelectionGate(ILogger<SelectionGate> logger) {
      _logger = logger;
    }

    public bool Invoker(long id) {


    }
  }
}
