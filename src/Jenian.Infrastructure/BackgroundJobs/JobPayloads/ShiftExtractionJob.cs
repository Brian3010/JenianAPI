namespace Jenian.Infrastructure.BackgroundJobs.JobPayloads
{
  public sealed record ShiftExtractionJob(
    long ChatId,
    string OcrText,
    string StaffName);
}
