using JenianAPI.Dtos.CwhDtos;
using JenianAPI.Services.Interfaces;
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

    public CWHController(ILogger<CWHController> logger, IParserService parserService) {
      _logger = logger;
      _parserService = parserService;
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
      var result = HandleStockUpdate(CWHReportRequest.DeliveryScreenShots);
      _logger.LogInformation("OCR Result: {OcrResult}", result);
      // testing with POSTMAN

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
          var ocrText = await _parserService.ExtractTextFromPhotoAsync(fileBytes, CancellationToken.None);
          allOcrText.AppendLine(ocrText);
        }
      }
      return allOcrText.ToString();
    }


  }
}
