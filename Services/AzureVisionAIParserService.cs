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
    private readonly string _modelOllama;

    public AzureVisionAIParserService(ILogger<AzureVisionAIParserService> logger, IConfiguration configuration, ImageAnalysisClient client, HttpClient httpClient, OpenAiService openAiService) {
      _logger = logger;
      _modelOllama = configuration["Ollama:Model"] ?? "qwen2.5:7b-instruct";
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
            ocrText.AppendLine($"{line.Text},[{string.Join(" ", line.BoundingPolygon)}]");

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
      /**
      // Formatting rules for AI system
      var systemRule = new ChatMessage(
         Role: "system",
         Content:
          """
           You extract shifts from OCR roster text.
           - The format will be {word}:[Bounding Polygon coordinates].
           - Days (MON–SUN) are column headers (align by x-position).
           - Names are left; shifts to the right under the correct day.
           - Normalize to 12-hour AM/PM (e.g., 8 - 4 → 8:00AM - 4:00PM; 8 - 4.30 → 8:00AM - 4:30PM).
           - Assume start ≤9 → AM; 1–11 as PM unless contradicted by end.
           - Keep tags like MT/AL/(GV)/(SV)/(BV) as "(TAG)" after time.
           - Ignore noise (CATALOGUE, FULL-TIME, PART-TIME, CASUAL, OFF-SITE EMPLOYEES, IMPORTANT NOTICE, EMPLOYEES NUMBER, etc.).
           - Fix malformed OCR shift times using these rules:
              8 -430 → 8:00AM - 4:30PM
              8 - → discard (missing end)
              11 - → discard (missing end)
              A .4 → discard (nonsense)
              B - 6 → 8:00AM - 6:00PM
              1.9 / 1 9 / 1:9 → 1:00PM - 9:00PM
              3.9 / 3 .9 → 3:00PM - 9:00PM
              11-9 → 11:00AM - 9:00PM
           Output strictly:
           {Staff Name} has shifts on:
           {DAY}: {start - end} (TAG if any)
           (Only include days that have shifts.)
           """
        );
      _logger.LogInformation("OCRText: {0}", ocrText);
      var userPrompt = new ChatMessage(
          Role: "user",
          Content:
          $"""
            Extract shifts for {staffName} using below OCR roster text: 
            {ocrText}

            """
      );

      // Asking AI
      var answer = await _ollamaClient.ChatAsync([systemRule, userPrompt], ct);
      

      return answer;*/


      // trying new method


      var res = await _openAiService.RosterQuery();
      return $"hello {res.Content[0].Text}";
    }


  }
}

