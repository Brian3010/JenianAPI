using Jenian.API.Configurations;
using Jenian.API.Middleware;
using Jenian.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Serilog;
using System.IdentityModel.Tokens.Jwt;

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

      /** Register Infrastructure services (DbContexts, Identity, repositories, workers, etc.) */
      builder.Services.AddInfrastructure(builder.Configuration);
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

      // Customize the API's response for invalid model states (e.g., failed validation) to return a consistent error format.
      builder.Services.Configure<ApiBehaviorOptions>(options => {
        options.InvalidModelStateResponseFactory = context =>
            new BadRequestObjectResult(new {
              message = "Validation failed",
              errors = context.ModelState
            });
      });

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


      // CORS
      if (app.Environment.IsDevelopment())
        app.UseCors("DevCors");
      else
        app.UseCors("ProdCors");

      app.UseAuthentication();
      app.UseAuthorization();

      app.MapControllers();

      app.Run();
    }
  }
}
