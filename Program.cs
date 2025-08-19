
using Azure;
using Azure.AI.Vision.ImageAnalysis;
using JenianAPI.Configurations;
using JenianAPI.Data;
using JenianAPI.Errors;
using JenianAPI.Models.AuthModels;
using JenianAPI.Services;
using JenianAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;


namespace JenianAPI
{
  public class Program
  {
    public static void Main(string[] args) {
      var builder = WebApplication.CreateBuilder(args);

      // tell dot net to run on this port
      //builder.WebHost.UseUrls("http://localhost:5018");
      //builder.WebHost.UseUrls("http://0.0.0.0:5018");

      // -----------------------------
      // CORS
      // -----------------------------
      builder.Services.AddCors(options => {
        // Dev: wide-open (handy for docker + Postman + localhost:3000, 5173, etc.)
        options.AddPolicy("DevCors", p =>
          p.AllowAnyOrigin()     // Tip: don’t combine AllowAnyOrigin with AllowCredentials
           .AllowAnyHeader()
           .AllowAnyMethod()
        );

        // Prod: lock to known frontends (from your original policy)
        options.AddPolicy("ProdCors", policy => {
          policy.WithOrigins(
            "https://your-frontend-domain",     // TODO: set real prod origin(s)
            "http://localhost:3000",            // keep if you want local FE to hit prod API
            "http://192.168.0.219:3000"         // remove if not needed
          )
          .AllowAnyHeader()
          .AllowAnyMethod()
          .AllowCredentials(); // only use credentials with explicit origins
        });
      });

      // Configure global exception handler
      builder.Services.AddProblemDetails();
      builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

      // Configure Serilog Provider
      var logger = new LoggerConfiguration()
        .WriteTo.Console(outputTemplate:
        "{NewLine}[{Timestamp:HH:mm}] {Message:lj}{NewLine}{Exception}")
        .MinimumLevel.Information()
        //.MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning) // Suppress Microsoft logs below Warning
        .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning) // Suppress System logs below Warning
        .CreateLogger();

      builder.Logging.ClearProviders();
      builder.Logging.AddSerilog(logger);
      logger.Information("Serilog starting");
      logger.Information($"Total services: {builder.Services.Count}");

      // Add services to the container.

      builder.Services.AddControllers();
      // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
      builder.Services.AddEndpointsApiExplorer();
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

      // DbContexts
      builder.Services.AddDbContext<JenianAuthDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("JenianAuthConnection")));

      // Add life-time services
      builder.Services.AddSingleton(serviceProvider => {
        var config = serviceProvider.GetRequiredService<IConfiguration>();
        var endpoint = new Uri(config["AzureVision:VisionEndpoint"]);
        var key = config["AzureVision:VisionKey"];

        return new ImageAnalysisClient(endpoint, new AzureKeyCredential(key));
      });
      builder.Services.AddHttpClient();
      builder.Services.AddScoped<IJwtTokenManager, JwtTokenManager>();
      builder.Services.AddScoped<TelegramService>();
      builder.Services.AddScoped<IParserService, AzureVisionAIParserService>();


      // Add Identity system to the ASP.NET Core service container
      builder.Services.AddIdentityCore<ApplicationUser>().AddEntityFrameworkStores<JenianAuthDbContext>()
        .AddDefaultTokenProviders(); // <-- required for reset/confirm tokens;

      builder.Services.Configure<IdentityOptions>(options => {
        // Password settings.
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.Password.RequiredLength = 6;
        options.Password.RequiredUniqueChars = 1;

        options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
        options.User.RequireUniqueEmail = true;
      });


      // JWT Bearers
      builder.Services.ConfigureOptions<JwtBearerConfigurationOptions>().AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

      var app = builder.Build();

      app.UseExceptionHandler();

      // Configure the HTTP request pipeline.
      if (app.Environment.IsDevelopment()) {
        app.UseSwagger();
        app.UseSwaggerUI();
      }
      // -----------------------------
      // HTTPS redirection
      // PROD ONLY: in dev/docker we often skip this so http://localhost:8080 works cleanly
      // -----------------------------
      if (!app.Environment.IsDevelopment()) {
        app.UseHttpsRedirection();
      }


      // -----------------------------
      // CORS
      // -----------------------------
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
