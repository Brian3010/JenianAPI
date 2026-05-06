using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jenian.Application.Features.Shifts.Commands
{
  public class DeleteShiftsCommand
  {
    public required IEnumerable<Guid> ShiftIds { get; set; }
    public required string UserId { get; set; }
  }
}
