using JenianAPI.Dtos.TelegramDtos;
using JenianAPI.Services.Interfaces;
using System.Net.Http.Headers;
using System.Text.Json;

namespace JenianAPI.Services
{
  public class OpenAIVisionParserService : IParserService
  {
    private readonly ILogger<OpenAIVisionParserService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public OpenAIVisionParserService(ILogger<OpenAIVisionParserService> logger, HttpClient httpClient, IConfiguration configuration) {
      _logger = logger;
      _httpClient = httpClient;
      _configuration = configuration;
    }

    public async Task<ShiftInfoDto> ParseShiftFromPhotoAsync(string base64DataUrl, CancellationToken cancellationToken = default) {
      var apiKey = _configuration["OpenAI:ApiKey"];
      var model = _configuration["OpenAI:Model"];
      // HACK: review Microsoft Azure Computer Vision (OCR)
      var requestPayload = new {
        model,
        messages = new object[] {
          new {
            role = "user",
            content = new object[] {
              new { type ="text", text ="prompt here" },
              new {type = "input_image", image_url = base64DataUrl}
            }
          }
          }
      };

      // Asking/requesting AI to parse
      var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
      request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
      request.Content = JsonContent.Create(requestPayload, options: new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
      var response = await _httpClient.SendAsync(request);
      response.EnsureSuccessStatusCode();

      var responseContent = await response.Content.ReadAsStringAsync();
      Console.WriteLine(responseContent);

      // Optional: Parse content from JSON response
      var json = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken);
      var content = json.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();


      return ParseAIResponse(content);
    }
    private ShiftInfoDto ParseAIResponse(string aiResponse) {
      try {
        var shift = JsonSerializer.Deserialize<ShiftInfoDto>(aiResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return shift;
      } catch {
        return new ShiftInfoDto { RawOutput = aiResponse }; // Fallback if AI gives plain text
      }
    }
  }
}
