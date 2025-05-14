
using JenianAPI.Configurations;
using JenianAPI.Data;
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

      // Configure Serilog Provider
      Log.Logger = new LoggerConfiguration()
        .WriteTo.Console()
        //.WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
        .Enrich.FromLogContext()
        .MinimumLevel.Information()
        .CreateLogger();

      Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .CreateLogger();

      // Replace built-in logger with Serilog
      builder.Host.UseSerilog();
      Log.Logger.Information("Serilog starting");
      Log.Logger.Information($"Host: {builder.Host}");
      Log.Logger.Information($"Total services: {builder.Services.Count}");

      // Add services to the container.

      builder.Services.AddControllers();
      // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
      builder.Services.AddEndpointsApiExplorer();
      builder.Services.AddSwaggerGen(options => {
        options.SwaggerDoc("v1", new OpenApiInfo { Title = "Jenian APIs", Version = "V1" });
        options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme {
          Name = "Authorization",
          Description = "Enter 'Bearer' following by space and JWT.",
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
      builder.Services.AddScoped<IJwtTokenManager, JwtTokenManager>();


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

      // Configure the HTTP request pipeline.
      if (app.Environment.IsDevelopment()) {
        app.UseSwagger();
        app.UseSwaggerUI();
      }

      app.UseHttpsRedirection();

      app.UseAuthentication();
      app.UseAuthorization();


      app.MapControllers();

      app.Run();
    }
  }
}
