using JenianAPI.Data;
using JenianAPI.Models.BackgroundJobsModels;
using JenianAPI.Models.JenianModels;
using Microsoft.EntityFrameworkCore;

namespace JenianAPI.Repositories
{
  /** Manage End of Day Report in the database */
  public class SQLCWHReportRepository
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
        _logger.LogInformation("Background Job found: {@job}", job);
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
      ;

      return deliveryDetails.FirstOrDefault()?.Result ?? "No result found";
    }

    /** 
     * Add or Update End of day report
     */
    public async Task AddOrUpdateEodReportAsync(Guid reportId, EodReport details) {
      var existingReport = await _dbContext.EodReports
        .Include(r => r.StockUpdate)
        .Include(r => r.NightTasks)
        .Include(r => r.AislesFacing)
        .Include(r => r.Cleaning)
        .Include(r => r.GeneralCheck)
        .FirstOrDefaultAsync(r => r.Id == reportId);

      if (existingReport != null) {
        // update
        _dbContext.Entry(existingReport).CurrentValues.SetValues(details);

      } else {
        // add
        await _dbContext.EodReports.AddAsync(details);
      }
      await _dbContext.SaveChangesAsync();
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
    public async Task UpdateAnswerToEodReportAsync(Guid reportId, string answer) {
      var existingReport = await _dbContext.EodReports
        .FirstOrDefaultAsync(r => r.Id == reportId);

      if (existingReport != null) {
        existingReport.Delivery = answer;
        await _dbContext.SaveChangesAsync();
      } else {
        _logger.LogWarning("EodReport with Id {ReportId} not found for updating delivery extracted text.", reportId);
      }
    }

    /** Use report Id to get detail of EodReport
     *  Return: details of the reprot
     */
    public async Task<EodReport?> GetEodReportByIdAsync(Guid reportId) {
      var report = await _dbContext.EodReports
        .Include(r => r.StockUpdate)
        .Include(r => r.NightTasks)
        .Include(r => r.AislesFacing)
        .Include(r => r.Cleaning)
        .Include(r => r.GeneralCheck)
        .FirstOrDefaultAsync(r => r.Id == reportId);


      if (report == null || report.Delivery == null) {
        _logger.LogInformation("EodReport with Id {ReportId} has null Delivery field.", reportId);
        return null;
      }
      return report;

    }

    /** Check if user submited the report today
     *  Return: Boolean
     */
    public async Task<Boolean> IsReportSubmitedToday(string userId) {
      var today = DateTime.UtcNow.Date;
      var isAnyReport = await _dbContext.EodReports.Where(e => e.UserId == userId &e.SubmitedAt.Date == today).AnyAsync();
      if (!isAnyReport) {
        _logger.LogInformation("No EodReport has been submited today.");
        return false;
      }
      return true;
    }




    /**
     * Format the report before saving in the database 
     * Return: report to saved
     */



    /**
     * Send the final report via Telegram 
     */




















  }

}
