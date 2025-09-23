using Azure.AI.Vision.ImageAnalysis;
using JenianAPI.Services.Interfaces;
using System.Text;

namespace JenianAPI.Services
{
  public class AzureVisionAIParserService : IParserService
  {
    private readonly ILogger<AzureVisionAIParserService> _logger;
    private ImageAnalysisClient _client;
    private readonly OpenAiService _openAiService;

    public AzureVisionAIParserService(ILogger<AzureVisionAIParserService> logger, IConfiguration configuration, ImageAnalysisClient client, HttpClient httpClient, OpenAiService openAiService) {
      _logger = logger;
      _client = client;
      _openAiService = openAiService;
    }

    /// <summary>
    /// Parse image using MemoryStream
    /// </summary>
    /// <param name="fileByte"></param>
    /// <returns><see cref="string"/></returns>
    public async Task<string> ExtractTextFromPhotoAsync(byte[] fileByte, CancellationToken cancellationToken) {

      if (fileByte == null || fileByte.Length == 0)
        throw new Exception("fileByte not provided or empty");

      BinaryData imageData = BinaryData.FromBytes(fileByte);
      _logger.LogInformation($"Image size: {imageData.ToStream().Length} bytes");

      // Ask only for text to reduce latency/cost
      var visualFeatures = VisualFeatures.Read;
      var options = new ImageAnalysisOptions {
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
        foreach (var line in block.Lines) {
          //_logger.LogInformation($"   Line: '{line.Text}', Bounding Polygon: [{string.Join(" ", line.BoundingPolygon)}]");
          if (!string.IsNullOrWhiteSpace(line.Text)) {
            ocrText.AppendLine($"{line.Text},[{string.Join(" ", line.BoundingPolygon)}]\n");

          }
          //var t = line?.Text;
          //if (!string.IsNullOrWhiteSpace(t))
          //  ocrText.AppendLine(t);
        }

      _logger.LogInformation(ocrText.ToString().Trim());

      return ocrText.ToString().Trim();
    }

    public async Task<string> ExtractShiftAsync(string ocrText, string staffName, CancellationToken ct = default) {
      if (string.IsNullOrWhiteSpace(ocrText))
        return "Text is empty";

      var res = await _openAiService.RosterQuery(ocrText, staffName);
      return $"{res}";
    }


  }
}

