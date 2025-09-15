using OpenAI.Chat;

namespace JenianAPI.Services
{
  public class OpenAiService
  {
    private readonly ChatClient _chatClient;

    public OpenAiService(ChatClient chatClient) {
      _chatClient = chatClient;
    }

    public async Task<ChatCompletion> RosterQuery() {

      ChatCompletion completion = await _chatClient.CompleteChatAsync("Say 'This is a test.'");

      return completion;
    }

  }
}
