using Jenian.API.Contracts.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jenian.Application.Features.Auth.Dtos
{
  public class AuthResultDto
  {
    public required string AccessToken { get; set; }
    public required DateTimeOffset AccessTokenExpiresAtUtc { get; set; }
    public string? RefreshToken { get; set; }
    public required string DeviceId { get; set; }
    public required UserDto User { get; set; }
  }
}
