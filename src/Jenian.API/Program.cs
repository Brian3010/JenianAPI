using Azure.Identity;
using Azure.Storage.Blobs;
using Jenian.API.Auth;
using Jenian.API.Configurations;
using Jenian.API.Middleware;
using Jenian.Application;
using Jenian.Application.Abstractions.Storage;
using Jenian.Infrastructure;
using Jenian.Infrastructure.storage;
using Jenian.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.RateLimiting;

namespace Jenian.API
{
  public class Program
  {
    public static async Task Main(string[] args) {
      var builder = WebApplication.CreateBuilder(args);

      // appsettings.Local.json is git-ignored and holds per-developer secrets
      // (jwt:Key, API keys, etc.).  It overrides appsettings.Development.json locally.
      builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

      /** Serilog configuration */
      var logger = new LoggerConfiguration()
        .WriteTo.Console(outputTemplate:
        "{NewLine}[{Timestamp:HH:mm}] {Level:u3} {Message:lj}{NewLine}{Exception}")
        .MinimumLevel.Information()
        .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning) // Suppress System logs below Warning
        .CreateLogger();

      builder.Logging.ClearProviders();
      builder.Logging.AddSerilog(logger);
      logger.Information("Serilog starting");
      logger.Information("Jenian starting");
      logger.Information($"Total services: {builder.Services.Count}");
      /**************************************************************/

      /** CORS */
      builder.Services.AddCors(options => {
        // Dev: wide-open (handy for docker + Postman + localhost:3000, 5173, etc.)
        options.AddPolicy("DevCors", p =>
          p.AllowAnyOrigin()
           .AllowAnyHeader()
           .AllowAnyMethod()
        );

        // Prod: lock to known frontends (from your original policy)
        options.AddPolicy("ProdCors", policy => {
          policy.WithOrigins(
            "https://jenian-client.vercel.app",      // TODO: set real prod origin(s)
            "http://localhost:3000"            // keep if you want local FE to hit prod API
          )
          .AllowAnyHeader()
          .AllowAnyMethod()
          .AllowCredentials(); // only use credentials with explicit origins
        });
      });
      /** END */

      /* Global error handling - using ProblemDetails for consistent API error responses.*/
      builder.Services.AddProblemDetails();
      builder.Services.AddExceptionHandler<GlobalExceptionHandler>();


      /** Auth cookie settings */
      builder.Services.Configure<AuthCookieSettings>(builder.Configuration.GetSection("AuthCookies"));




      builder.Services.AddControllers();
      // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
      builder.Services.AddEndpointsApiExplorer();

      /** Swagger with JWT support */
      builder.Services.AddSwaggerGen(options => {
        options.SwaggerDoc("v1", new OpenApiInfo { Title = "Jenian APIs", Version = "V1" });
        options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme {
          Name = "Authorization",
          Description = "Type: Bearer {your JWT}",
          In = ParameterLocation.Header,
          Type = SecuritySchemeType.ApiKey,
          Scheme = "bearer",
        });
        options.AddSecurityRequirement(new OpenApiSecurityRequirement {
                  {
                    new OpenApiSecurityScheme {
                      Reference = new OpenApiReference{Type = ReferenceType.SecurityScheme, Id = JwtBearerDefaults.AuthenticationScheme },
                      Scheme ="Oauth2",
                      Name = JwtBearerDefaults.AuthenticationScheme,
                      In = ParameterLocation.Header
                    },
                     new List<string>()
                  }
                });
      });
      /**************************************************************/

      /** Register Infrastructure and Application services (DbContexts, Identity, repositories, workers, etc.) */
      builder.Services.AddInfrastructure(builder.Configuration);
      builder.Services.AddApplication();

      /**************************************************************/

      /* HTTP clients */
      var jenianApiBaseUrl = builder.Configuration["JenianAPI:BaseUrl"]
          ?? throw new InvalidOperationException("JenianAPI:BaseUrl is not configured.");
      builder.Services.AddHttpClient("JenianAPI", http => {
        http.Timeout = TimeSpan.FromSeconds(30); // set a reasonable timeout for all API calls
        http.BaseAddress = new Uri(jenianApiBaseUrl); // centralize base URL
      });
      builder.Services.AddHttpClient();
      /**************************************************************/

      /** JWT Bearers */
      JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
      builder.Services.ConfigureOptions<JwtBearerConfigurationOptions>().AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
      /**************************************************************/


      /* Rate limiting */
      builder.Services.AddRateLimiter(options => {


        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext => {
          // Use the user's ID as the partition key if authenticated; otherwise, use IP address.
          var userId = httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
          var clientIp =
              httpContext.Connection.RemoteIpAddress?.ToString()
              ?? "anonymous";

          var partitionKey =
               !string.IsNullOrWhiteSpace(userId)
                 ? $"user:{userId}"
                 : $"ip:{clientIp}";
          logger.Information("Rate limiting partition key: {PartitionKey}", partitionKey);
          return RateLimitPartition.GetSlidingWindowLimiter(partitionKey, _ => new SlidingWindowRateLimiterOptions {
            PermitLimit = 60, // max requests per window
            Window = TimeSpan.FromMinutes(1), // window duration
            SegmentsPerWindow = 6, // divide window into segments for smoother rate limiting
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0, // no queuing; reject immediately if limit exceeded
            AutoReplenishment = true
          });
        });

        options.AddPolicy("login", httpContext => {
          var ipAddress =
            httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous";

          return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"login:{ipAddress}",
            factory: _ => new FixedWindowRateLimiterOptions {
              PermitLimit = 30,
              Window = TimeSpan.FromMinutes(1),

              QueueProcessingOrder =
                QueueProcessingOrder.OldestFirst,

              QueueLimit = 0,
              AutoReplenishment = true
            }
          );
        });


        options.OnRejected = async (context, cancellationToken) => {
          if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)) {
            context.HttpContext.Response.Headers["Retry-After"] = Math.Ceiling(retryAfter.TotalSeconds)
          .ToString(CultureInfo.InvariantCulture);

          }

          context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
          await context.HttpContext.Response.WriteAsJsonAsync(
           new {
             message = "Too many requests. Please try again later."
           },
           cancellationToken
         );

        };
      });
      /**************************************************************/


      // Customize the API's response for invalid model states (e.g., failed validation) to return a consistent error format.
      builder.Services.Configure<ApiBehaviorOptions>(options => {
        options.InvalidModelStateResponseFactory = context =>
            new BadRequestObjectResult(new {
              message = "Validation failed",
              errors = context.ModelState
            });
      });


      // Azure Blob Storage configuration and service registration
      builder.Services.Configure<AzureBlobStorageOptions>(
      builder.Configuration.GetSection("AzureBlobStorage"));

      var blobOptions = builder.Configuration
          .GetSection("AzureBlobStorage")
          .Get<AzureBlobStorageOptions>()
          ?? throw new InvalidOperationException("AzureBlobStorage config is missing.");

      if (string.IsNullOrWhiteSpace(blobOptions.AccountUrl))
        throw new InvalidOperationException("AzureBlobStorage:AccountUrl is missing.");

      builder.Services.AddSingleton(_ => {
        var credential = new DefaultAzureCredential();

        return new BlobServiceClient(
            new Uri(blobOptions.AccountUrl),
            credential);
      });

      builder.Services.AddSingleton<IBlobStorageService, AzureBlobStorageService>();
      builder.Services.AddHostedService<BlobContainerInitialiser>();
      /**************************************************************/

      var app = builder.Build();
      logger.Information("App environment: {0}", app.Environment.EnvironmentName);


      /* Run EF Core migrations on startup if the RUN_MIGRATIONS flag is set. */
      var runMigrations = builder.Configuration.GetValue<bool>("RUN_MIGRATIONS");
      if (runMigrations) {
        await app.Services.RunMigrationsAsync(app.Logger);
        return; // one-off migration job
      }

      // Configure the HTTP request pipeline.
      if (app.Environment.IsDevelopment()) {
        app.UseSwagger();
        app.UseSwaggerUI();
      }

      app.UseExceptionHandler();
      // HTTPS redirection
      // PROD ONLY: in dev/docker we often skip this so http://localhost:8080 works cleanly
      if (!app.Environment.IsDevelopment()) {
        app.UseHttpsRedirection();
      }

      app.UseRouting();
      // CORS
      if (app.Environment.IsDevelopment())
        app.UseCors("DevCors");
      else
        app.UseCors("ProdCors");

      // check if different devices produce different IPs
      //app.Use(async (httpContext, next) => {
      //  app.Logger.LogInformation(
      //      """
      //        IP test:
      //        RemoteIp={RemoteIp}
      //        CF-Connecting-IP={CfConnectingIp}
      //        X-Forwarded-For={XForwardedFor}
      //      """,
      //     httpContext.Connection.RemoteIpAddress?.ToString(),
      //     httpContext.Request.Headers["CF-Connecting-IP"].ToString(),
      //     httpContext.Request.Headers["X-Forwarded-For"].ToString()
      //  );

      //  await next();
      //});

      app.UseAuthentication();

      app.UseRateLimiter();
      app.UseAuthorization();

      app.MapControllers();

      app.Run();
    }
  }
}
