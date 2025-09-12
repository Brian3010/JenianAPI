using JenianAPI.Configurations;
using Microsoft.Extensions.Options;
using static JenianAPI.Dtos.OllamaDtos.OllamaDtos;

namespace JenianAPI.Services
{
  public class OllamaClient
  {
    private readonly HttpClient _httpClient;
    private readonly OllamaOptions _ollamaOptions;
    private readonly ILogger<OllamaClient> _logger;


    public OllamaClient(HttpClient httpClient, IOptions<OllamaOptions> ollamaOptions, ILogger<OllamaClient> logger) {
      _httpClient = httpClient;
      _ollamaOptions = ollamaOptions.Value;
      _logger = logger;
    }

    // Check if connection ready
    public async Task<bool> IsConnectedAsync(CancellationToken ct = default) {
      var payload = await _httpClient.GetFromJsonAsync<VersionPayload>("/api/version", ct)
                           ?? throw new InvalidOperationException("No version response.");

      return !string.IsNullOrEmpty(payload.Version);
    }

    // Communicate with Ollama
    public async Task<string> ChatAsync(List<ChatMessage> messages, CancellationToken ct = default) {

      var req = new ChatRequest(
        Model: _ollamaOptions.Model,
        Messages: messages,
        Stream: false
        );

      using var res = await _httpClient.PostAsJsonAsync("api/chat", req, ct);
      res.EnsureSuccessStatusCode();

      var payload = await res.Content.ReadFromJsonAsync<ChatResponse>(ct);

      return payload?.Message?.Content ?? "I Couldn't extract the roster. Sorry :-(";
    }



    // --- C) Chat, streaming (typewriter effect) ---
    // Usage:
    //   await foreach (var piece in client.ChatStreamAsync(msgs)) { buffer += piece; }
    /**
    public async IAsyncEnumerable<string> ChatStreamAsync(IEnumerable<ChatMessage> messages, string? modelOverride = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default) {
      var req = new ChatRequest(
          Model: modelOverride ?? Model,
          Messages: messages.ToList(),
          Stream: true
      );

      // Ask for headers-first so we can read as the server sends
      using var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/chat") { Content = JsonContent.Create(req, options: _json) };

      using var res = await _http.SendAsync(httpReq, HttpCompletionOption.ResponseHeadersRead, ct);
      res.EnsureSuccessStatusCode();

      using var stream = await res.Content.ReadAsStreamAsync(ct);
      using var reader = new StreamReader(stream);

      while (!reader.EndOfStream && !ct.IsCancellationRequested) {
        var line = await reader.ReadLineAsync();
        if (string.IsNullOrWhiteSpace(line)) continue;

        using var obj = JsonDocument.Parse(line);
        // Each NDJSON line may have { message: { content: "..." }, done: false }
        if (obj.RootElement.TryGetProperty("message", out var msg) &&
            msg.TryGetProperty("content", out var content)) {
          var piece = content.GetString();
          if (!string.IsNullOrEmpty(piece)) yield return piece!;
        }
        if (obj.RootElement.TryGetProperty("done", out var done) && done.GetBoolean())
          yield break;
      }
    }
    */




  }
}
