using Jenian.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace Jenian.API.Configurations
{
  public class JwtBearerConfigurationOptions : IConfigureNamedOptions<JwtBearerOptions>
  {
    private readonly IConfiguration _configuration;

    public JwtBearerConfigurationOptions(
      IConfiguration configuration) {
      _configuration = configuration;
    }


    public void Configure(string? name, JwtBearerOptions options) {
      var jwt = _configuration.GetSection("jwt");

      options.TokenValidationParameters = new TokenValidationParameters {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwt["Issuer"],
        ValidAudience = jwt["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!))
      };

      options.Events = new JwtBearerEvents {
        OnTokenValidated = async context => {
          var userManager = context.HttpContext.RequestServices
            .GetRequiredService<UserManager<ApplicationUser>>();

          var userId = context.Principal?
            .FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

          if (string.IsNullOrWhiteSpace(userId)) {
            context.Fail("The token does not contain a user identifier.");
            return;
          }

          var user = await userManager.FindByIdAsync(userId);

          if (user is null) {
            context.Fail("The user no longer exists.");
            return;
          }

          if (!user.IsDemoUser) {
            return;
          }

          if (user.DemoStatus == DemoAccountStatus.PendingDeletion) {
            context.Fail("The demo account is pending deletion.");
            return;
          }

          if (!user.DemoExpiresAtUtc.HasValue ||
              DateTimeOffset.UtcNow > user.DemoExpiresAtUtc.Value) {
            context.Fail("The demo account has expired.");
          }
        }
      };
    }

    public void Configure(JwtBearerOptions options) => Configure(Options.DefaultName, options);
  }
}
