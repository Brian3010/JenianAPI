using JenianAPI.Dtos.CwhDtos;
using JenianAPI.Services;
using JenianAPI.Services.Interfaces;
using JenianAPI.Workers;
using JenianAPI.Workers.JobPayloads;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace JenianAPI.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class CWHController : ControllerBase
  {
    private readonly ILogger<CWHController> _logger;
    private readonly IParserService _parserService;
    private readonly OpenAiService _openAiService;
    private readonly IBackgroundJobQueue<DeliveryExtractorJob> _jobQueue;

    public CWHController(ILogger<CWHController> logger, IParserService parserService, OpenAiService openAiService, IBackgroundJobQueue<DeliveryExtractorJob> jobQueue) {
      _logger = logger;
      _parserService = parserService;
      _openAiService = openAiService;
      _jobQueue = jobQueue;
    }


    //[Authorize]
    [HttpPost("eod-report")]
    public async Task<IActionResult> HandleReport([FromForm] CWHReportRequestDTOs CWHReportRequest) {
      _logger.LogInformation("EOD-REPORT POST API HIT");
      if (CWHReportRequest == null) {
        return BadRequest("Invalid report data.");
      }
      // process the StockUpdate - process the DeliveryScreenShots
      if (CWHReportRequest.DeliveryScreenShots == null || CWHReportRequest.DeliveryScreenShots.Count == 0) {
        return BadRequest("No delivery screenshots provided.");
      }
      // parse the photos into OCR TEXT
      var result = await HandleStockUpdate(CWHReportRequest.DeliveryScreenShots);
      _logger.LogInformation("OCR Result: {OcrResult}", result);

      //var test = await _openAiService.DeliveryTextExtractor(result);
      //_logger.LogInformation("OpenAI Result: {OpenAiResult}", test);

      await _jobQueue.EnqueueAsync(new DeliveryExtractorJob (result));


      // process the NightTasks
      // process the AislesFacing
      // process the Cleaning
      // process the GeneralCheck

      return Ok();
    }

    private async Task<string> HandleStockUpdate(List<IFormFile> deliveryScreenShots) {
      StringBuilder allOcrText = new StringBuilder();
      foreach (var formFile in deliveryScreenShots) {
        if (formFile.Length > 0) {
          using var memoryStream = new MemoryStream();
          await formFile.CopyToAsync(memoryStream);
          var fileBytes = memoryStream.ToArray();
          var ocrText = await _parserService.ExtractTextFromPhotoAsync(fileBytes, CancellationToken.None,false);
          allOcrText.AppendLine(ocrText);
        }
      }
      return allOcrText.ToString();
    }


  }
}
