using Jenian.Application.Abstractions.AI;
using Jenian.Application.Abstractions.BackgroundJobs;
using Jenian.Application.Abstractions.Messaging;
using Jenian.Application.Abstractions.Persistence;
using Jenian.Application.Abstractions.Storage;
using Jenian.Domain.Entities;
using Jenian.Infrastructure.BackgroundJobs.JobPayloads;
using Jenian.Infrastructure.Identity;
using Jenian.Infrastructure.Persistence.App;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Jenian.Infrastructure.BackgroundJobs
{
  public class DeliveryExtractionWorker : BackgroundService
  {
    private readonly IBackgroundJobQueue<DeliveryWorkerJob> _backgroundJobQueue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DeliveryExtractionWorker> _logger;

    public DeliveryExtractionWorker(IBackgroundJobQueue<DeliveryWorkerJob> backgroundJobQueue, IServiceScopeFactory scopeFactory,
      ILogger<DeliveryExtractionWorker> logger, IHttpClientFactory httpClientFactory) {
      _backgroundJobQueue = backgroundJobQueue;
      _scopeFactory = scopeFactory;
      _logger = logger;
    }


    private async Task<string> BuildOcrTextAsync(
    List<string> blobNames,
    IBlobStorageService blobStorageService,
    IParserService azureParser,
    CancellationToken cancellationToken) {
      if (blobNames == null || blobNames.Count == 0)
        throw new InvalidOperationException("No blob names were provided for OCR processing.");

      var allOcrText = new StringBuilder();

      foreach (var blobName in blobNames) {
        if (string.IsNullOrWhiteSpace(blobName))
          continue;

        // Get blob stream from storage and extract text using Azure OCR
        await using var stream = await blobStorageService.OpenReadAsync(blobName, cancellationToken);

        var ocrText = await azureParser.ExtractTextFromDeliveryPhotoStreamAsync(
            stream,
            cancellationToken
            );

        if (!string.IsNullOrWhiteSpace(ocrText))
          allOcrText.AppendLine(ocrText.Trim());
      }

      var finalOcrText = allOcrText.ToString().Trim();

      //if (string.IsNullOrWhiteSpace(finalOcrText))
      //  throw new InvalidOperationException("OCR completed but produced no text.");

      return finalOcrText ?? string.Empty;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
      _logger.LogInformation("Background worker for delivery extractor run");

      while (!stoppingToken.IsCancellationRequested) {
        var job = await _backgroundJobQueue.DequeueAsync(stoppingToken);
        _logger.LogInformation("Enqueueing background Job: {@job}", job);
        // Create a scope for each job (or each batch/iteration)
        using var scope = _scopeFactory.CreateScope();
        var openAi = scope.ServiceProvider.GetRequiredService<IOpenAiService>();
        var jenianDbContext = scope.ServiceProvider.GetRequiredService<JenianDbContext>();
        var reportRepository = scope.ServiceProvider.GetRequiredService<ICWHReportRepository>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var telegramMessenger = scope.ServiceProvider.GetRequiredService<ITelegramMessenger>();
        var blobStorage = scope.ServiceProvider.GetRequiredService<IBlobStorageService>();
        var parserService = scope.ServiceProvider.GetRequiredService<IParserService>();

        try {

          var ocrText = string.Empty;
          if (job.BlobNames is { Count: > 0 }) {
            ocrText = await BuildOcrTextAsync(job.BlobNames, blobStorage, parserService, stoppingToken);
          }

          // read OCR text and extract delivery information using OpenAI service  
          var answer = await openAi.DeliveryTextExtractor(ocrText, stoppingToken);
          _logger.LogInformation("DeliveryExtractorWorker processed job. Result: {Result}", answer);
          // update job status
          var bgJob = await jenianDbContext.DeliveryExtractionJobs.FirstAsync(d => d.Id == job.JobId, cancellationToken: stoppingToken);
          bgJob.Status = JobStatus.Succeeded;
          bgJob.Result = answer;
          _logger.LogInformation("BackgroundJob to save in the database {@bgJob}", bgJob);
          await jenianDbContext.SaveChangesAsync(cancellationToken: stoppingToken);

          // Add answer to DeliveryExtractionJob table
          await reportRepository.UpdateAnswerToDeliveryAsync(job.JobId, answer);
          // Add answer to delivey column in EodReports table
          await reportRepository.UpdateAnswerToEodReportAsync(job.UserId, answer);

          //TODO:  Check if user account is linked to telegram account yet?
          var telegramUserId = await userManager.Users.Where(u => u.Id == job.UserId).Select(u => u.TelegramUserId).SingleOrDefaultAsync(cancellationToken: stoppingToken);

          if (telegramUserId != null && long.TryParse(telegramUserId, out var telegramChatId)) {
            var r = await reportRepository.PopulateReportTemplateAsync(job.ReportId, job.UserId);
            if (r != null) {
              _logger.LogInformation("Background worker r = {r}", r);
              await telegramMessenger.SendMessageAsync(telegramChatId, r, stoppingToken);
            }
          }


        } catch (OperationCanceledException) {
        } catch (Exception e) {
          _logger.LogError(e, "Failed processing DeliveryExtractorJob");
        }
      }

    }
  }
}
