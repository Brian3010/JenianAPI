using Azure;
using Azure.AI.Vision.ImageAnalysis;
using Jenian.Application.Abstractions.AI;
using Jenian.Application.Abstractions.Auth;
using Jenian.Application.Abstractions.BackgroundJobs;
using Jenian.Application.Abstractions.Messaging;
using Jenian.Application.Abstractions.Persistence;
using Jenian.Infrastructure.BackgroundJobs;
using Jenian.Infrastructure.BackgroundJobs.JobPayloads;
using Jenian.Infrastructure.Concurrency;
using Jenian.Infrastructure.Identity;
using Jenian.Infrastructure.Persistence.App;
using Jenian.Infrastructure.Persistence.Auth;
using Jenian.Infrastructure.Persistence.Repositories;
using Jenian.Infrastructure.Services.AI;
using Jenian.Infrastructure.Services.Auth;
using Jenian.Infrastructure.Services.Telegram;
using Jenian.Infrastructure.Services.Telegram.Bots;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI.Chat;

namespace Jenian.Infrastructure
{
  public static class DependencyInjection
  {
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration) {

      /* EF Core with SQL Server - register both Auth and App contexts. */
      services.AddDbContext<JenianAuthDbContext>(options =>
        options.UseSqlServer(configuration.GetConnectionString("JenianAuthConnection")));
      services.AddDbContext<JenianDbContext>(options =>
        options.UseSqlServer(configuration.GetConnectionString("JenianDbConnection")));

      /** Add Identity system to the ASP.NET Core service container */
      services.AddIdentityCore<ApplicationUser>()
        .AddEntityFrameworkStores<JenianAuthDbContext>()
        .AddDefaultTokenProviders();

      services.Configure<IdentityOptions>(options => {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.Password.RequiredLength = 6;
        options.Password.RequiredUniqueChars = 1;

        options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
        options.User.RequireUniqueEmail = true;
      });

      // Register Azure Vision client with endpoint and key
      services.AddSingleton(serviceProvider => {
        var config = serviceProvider.GetRequiredService<IConfiguration>();
        var endpoint = new Uri(config["AzureVision:VisionEndpoint"]!);
        var key = config["AzureVision:VisionKey"];

        return new ImageAnalysisClient(endpoint, new AzureKeyCredential(key!));
      });

      // Register background job queues with a capacity of 200 (tune as needed)
      services.AddSingleton<IBackgroundJobQueue<ShiftExtractionJob>>(
            _ => new BackgroundJobQueue<ShiftExtractionJob>(capacity: 200));
      services.AddSingleton<IBackgroundJobQueue<DeliveryExtractorJob>>(
            _ => new BackgroundJobQueue<DeliveryExtractorJob>(capacity: 200));

      /** OpenAI Setup */
      services.AddSingleton<ChatClient>(serviceProvider => {
        var config = serviceProvider.GetRequiredService<IConfiguration>();
        var apiKey = config["OpenAI:ApiKey"];
        var model = config["OpenAI:Model"];

        return new ChatClient(model, apiKey);
      });

      // LatestRequestRunner and StateStore are singletons to maintain shared state across the app
      services.AddSingleton<LatestRequestRunner>();
      services.AddSingleton<RosterSessionManager>();
      services.AddSingleton<StateStore>();

      services.AddHostedService<ShiftExtractionWorker>();
      services.AddHostedService<DeliveryExtractorWorker>();

      services.AddScoped<IJwtTokenManager, JwtTokenManager>();
      services.AddScoped<ITelegramService, TelegramService>();
      services.AddScoped<IParserService, AzureVisionAIParserService>();
      services.AddScoped<IOpenAiService, OpenAiService>();
      services.AddScoped<ITelegramMessenger, TelegramMessenger>();
      services.AddScoped<IRosterExtractor, TableRosterExtractor>();
      services.AddScoped<IReportChemistBot, ReportChemistBot>();
      services.AddScoped<ICWHReportRepository, SQLCWHReportRepository>();
      services.AddScoped<IJenianAuthRepository, SQLJenianAuthRepository>();

      return services;
    }
    public static async Task RunMigrationsAsync(this IServiceProvider serviceProvider, ILogger logger) {
      logger.LogWarning("### RUN_MIGRATIONS=true - starting EF migrations ###");

      try {
        using var scope = serviceProvider.CreateScope();
        var sp = scope.ServiceProvider;

        var authDb = sp.GetRequiredService<JenianAuthDbContext>();
        await authDb.Database.MigrateAsync();
        logger.LogWarning("### AuthDbContext migrated ###");

        var appDb = sp.GetRequiredService<JenianDbContext>();
        await appDb.Database.MigrateAsync();
        logger.LogWarning("### AppDbContext migrated ###");

        logger.LogWarning("### EF migrations completed successfully ###");
      } catch (Exception ex) {
        logger.LogError(ex, "### EF migrations FAILED ###");
        throw;
      }
    }
  }
}
