using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jenian.Application.Features.Auth.Commands
{
  public class LogoutCommand
  {
    public string? UserId { get; set; }
    public string? RefreshToken { get; set; }
    public string? DeviceId { get; set; }
  }
}
