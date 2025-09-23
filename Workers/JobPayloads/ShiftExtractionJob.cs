namespace JenianAPI.Workers.JobPayloads
{
  public sealed record ShiftExtractionJob(
    long ChatId,
    string OcrText,
    string StaffName);
}
