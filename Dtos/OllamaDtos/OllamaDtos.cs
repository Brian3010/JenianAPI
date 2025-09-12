namespace JenianAPI.Dtos.OllamaDtos
{
  public class OllamaDtos
  {

    public sealed record ChatMessage(string Role, string Content);

    public sealed record OptionDecoding(double Temperature, double Repeat_penalty, int Num_ctx, int Mirostat);
    // No Streaming
    public sealed record ChatRequest(string Model, List<ChatMessage> Messages, bool Stream = false);

    // Chat Reponse (non-streaming)
    public sealed record ChatResponse(string Model, ChatMessage Message, bool Done);

    public sealed record VersionPayload(string Version);

  }
}
