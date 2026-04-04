using Jenian.Application.Abstractions.BackgroundJobs;
using Jenian.Application.Abstractions.Messaging;
using Jenian.Domain.Entities;
using Jenian.Infrastructure.BackgroundJobs.JobPayloads;
using Jenian.Infrastructure.Identity;
using Jenian.Infrastructure.Persistence.App;
using Jenian.Infrastructure.Persistence.Repositories;
using Jenian.Infrastructure.Services.AI;
using Jenian.Infrastructure.Services.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Jenian.Infrastructure.BackgroundJobs
{
  public class DeliveryExtractorWorker : BackgroundService
  {
    private readonly IBackgroundJobQueue<DeliveryExtractorJob> _backgroundJobQueue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DeliveryExtractorWorker> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public DeliveryExtractorWorker(IBackgroundJobQueue<DeliveryExtractorJob> backgroundJobQueue, IServiceScopeFactory scopeFactory,
      ILogger<DeliveryExtractorWorker> logger, IHttpClientFactory httpClientFactory) {
      _backgroundJobQueue = backgroundJobQueue;
      _scopeFactory = scopeFactory;
      _logger = logger;
      _httpClientFactory = httpClientFactory;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
      _logger.LogInformation("Background worker for delivery extractor run");

      while (!stoppingToken.IsCancellationRequested) {
        var job = await _backgroundJobQueue.DequeueAsync(stoppingToken);
        _logger.LogInformation("Enqueueing background Job: {@job}", job);
        // Create a scope for each job (or each batch/iteration)
        using var scope = _scopeFactory.CreateScope();
        var openAi = scope.ServiceProvider.GetRequiredService<OpenAiService>();
        var jenianDbContext = scope.ServiceProvider.GetRequiredService<JenianDbContext>();
        var jenianRepository = scope.ServiceProvider.GetRequiredService<SQLCWHReportRepository>();
        var jwtTokenManager = scope.ServiceProvider.GetRequiredService<IJwtTokenManager>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var telegramMessenger = scope.ServiceProvider.GetRequiredService<ITelegramMessenger>();

        try {
          var answer = await openAi.DeliveryTextExtractor(job.OcrText, stoppingToken);
          _logger.LogInformation("DeliveryExtractorWorker processed job. Result: {Result}", answer);
          // update job status
          var bgJob = await jenianDbContext.DeliveryExtractionJobs.FirstAsync(d => d.Id == job.JobId, cancellationToken: stoppingToken);
          bgJob.Status = JobStatus.Succeeded;
          bgJob.Result = answer;
          _logger.LogInformation("BackgroundJob to save in the database {@bgJob}", bgJob);
          await jenianDbContext.SaveChangesAsync(cancellationToken: stoppingToken);

          // Add answer to DeliveryExtractionJob table
          await jenianRepository.UpdateAnswerToDeliveryAsync(job.JobId, answer);
          // Add answer to delivey column in EodReports table
          await jenianRepository.UpdateAnswerToEodReportAsync(job.userId, answer);

          //TODO:  Check if user account is linked to telegram account yet?
          var telegramUserId = await userManager.Users.Where(u => u.Id == job.userId).Select(u => u.TelegramUserId).SingleOrDefaultAsync(cancellationToken: stoppingToken);

          if (telegramUserId != null) {
            var r = await jenianRepository.PopulateReportTemplateAsync(job.ReportId, job.userId);
            if (r != null) {
              _logger.LogInformation("Background worker r = {r}", r);
              await telegramMessenger.SendMessageAsync(long.Parse(telegramUserId), r, stoppingToken);
            }
          }


        } catch (OperationCanceledException) {
        } catch (Exception e) {
          _logger.LogError(e.Message, "Failed processing DeliveryExtractorJob");
        }
      }

    }
  }
}
