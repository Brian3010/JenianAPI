using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Jenian.API.Configurations
{
  public class JwtBearerConfigurationOptions : IConfigureNamedOptions<JwtBearerOptions>
  {
    private readonly IConfiguration _configuration;

    public JwtBearerConfigurationOptions(IConfiguration configuration) {
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
    }

    public void Configure(JwtBearerOptions options) => Configure(Options.DefaultName, options);
  }
}
