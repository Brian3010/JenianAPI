namespace Jenian.Infrastructure.BackgroundJobs.JobPayloads
{
  public sealed record DeliveryExtractorJob(
    Guid ReportId,
    string OcrText, Guid JobId, string UserId
    );

}
