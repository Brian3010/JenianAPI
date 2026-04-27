using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jenian.Application.Features.Auth.Commands
{
  public class RefreshTokenCommand
  {
    public string? DeviceId { get; set; }
    public string? RefreshToken { get; set; }
  }
}
