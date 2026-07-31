using Jenian.Domain.Entities;

namespace Jenian.Application.Abstractions.Persistence
{
  public interface ICWHReportRepository
  {
    Task SaveBackgroundJobIdAsync(DeliveryExtractionJob jobDetails, CancellationToken cancellationToken);
    Task<JobStatus> GetBgJobStatusByIdAsync(Guid jobId, CancellationToken cancellationToken);
    Task<string> GetDeliveryResultById(Guid jobId, CancellationToken cancellationToken);
    Task<Guid> AddOrUpdateEodReportAsync(string userId, EodReport incomingReport, CancellationToken cancellationToken);
    Task UpdateAnswerToDeliveryAsync(Guid jobId, string answer, CancellationToken cancellationToken);
    Task UpdateAnswerToEodReportAsync(string userId, string answer, CancellationToken cancellationToken);
    Task<bool> IsReportSubmitedToday(string userId, CancellationToken cancellationToken);
    Task<string?> PopulateReportTemplateAsync(Guid reportId, string userId, CancellationToken cancellationToken);
  }
}
