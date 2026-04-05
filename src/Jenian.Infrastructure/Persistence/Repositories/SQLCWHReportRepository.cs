using Jenian.Application.Abstractions.Messaging;
using Jenian.Application.Abstractions.Persistence;
using Jenian.Domain.Entities;
using Jenian.Infrastructure.Persistence.App;
using Microsoft.EntityFrameworkCore;

namespace Jenian.Infrastructure.Persistence.Repositories
{
  /** Manage End of Day Report in the database */
  public class SQLCWHReportRepository : ICWHReportRepository
  {
    private readonly JenianDbContext _dbContext;
    private readonly ILogger<SQLCWHReportRepository> _logger;

    public SQLCWHReportRepository(JenianDbContext dbContext, ILogger<SQLCWHReportRepository> logger) {
      _dbContext = dbContext;
      _logger = logger;
    }

    /**
     * Save delivery extractor job in database
     */
    public async Task SaveBackgroundJobIdAsync(DeliveryExtractionJob JobDetails) {
      // save the job details to the database
      await _dbContext.DeliveryExtractionJobs.AddAsync(JobDetails);
      await _dbContext.SaveChangesAsync();
    }

    /**
     * Get the status of delivery extraction
     * status.Failed means extraction fails
     * 
     * Return: Background job status
     */
    public async Task<JobStatus> GetBgJobStatusByIdAsync(Guid JobId) {
      var job = await _dbContext.DeliveryExtractionJobs.FindAsync(JobId);
      if (job == null) {
        _logger.LogInformation("Background Job not found for id: {JobId}", JobId);
        return JobStatus.Failed;
      }
      return job.Status;

    }

    /**
     * Retreive the delivery details
     * Return: delivery details from DeliveryExtractionJobs table including Id and Result
     */
    public async Task<string> GetDeliveryResultById(Guid JobId) {

      if (await GetBgJobStatusByIdAsync(JobId) != JobStatus.Succeeded) {
        return "Stock update detail might not be ready yet";
      }

      var deliveryDetails = await _dbContext.DeliveryExtractionJobs
        .Where(j => j.Id == JobId)
        .Select(j => new {
          j.Id,
          j.Result
        }).ToListAsync();

      return deliveryDetails.FirstOrDefault()?.Result ?? "No result found";
    }

    /** 
     * Add or Update End of day report
     */
    public async Task<Guid> AddOrUpdateEodReportAsync(string userId, EodReport incommingReport) {
      Guid reportId;

      var existingReport = await _dbContext.EodReports
        .FirstOrDefaultAsync(r => r.UserId == userId && r.SubmitedAt.Date == DateTime.UtcNow.Date);


      _logger.LogInformation("Message {existingReport}", existingReport);

      if (existingReport is null) {
        // add
        reportId = incommingReport.Id;
        await _dbContext.EodReports.AddAsync(incommingReport);
      } else {
        // update
        existingReport.Delivery = incommingReport.Delivery;
        existingReport.StockUpdate = incommingReport.StockUpdate;
        existingReport.NightTasks = incommingReport.NightTasks;
        existingReport.AislesFacing = incommingReport.AislesFacing;
        existingReport.Cleaning = incommingReport.Cleaning;
        existingReport.GeneralCheck = incommingReport.GeneralCheck;
        reportId = existingReport.Id;
      }

      await _dbContext.SaveChangesAsync();

      return reportId;
    }

    /**
     * After the AI extraction done, this function is to Add the its answer to DeliveryExtractionJob table
     */
    public async Task UpdateAnswerToDeliveryAsync(Guid jobId, string answer) {

      var existingReport = await _dbContext.DeliveryExtractionJobs
        .FirstOrDefaultAsync(r => r.Id == jobId);
      if (existingReport != null) {
        existingReport.Result = answer;
        await _dbContext.SaveChangesAsync();
      } else {
        _logger.LogWarning("DeliveryExtractionJob with Id {jobId} not found for updating delivery extracted text.", jobId);
      }
    }

    /**
     * After the AI extraction done, this function is to Add the its answer to EodReports table
     */
    public async Task UpdateAnswerToEodReportAsync(string userId, string answer) {
      var today = DateTime.UtcNow.Date;

      _logger.LogInformation("userId {userId}", userId);

      var existingReport = await _dbContext.EodReports
        .FirstOrDefaultAsync(r => r.UserId == userId && r.SubmitedAt.Date == today);

      if (existingReport != null) {
        existingReport.Delivery = answer;
        await _dbContext.SaveChangesAsync();
      } else {
        _logger.LogWarning("Cannot add AI answer to delivery field as report is not exist ");
      }
    }

    /** Use report Id to get detail of EodReport
     *  Return: details of the reprot
     */
    private async Task<EodReport?> GetRawEodReportByIdAsync(Guid reportId, string userId) {
      var report = await _dbContext.EodReports
        .Include(r => r.StockUpdate)
        .Include(r => r.NightTasks)
        .Include(r => r.AislesFacing)
        .Include(r => r.Cleaning)
        .Include(r => r.GeneralCheck)
        .FirstOrDefaultAsync(r => r.Id == reportId && r.UserId == userId);


      if (report == null || report.Delivery == null) {
        _logger.LogWarning("Delivery field is not ready for the report with Id '{ReportId}'", reportId);
        return null;
      }
      return report;

    }

    /** Check if user submited the report today
     *  Return: Boolean
     */
    public async Task<Boolean> IsReportSubmitedToday(string userId) {
      var today = DateTime.UtcNow.Date;
      var isAnyReport = await _dbContext.EodReports.Where(e => e.UserId == userId && e.SubmitedAt.Date == today).AnyAsync();
      if (!isAnyReport) {
        _logger.LogInformation("No EodReport has been submited today.");
        return false;
      }
      return true;
    }


    /**
     * assemble the report following the template before sending to Telegram 
     * Return: report in string format following the template
     */
    public async Task<string?> PopulateReportTemplateAsync(Guid reportId, string userId) {
      var rawReport = await GetRawEodReportByIdAsync(reportId, userId);
      if (rawReport == null) {
        return null;
      }

      var melbourneTimeZone = TimeZoneInfo.TryFindSystemTimeZoneById("Australia/Melbourne", out var tz)
        ? tz
        : TimeZoneInfo.FindSystemTimeZoneById("AUS Eastern Standard Time"); // Windows fallback
      var melbourneNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, melbourneTimeZone);
      string formattedDate = melbourneNow.ToString("dddd (dd/MM/yyyy)");

      var finalReport =
        "Night Protocol " + formattedDate + "\n\n" +
        "Scan GAPs using Augmodo ✅\n\n" +
        "Deliveries\n" +
        rawReport.Delivery + "\n\n" +
        "Stock Updates\n" +
        FormatStockUpdate(rawReport.StockUpdate) + "\n\n" +

        "Night Tasks:\n" +
        "Off Locations (Fill & Face) @8.00pm\n" +
        "Disp Ledge/Pods/Mesh: " + rawReport.NightTasks.DispLedge + "\n" +
        "Gondolas: " + rawReport.NightTasks.Gondolas + "\n" +
        "Mesh: " + rawReport.NightTasks.Mesh + "\n" +
        "Tills: " + rawReport.NightTasks.Tills + "\n" +
        "Clip Strips: " + rawReport.NightTasks.ClipStrips + "\n" +
        "Podiums: " + rawReport.NightTasks.Podiums + "\n" +
        "Low level: " + rawReport.NightTasks.LowLevel + "\n" +
        "Floor Stack: " + rawReport.NightTasks.FloorStack + "\n" +
        "Top Sellers: " + rawReport.NightTasks.TopSellers + "\n" +
        "Batwings: " + rawReport.NightTasks.BatWings + "\n" +
        "Sunglasses: " + rawReport.NightTasks.Sunglasses + "\n" +
        "Catalogue: " + rawReport.NightTasks.Catalogue + "\n\n" +

        "Aisles (Fill & Face) @8.00pm\n" +
        "Front Counter: " + rawReport.AislesFacing.FrontCounter + "\n" +
        "Fem Hyg, House, Summer: " + rawReport.AislesFacing.FemHygSummer + "\n" +
        "Haircare: " + rawReport.AislesFacing.Haircare + "\n" +
        "Skincare: " + rawReport.AislesFacing.Skincare + "\n" +
        "Vitamins: " + rawReport.AislesFacing.Vitamins + "\n" +
        "PSA: " + rawReport.AislesFacing.PSA + "\n" +
        "Backwall: " + rawReport.AislesFacing.Backwall + "\n" +
        "Sport Nutritions: " + rawReport.AislesFacing.SportNutritions + "\n" +
        "Baby/First Aid: " + rawReport.AislesFacing.BabyFirstAid + "\n" +
        "Cosmetics: " + rawReport.AislesFacing.Cosmetics + "\n" +
        "Fragrance: " + rawReport.AislesFacing.Fragrances + "\n\n" +

        "Cleaning:\n" +
        "Bin Run: " + rawReport.Cleaning.BinRun + "\n" +
        "Sweeping: " + rawReport.Cleaning.Sweeping + "\n" +
        "Tea Room: " + rawReport.Cleaning.TeaRoom + "\n" +
        "Consulting room: " + rawReport.Cleaning.ConsultingRoom + "\n\n" +

        "General Checks:\n" +
        "Free Trolleys: " + rawReport.GeneralCheck.FreeTrolleys + "/14\n" +
        "Free Cages: " + rawReport.GeneralCheck.FreeCages + "/9\n" +
        "# of Outstanding Click & Collect: " + rawReport.GeneralCheck.NumOfClickCollect + "\n" +
        "# of Catalogue Bundles: " + rawReport.GeneralCheck.NumOfCataBundle + "\n" +
        "# of Magazone Bundles: " + rawReport.GeneralCheck.NumOfMagaBundle + "\n" +
        "My Pals on charge: " + rawReport.GeneralCheck.NumOfMyPals + "/5\n" +
        "Fragrance keys on security desk: " + rawReport.GeneralCheck.NumOfFragKeys + "/3\n" +
        "Lift Passes in dispensary: " + rawReport.GeneralCheck.NumOfLiftPasses + "/2\n" +
        "Augmodo in consulting room: " + rawReport.GeneralCheck.NumOfAugmodos + "/4\n"
       ;
      return finalReport;
    }

    private static string FormatStockUpdate(StockUpdate stockUpdate) {
      var stockUpdateString = "";
      // Stock
      if (stockUpdate.TrolleyOfStock > 0) {
        var trolleyNum = stockUpdate.TrolleyOfStock > 1 ? "trolleys" : "trolley";
        stockUpdateString += stockUpdate.TrolleyOfStock + " " + trolleyNum + " of stock";

        if (stockUpdate.StockNote != null) {
          stockUpdateString += " - " + stockUpdate.StockNote;
        }
        stockUpdateString += "\n";
      }
      // Cosmetics
      if (stockUpdate.TrolleyOfCosmetics > 0) {
        var trolleyNum = stockUpdate.TrolleyOfCosmetics > 1 ? "trolleys" : "trolley";
        stockUpdateString += stockUpdate.TrolleyOfCosmetics + " " + trolleyNum + " of cosmetic";

        if (stockUpdate.CosmeticNote != null) {
          stockUpdateString += " - " + stockUpdate.CosmeticNote;
        }
        stockUpdateString += "\n";
      }

      //Fragrances
      if (stockUpdate.TrolleyofFragrances > 0) {
        var trolleyNum = stockUpdate.TrolleyofFragrances > 1 ? "trolleys" : "trolley";
        stockUpdateString += stockUpdate.TrolleyofFragrances + " " + trolleyNum + " of fragrance";

        if (stockUpdate.FragranceNote != null) {
          stockUpdateString += " - " + stockUpdate.FragranceNote;
        }
        stockUpdateString += "\n";
      }

      // other notes
      stockUpdateString += stockUpdate.AdditionalStock;
      stockUpdateString += "\n";
      stockUpdateString += stockUpdate.AdditionalNote;

      return stockUpdateString;
    }


  }

}
