using JenianAPI.Dtos.CwhDtos;
using JenianAPI.Models.BackgroundJobsModels;
using JenianAPI.Models.JenianModels;
using JenianAPI.Repositories;
using JenianAPI.Services;
using JenianAPI.Services.Interfaces;
using JenianAPI.Workers;
using JenianAPI.Workers.JobPayloads;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
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
    private readonly SQLCWHReportRepository _CWHReportRepository;

    public CWHController(ILogger<CWHController> logger, IParserService parserService, OpenAiService openAiService, IBackgroundJobQueue<DeliveryExtractorJob> jobQueue
      , SQLCWHReportRepository CWHReportRepository) {
      _logger = logger;
      _parserService = parserService;
      _openAiService = openAiService;
      _jobQueue = jobQueue;
      _CWHReportRepository = CWHReportRepository;
    }


    [Authorize]
    [HttpPost("eod-report")]
    public async Task<IActionResult> HandleReport([FromForm] CWHReportRequestDTOs CWHReportRequest) {
      _logger.LogInformation("EOD-REPORT POST API HIT");
      var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
      if (userId == null) return NotFound("Cannot Find User Information");


      if (CWHReportRequest == null) {
        return BadRequest("Invalid report data.");
      }
      // process the StockUpdate - process the DeliveryScreenShots
      if (CWHReportRequest.DeliveryScreenShots == null || CWHReportRequest.DeliveryScreenShots.Count == 0) {
        return BadRequest("No delivery screenshots provided.");
      }
      // parse the photos into OCR TEXT
      var ocrDeliveryResult = await HandleStockUpdate(CWHReportRequest.DeliveryScreenShots);
      _logger.LogInformation("OCR Result: {OcrResult}", ocrDeliveryResult);

      //var test = await _openAiService.DeliveryTextExtractor(result);
      //_logger.LogInformation("OpenAI Result: {OpenAiResult}", test);

      // Save background job ID to the database
      var backgroundJobDetails = new DeliveryExtractionJob {

        JobType = nameof(DeliveryExtractorJob),
        UserId = userId
      };

      var eodReport = new EodReport {
        UserId = userId,
        Id = backgroundJobDetails.Id,
        StockUpdate = new Models.JenianModels.StockUpdate {
          TrolleyOfStock = CWHReportRequest.StockUpdate.TrolleyOfStock,
          StockNote = CWHReportRequest.StockUpdate.StockNote,
          TrolleyOfCosmetics = CWHReportRequest.StockUpdate.TrolleyOfCosmetics,
          CosmeticNote = CWHReportRequest.StockUpdate.CosmeticNote,
          TrolleyofFragrances = CWHReportRequest.StockUpdate.TrolleyofFragrances,
          FragranceNote = CWHReportRequest.StockUpdate.FragranceNote,
          AdditionalStock = CWHReportRequest.StockUpdate.AdditionalStock,
          AdditionalNote = CWHReportRequest.StockUpdate.AdditionalNote
        },
        NightTasks = new Models.JenianModels.NightTasks {
          BatWings = CWHReportRequest.NightTasks.BatWings,
          Catalogue = CWHReportRequest.NightTasks.Catalogue,
          ClipStrips = CWHReportRequest.NightTasks.ClipStrips,
          DispLedge = CWHReportRequest.NightTasks.DispLedge,
          FloorStack = CWHReportRequest.NightTasks.FloorStack,
          Gondolas = CWHReportRequest.NightTasks.Gondolas,
          LowLevel = CWHReportRequest.NightTasks.LowLevel,
          Mesh = CWHReportRequest.NightTasks.Mesh,
          Podiums = CWHReportRequest.NightTasks.Podiums,
          Sunglasses = CWHReportRequest.NightTasks.Sunglasses,
          Tills = CWHReportRequest.NightTasks.Tills,
          TopSellers = CWHReportRequest.NightTasks.TopSellers
        },
        AislesFacing = new Models.JenianModels.AislesFacing {
          BabyFirstAid = CWHReportRequest.AislesFacing.BabyFirstAid,
          Backwall = CWHReportRequest.AislesFacing.Backwall,
          Cosmetics = CWHReportRequest.AislesFacing.Cosmetics,
          FemHygSummer = CWHReportRequest.AislesFacing.FemHygSummer,
          Fragrances = CWHReportRequest.AislesFacing.Fragrances,
          FrontCounter = CWHReportRequest.AislesFacing.FrontCounter,
          Haircare = CWHReportRequest.AislesFacing.Haircare,
          PSA = CWHReportRequest.AislesFacing.PSA,
          Skincare = CWHReportRequest.AislesFacing.Skincare,
          SportNutritions = CWHReportRequest.AislesFacing.SportNutritions,
          Vitamins = CWHReportRequest.AislesFacing.Vitamins
        },
        Cleaning = new Models.JenianModels.Cleaning {
          BinRun = CWHReportRequest.Cleaning.BinRun,
          ConsultingRoom = CWHReportRequest.Cleaning.ConsultingRoom,
          Sweeping = CWHReportRequest.Cleaning.Sweeping,
          TeaRoom = CWHReportRequest.Cleaning.TeaRoom
        },
        GeneralCheck = new Models.JenianModels.GeneralCheck {
          FreeCages = CWHReportRequest.GeneralCheck.FreeCages,
          FreeTrolleys = CWHReportRequest.GeneralCheck.FreeTrolleys,
          NumOfAugmodos = CWHReportRequest.GeneralCheck.NumOfAugmodos,
          NumOfClickCollect = CWHReportRequest.GeneralCheck.NumOfClickCollect,
          NumOfCataBundle = CWHReportRequest.GeneralCheck.NumOfCataBundle,
          NumOfFragKeys = CWHReportRequest.GeneralCheck.NumOfFragKeys,
          NumOfLiftPasses = CWHReportRequest.GeneralCheck.NumOfLiftPasses,
          NumOfMagaBundle = CWHReportRequest.GeneralCheck.NumOfMagaBundle,
          NumOfMyPals = CWHReportRequest.GeneralCheck.NumOfMyPals

        },
      };

      _logger.LogInformation("backgroundJobDetails: {@backgroundJobDetails}", backgroundJobDetails);
      await _CWHReportRepository.SaveBackgroundJobIdAsync(backgroundJobDetails);

      var rId = await _CWHReportRepository.AddOrUpdateEodReportAsync(userId, eodReport);
      //TODO: Might change migration to add AdditonalTasks column to CWHReports table

      // Enqueue the job
      await _jobQueue.EnqueueAsync(new DeliveryExtractorJob(rId, ocrDeliveryResult, backgroundJobDetails.Id, userId));

      _logger.LogInformation("EOD Report to be saved: {@EodReport}", eodReport);

      return Ok(new { reportId = eodReport.Id });
    }

    //TODO: Add get background job status route -> return status
    [Authorize]
    [HttpGet("background-job-status/{jobId}")]
    public async Task<IActionResult> GetBackgroundJobStatus(Guid jobId) {
      var jobDetails = await _CWHReportRepository.GetBgJobStatusByIdAsync(jobId);
      return Ok(jobDetails);
    }


    [Authorize]
    [HttpGet("eod-report/{reportId}")]
    public async Task<IActionResult> GetEodReport(Guid reportId) {
      var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
      if (userId == null) return NotFound("Cannot Find User Id");
      var report = await _CWHReportRepository.PopulateReportTemplateAsync(reportId, userId);
      if (report == null) {
        return NotFound("Report is not ready yet. Please try again later.");
      }
      _logger.LogInformation("Final Report fetched: {@FinalReport}", report);

      return Ok(new { report });
    }

    [Authorize]
    [HttpGet("is-report-submitted")]
    public async Task<IActionResult> IsReportSubmittedToday() {
      var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
      if (userId == null) return NotFound("Cannot Find User Information");
      var isSubmitted = await _CWHReportRepository.IsReportSubmitedToday(userId);
      return Ok(isSubmitted);
    }

    private async Task<string> HandleStockUpdate(List<IFormFile> deliveryScreenShots) {
      StringBuilder allOcrText = new StringBuilder();
      foreach (var formFile in deliveryScreenShots) {
        if (formFile.Length > 0) {
          using var memoryStream = new MemoryStream();
          await formFile.CopyToAsync(memoryStream);
          var fileBytes = memoryStream.ToArray();
          var ocrText = await _parserService.ExtractTextFromPhotoAsync(fileBytes, CancellationToken.None, false);
          allOcrText.AppendLine(ocrText);
        }
      }
      return allOcrText.ToString();
    }


  }
}
