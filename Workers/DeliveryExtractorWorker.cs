
using JenianAPI.Data;
using JenianAPI.Models.BackgroundJobsModels;
using JenianAPI.Services;
using JenianAPI.Services.Interfaces;
using JenianAPI.Workers.JobPayloads;
using Microsoft.EntityFrameworkCore;

namespace JenianAPI.Workers
{
  public class DeliveryExtractorWorker : BackgroundService
  {
    private readonly IBackgroundJobQueue<DeliveryExtractorJob> _backgroundJobQueue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DeliveryExtractorWorker> _logger;

    public DeliveryExtractorWorker(IBackgroundJobQueue<DeliveryExtractorJob> backgroundJobQueue, IServiceScopeFactory scopeFactory, ILogger<DeliveryExtractorWorker> logger) {
      _backgroundJobQueue = backgroundJobQueue;
      _scopeFactory = scopeFactory;
      _logger = logger;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
      _logger.LogInformation("Background worker for delivery extractor run");

      while (!stoppingToken.IsCancellationRequested) {
        var job = await _backgroundJobQueue.DequeueAsync(stoppingToken);
        _logger.LogInformation("Enqueueing background Job: {@job}", job);
        // Create a scope for each job (or each batch/iteration)
        using var scope = _scopeFactory.CreateScope();
        var openAi = scope.ServiceProvider.GetRequiredService<OpenAiService>();
        var JenianDbContext = scope.ServiceProvider.GetRequiredService<JenianDbContext>();
        try {
          var answer = await openAi.DeliveryTextExtractor(job.OcrText, stoppingToken);
          _logger.LogInformation("DeliveryExtractorWorker processed job. Result: {Result}", answer);
          var bgJob = await JenianDbContext.DeliveryExtractionJobs.FirstAsync(d =>d.Id == job.JobId, cancellationToken: stoppingToken);
          bgJob.Status = JobStatus.Succeeded;
          bgJob.Result = answer;
          _logger.LogInformation("BackgroundJob to save in the database {@bgJob}",bgJob);
          await JenianDbContext.SaveChangesAsync(cancellationToken: stoppingToken);

          //TODO: while this background running process other intel, after receive answer, trigger sendTelegrammessage
        } catch (OperationCanceledException) {
        } catch (Exception e) {
          _logger.LogError(e.Message, "Failed processing DeliveryExtractorJob");
        }
      }

    }
  }
}
