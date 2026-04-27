using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jenian.Application.Features.Auth.Commands
{
  public class LoginCommand
  {
    public required string UserName { get; set; }
    public required string Password { get; set; }
    public Guid DeviceId { get; set; }
    public required string RefreshToken { get; set; }
  }
}
