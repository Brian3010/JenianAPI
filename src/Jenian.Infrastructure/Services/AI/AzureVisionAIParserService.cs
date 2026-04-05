using Azure.AI.Vision.ImageAnalysis;
using Jenian.Application.Abstractions.AI;
using System.Text;

namespace Jenian.Infrastructure.Services.AI
{
  public class AzureVisionAIParserService : IParserService
  {
    private readonly ILogger<AzureVisionAIParserService> _logger;
    private ImageAnalysisClient _client;
    private readonly IOpenAiService _openAiService;

    public AzureVisionAIParserService(ILogger<AzureVisionAIParserService> logger, IConfiguration configuration, ImageAnalysisClient client, HttpClient httpClient, IOpenAiService openAiService) {
      _logger = logger;
      _client = client;
      _openAiService = openAiService;
    }

    /// <summary>
    /// Parse image using MemoryStream
    /// </summary>
    /// <param name="fileByte"></param>
    /// <returns><see cref="string"/></returns>
    public async Task<string> ExtractTextFromPhotoAsync(byte[] fileByte, CancellationToken cancellationToken, bool? isPoligon = true) {

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
          if (!string.IsNullOrWhiteSpace(line.Text)) {
            if (isPoligon == false)
              ocrText.AppendLine($"{line.Text}\n");
            else
              ocrText.AppendLine($"{line.Text},[{string.Join(" ", line.BoundingPolygon)}]\n");
          }
        }

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
