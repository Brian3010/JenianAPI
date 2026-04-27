using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jenian.Application.Features.Auth.Commands
{
  public class RegisterCommand
  {
    public required string UserName { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }

    public required string ConfirmPassword { get; set; }
    //public required string DeviveId { get; set; }

  }
}
