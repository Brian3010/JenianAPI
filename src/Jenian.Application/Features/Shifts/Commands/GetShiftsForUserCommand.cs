using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jenian.Application.Features.Shifts.Commands
{
  public class GetShiftsForUserCommand
  {
    public required string UserId { get; set; }
    public required List<Guid> ShiftIds { get; set; }
  }
}
