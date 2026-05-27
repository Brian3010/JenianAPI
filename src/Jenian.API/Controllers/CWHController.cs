using Jenian.API.Contracts.Cwh;
using Jenian.Application.Abstractions.AI;
using Jenian.Application.Abstractions.BackgroundJobs;
using Jenian.Application.Abstractions.Persistence;
using Jenian.Application.Abstractions.Storage;
using Jenian.Application.Features.Shifts.Commands;
using Jenian.Application.Features.Shifts.Services;
using Jenian.Domain.Entities;
using Jenian.Infrastructure.BackgroundJobs.JobPayloads;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace Jenian.API.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class CWHController : ControllerBase
  {
    private readonly ILogger<CWHController> _logger;
    private readonly IParserService _parserService;
    private readonly IOpenAiService _openAiService;
    private readonly IBackgroundJobQueue<DeliveryWorkerJob> _jobQueue;
    private readonly ICWHReportRepository _CWHReportRepository;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IShiftService _shiftService;

    public CWHController(ILogger<CWHController> logger,
      IParserService parserService,
      IOpenAiService openAiService,
      IBackgroundJobQueue<DeliveryWorkerJob> jobQueue,
      ICWHReportRepository CWHReportRepository,
      IBlobStorageService blobStorageService,
      IShiftService shiftService

      ) {
      _logger = logger;
      _parserService = parserService;
      _openAiService = openAiService;
      _jobQueue = jobQueue;
      _CWHReportRepository = CWHReportRepository;
      _blobStorageService = blobStorageService;
      _shiftService = shiftService;
    }

    [Authorize]
    [HttpPost("eod-report")]
    public async Task<IActionResult> HandleReport([FromForm] CWHReportRequest CWHReportRequest, CancellationToken cancellationToken) {
      _logger.LogInformation("EOD-REPORT POST API HIT");
      var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
      if (userId == null) return NotFound("Cannot Find User Information");

      if (CWHReportRequest == null) {
        return BadRequest("Invalid report data.");
      }

      if (CWHReportRequest.DeliveryScreenShots.Count > 5) {
        return BadRequest("You can upload a maximum of 5 photos.");
      }

      // create data for DeliveryExtractionJob table
      var deliveryExtractionData = new DeliveryExtractionJob {

        JobType = nameof(DeliveryExtractionJob),
        UserId = userId
      };

      var eodReport = new EodReport {
        UserId = userId,
        Id = deliveryExtractionData.Id,
        StockUpdate = new StockUpdate {
          TrolleyOfStock = CWHReportRequest.StockUpdate.TrolleyOfStock,
          StockNote = CWHReportRequest.StockUpdate.StockNote,
          TrolleyOfCosmetics = CWHReportRequest.StockUpdate.TrolleyOfCosmetics,
          CosmeticNote = CWHReportRequest.StockUpdate.CosmeticNote,
          TrolleyofFragrances = CWHReportRequest.StockUpdate.TrolleyofFragrances,
          FragranceNote = CWHReportRequest.StockUpdate.FragranceNote,
          AdditionalStock = CWHReportRequest.StockUpdate.AdditionalStock,
          AdditionalNote = CWHReportRequest.StockUpdate.AdditionalNote
        },
        NightTasks = new NightTasks {
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
        AislesFacing = new AislesFacing {
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
        Cleaning = new Cleaning {
          BinRun = CWHReportRequest.Cleaning.BinRun,
          ConsultingRoom = CWHReportRequest.Cleaning.ConsultingRoom,
          Sweeping = CWHReportRequest.Cleaning.Sweeping,
          TeaRoom = CWHReportRequest.Cleaning.TeaRoom
        },
        GeneralCheck = new GeneralCheck {
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

      _logger.LogInformation("backgroundJobDetails: {@backgroundJobDetails}", deliveryExtractionData);
      // Save background job ID to the database
      await _CWHReportRepository.SaveBackgroundJobIdAsync(deliveryExtractionData);

      // Save the initial report data to get the report ID for the background job
      var reportId = await _CWHReportRepository.AddOrUpdateEodReportAsync(userId, eodReport);

      // Handle screenshots: Upload to blob storage and get blob names
      var blobNames = new List<string>();
      if (CWHReportRequest.DeliveryScreenShots != null && CWHReportRequest.DeliveryScreenShots.Count >= 1) {
        foreach (var file in CWHReportRequest.DeliveryScreenShots) {
          if (file.Length <= 0)
            continue;

          await using var stream = file.OpenReadStream();

          // Upload to blob storage
          var blobName = await _blobStorageService.UploadAsync(
              fileStream: stream,
              originalFileName: file.FileName,
              contentType: file.ContentType,
              cancellationToken: cancellationToken);

          blobNames.Add(blobName);
        }
      }

      // Enqueue the job
      var deliveryWorker = new DeliveryWorkerJob(
        reportId,
        deliveryExtractionData.Id,
        userId,
        blobNames
      );
      await _jobQueue.EnqueueAsync(deliveryWorker, cancellationToken);

      _logger.LogInformation("EOD Report to be saved: {@EodReport}", eodReport);

      return Ok(new { reportId = eodReport.Id });
    }

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


    /*** Shift management endpoints for CWH users */

    // GET /api/shift-calculator/current, returning curent pay cycle settings by userId
    [Authorize]
    [HttpGet("shift-calculator/current")]
    public async Task<IActionResult> GetCurrentPayCycleSettings(CancellationToken cancellationToken) {
      var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
      if (userId == null) return NotFound("Cannot Find User Information");
      var result = await _shiftService.GetCurrentPayCycleSettingsForUserAsync(userId, cancellationToken);

      if (!result.IsSuccess) {
        return BadRequest(new { result.Errors });
      }


      return Ok(new { PayCycleSetting = result.Data });
    }

    // POST /api/pay-cycle-settings/update, allowing users to update their pay cycle settings
    [Authorize]
    [HttpPost("pay-cycle-settings/update")]
    public async Task<IActionResult> UpdatePayCycleSettings([FromBody] PayCycleSettingsUpdateRequest request, CancellationToken cancellationToken) {
      var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
      if (string.IsNullOrWhiteSpace(userId)) {
        return Unauthorized("Cannot find user information from token.");
      }

      var command = new CreatePayCycleSettingsCommand {
        UserId = userId,
        PayCycleType = request.PayCycleType,
        AnchorStartDate = request.AnchorStartDate
      };
      var result = await _shiftService.UpdatePayCycleSettingsForUserAsync(command, cancellationToken);


      if (!result.IsSuccess) {
        return BadRequest(new { result.Errors });
      }

      return Ok(new { message = "Pay cycle settings updated successfully.", PayCycleSetting = result.Data });

    }


    // PUT /api/shifts/bulks?cycleStartDate=2026-05-01&cycleEndDate=2026-05-14, submitting shift data for a specified pay cycle
    [HttpPut("/shifts/bulks")]
    public async Task<IActionResult> SubmitShifts(
      [FromQuery] DateOnly cycleStartDate,
      [FromQuery] DateOnly cycleEndDate,
      [FromBody] ShiftSubmissionRequest request,
      CancellationToken cancellationToken) {
      var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
      if (string.IsNullOrWhiteSpace(userId)) {
        throw new UnauthorizedAccessException("Cannot find user information from token.");
      }


      if (cycleEndDate < cycleStartDate) {
        return BadRequest("Invalid date range. Start date must be before or equal to end date.");
      }

      var command = new SaveShiftsCommand {
        UserId = userId,
        ShiftDtos = request.Shifts,
        DeletedShiftIds = request.DeletedShiftIds,
        RangeStartDate = cycleStartDate,
        RangeEndDate = cycleEndDate,
      };

      var result = await _shiftService.SaveShiftsAsync(command, cancellationToken);

      if (!result.IsSuccess) {
        return BadRequest(result.Errors);
      }
      return Ok(result.Data);

    }

    // GET /api/shifts/by-date-range?from=2026-05-01&to=2026-05-14, retrieving shifts for the user within a specified date range
    [Authorize]
    [HttpGet("shifts/by-date-range")]
    public async Task<IActionResult> GetShiftsByDateRange(
      [FromQuery] DateOnly from,
      [FromQuery] DateOnly to,
      CancellationToken cancellationToken) {
      var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
      if (string.IsNullOrWhiteSpace(userId)) {
        return Unauthorized("Cannot find user information from token.");

      }
      if (to < from) {
        return BadRequest("Invalid date range. 'from' date must be before or equal to 'to' date.");
      }

      var command = new GetShiftsForUserByDateRangeCommand {
        UserId = userId,
        From = from,
        To = to,
      };
      var result = await _shiftService.GetShiftsByUserAndDateRangeAsync(command, cancellationToken);
      if (!result.IsSuccess) {
        return BadRequest(new { result.Errors });
      }
      return Ok(new { From = from, To = to, Shifts = result.Data });




    }
  }
}
