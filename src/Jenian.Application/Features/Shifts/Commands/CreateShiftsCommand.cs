using Jenian.Application.Features.Shifts.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jenian.Application.Features.Shifts.Commands
{
  public class CreateShiftsCommand
  {
    public required IEnumerable<ShiftDto> ShiftDtos { get; set; }
    public required string UserId { get; set; }

  }
}
