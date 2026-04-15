using Azure.AI.Vision.ImageAnalysis;
using Jenian.Application.Abstractions.AI;
using System.Text;

namespace Jenian.Infrastructure.Services.AI
{
  public class AzureVisionParserService : IParserService
  {
    private readonly ILogger<AzureVisionParserService> _logger;
    private readonly ImageAnalysisClient _client;
    private readonly IOpenAiService _openAiService;

    public AzureVisionParserService(ILogger<AzureVisionParserService> logger,
      ImageAnalysisClient client,
      IOpenAiService openAiService
      ) {
      _logger = logger;
      _client = client;
      _openAiService = openAiService;
    }

    private async Task<string> GetOcrTextFromStream(Stream fileStream, CancellationToken cancellationToken, bool isPoligon = true) {
      if (fileStream == null)
        return "fileStream is null";
      var imageData = BinaryData.FromStream(fileStream);
      _logger.LogInformation("Image size : {ImageSize} bytes", imageData.ToMemory().Length);
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
              ocrText.AppendLine($"{line.Text}");
            else
              ocrText.AppendLine($"{line.Text},[{string.Join(" ", line.BoundingPolygon)}]");
          }
        }
      return ocrText.ToString().Trim();
    }

    /// <summary>
    /// Parse image using MemoryStream
    /// </summary>
    /// <param name="fileByte"></param>
    /// <returns><see cref="string"/></returns>
    public async Task<string> ExtractTextFromRosterPhotoStreamAsync(Stream fileStreams, CancellationToken cancellationToken) {

      if (fileStreams == null)
        return "fileStreams is null";


      // Preprocess the image to enhance OCR accuracy (deskew, perspective correction, resize)
      byte[] cleanedBytes = await OcrPreprocess.PhotoCleanUpAsync(fileStreams, cancellationToken: cancellationToken);
      await using var cleanedStream = new MemoryStream(cleanedBytes);

      var ocrText = await GetOcrTextFromStream(cleanedStream, cancellationToken, true);
      return ocrText;
    }

    public async Task<string> ExtractShiftsAsync(string ocrText, string staffName, CancellationToken ct = default) {
      if (string.IsNullOrWhiteSpace(ocrText))
        return "Text is empty";

      var res = await _openAiService.RosterQuery(ocrText, staffName);
      return $"{res}";
    }

    public async Task<string> ExtractTextFromDeliveryPhotoStreamAsync(Stream fileStreams, CancellationToken cancellationToken) {
      if (fileStreams == null)
        return "fileStreams is null";
      var ocrText = await GetOcrTextFromStream(fileStreams, cancellationToken, false);
      return ocrText;
    }


  }
}
