using Jenian.Domain.Entities;

namespace Jenian.Application.Abstractions.Persistence
{
  public interface ICWHReportRepository
  {
    Task SaveBackgroundJobIdAsync(DeliveryExtractionJob jobDetails);
    Task<JobStatus> GetBgJobStatusByIdAsync(Guid jobId);
    Task<string> GetDeliveryResultById(Guid jobId);
    Task<Guid> AddOrUpdateEodReportAsync(string userId, EodReport incomingReport);
    Task UpdateAnswerToDeliveryAsync(Guid jobId, string answer);
    Task UpdateAnswerToEodReportAsync(string userId, string answer);
    Task<bool> IsReportSubmitedToday(string userId);
    Task<string?> PopulateReportTemplateAsync(Guid reportId, string userId);
  }
}
