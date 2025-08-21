namespace JenianAPI.Services
{
  public class OllamaClient
  {
    private readonly HttpClient _httpClient;
    private readonly string OllamaModel;
    private readonly string OllamaBaseUrl;

    public OllamaClient(HttpClient httpClient, IConfiguration configuration) {
      _httpClient = httpClient;
      OllamaModel = configuration["Ollama:Model"] ?? "qwen2.5:7b-instruct";
      OllamaBaseUrl = configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
    }






  }
}
