using Azure.AI.Vision.ImageAnalysis;
using JenianAPI.Services.Interfaces;
using System.Text;

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
    /// <returns><see cref="string"/></returns>
    public async Task<string> ExtractTextFromPhotoAsync(MemoryStream fileStream, CancellationToken cancellationToken) {

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

      var res = await _client.AnalyzeAsync(
        imageData,
        visualFeatures,
        options,
        cancellationToken
        );

      var read = res.Value.Read;

      if (read?.Blocks is null || read.Blocks.Count == 0)
        return "There's not thing to read."; // nothing to read

      // 2) Concatenate lines into a single OCR_TEXT string
      var ocrText = new StringBuilder();
      foreach (var block in read.Blocks)
        foreach (var line in block.Lines)
          if (!string.IsNullOrWhiteSpace(line.Text))
            ocrText.AppendLine(line.Text);

      //_logger.LogInformation(ocrText.ToString().Trim());

      return ocrText.ToString().Trim();
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
