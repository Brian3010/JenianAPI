namespace Jenian.Infrastructure.BackgroundJobs.JobPayloads
{
  public sealed record DeliveryWorkerJob(
    Guid ReportId,
    Guid JobId,
    string UserId,
    List<string>? BlobNames
    );
  //public sealed record DeliveryWorkerJob(
  //  Guid ReportId,
  //  string OcrText, Guid JobId, string UserId
  //  );

}
