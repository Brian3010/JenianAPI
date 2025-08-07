using Azure.AI.Vision.ImageAnalysis;
using JenianAPI.Dtos.TelegramDtos;
using JenianAPI.Services.Interfaces;

namespace JenianAPI.Services
{
  public class AzureVisionAIParserService : IParserService
  {
    private readonly ILogger<AzureVisionAIParserService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private ImageAnalysisClient _client;

    public AzureVisionAIParserService(ILogger<AzureVisionAIParserService> logger, HttpClient httpClient, IConfiguration configuration, ImageAnalysisClient client) {
      _logger = logger;
      _httpClient = httpClient;
      _configuration = configuration;
      _client = client;
    }

    /// <summary>
    /// Parse image using MemoryStream
    /// </summary>
    /// <param name="fileStream"></param>
    /// <returns><see cref="ShiftInfoDto"/></returns>
    //public async Task<ShiftInfoDto> ParseShiftFromPhotoAsync(MemoryStream fileStream) {
    public async Task ParseShiftFromPhotoAsync(MemoryStream fileStream, CancellationToken cancellationToken) {

      if (fileStream == null || fileStream.Length == 0)
        throw new Exception("fileStream not provided or empty");

      // MemoryStream acts like a file: once you read it, its internal pointer moves to the end.
      //If you try to convert it to BinaryData without resetting it, the stream is "empty" from that point on.
      fileStream.Position = 0;
      BinaryData imageData = BinaryData.FromStream(fileStream);
      _logger.LogInformation($"Image size: {imageData.ToStream().Length} bytes");

      // Setting up Azure Vision
      VisualFeatures visualFeatures = VisualFeatures.SmartCrops | VisualFeatures.Read | VisualFeatures.Objects;
      ImageAnalysisOptions options = new ImageAnalysisOptions {
        Language = "en",
      };


      // Call the Analyse API
      ImageAnalysisResult result = await _client.AnalyzeAsync(
        imageData,
        visualFeatures,
        options,
        cancellationToken
        );

      //return new ShiftInfoDto {
      //  Location
      //};
      _logger.LogInformation($"result from Azure Vision: {result.Read}");
    }


    //private ShiftInfoDto ParseAIResponse(string aiResponse) {
    //  try {
    //    var shift = JsonSerializer.Deserialize<ShiftInfoDto>(aiResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    //    return shift;
    //  } catch {
    //    return new ShiftInfoDto { RawOutput = aiResponse }; // Fallback if AI gives plain text
    //  }
    //}
  }
}
