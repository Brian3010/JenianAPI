namespace JenianAPI.Workers.JobPayloads
{
  public sealed record DeliveryExtractorJob(
    Guid ReportId,
    string OcrText,Guid JobId,string userId
    );
    
}
