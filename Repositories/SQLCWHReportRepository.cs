using JenianAPI.Data;
using JenianAPI.Models.BackgroundJobsModels;
using Microsoft.EntityFrameworkCore;

namespace JenianAPI.Repositories
{
  public class SQLCWHReportRepository
  {
    private readonly JenianDbContext _dbContext;
    private readonly ILogger<SQLCWHReportRepository> _logger;

    public SQLCWHReportRepository(JenianDbContext dbContext, ILogger<SQLCWHReportRepository> logger) {
      _dbContext = dbContext;
      _logger = logger;
    }


    public async Task SaveBackgroundJobIdAsync(DeliveryExtractionJob JobDetails) {
      // save the job details to the database
      await _dbContext.DeliveryExtractionJobs.AddAsync(JobDetails);
      await _dbContext.SaveChangesAsync();
    }


    public async Task<JobStatus> GetBgJobStatus(Guid JobId) {
      var job = await _dbContext.DeliveryExtractionJobs.FindAsync(JobId);
      if (job == null) {
        _logger.LogInformation("Background Job found: {@job}", job);
        return JobStatus.Failed;
      }
      return job.Status;

    }

    public async Task<string> GetStockUpdateDetailByIdAsync(Guid JobId) {
      
      if (await GetBgJobStatus(JobId) != JobStatus.Succeeded) {
        return "Stock update detail might not be ready yet" ;
      }

      var stockUpdateDetail = await _dbContext.DeliveryExtractionJobs
        .Where(j => j.Id == JobId)
        .Select(j => new {
          j.Id,j.Result
        }).ToListAsync();
      ;

      return stockUpdateDetail.FirstOrDefault()?.Result ?? "No result found";
    }
  }
}
