namespace JenianAPI.Dtos.OllamaDtos
{
  public class OllamaDtos
  {

    public sealed record ChatMessage(string Role, string Content);

    // No Streaming
    public sealed record ChatRequest(string Model, List<ChatMessage> Message, bool Stream = false);
  }
}
