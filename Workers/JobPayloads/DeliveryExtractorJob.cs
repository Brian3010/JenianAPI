namespace JenianAPI.Workers.JobPayloads
{
  public sealed record DeliveryExtractorJob(
    string OcrText,Guid JobId
    );
    
}
