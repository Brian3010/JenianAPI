using Jenian.Application.Features.Shifts.Dtos;

namespace Jenian.Application.Features.Shifts.Commands
{
  public class CreateShiftsCommand
  {
    public required IEnumerable<ShiftDto> ShiftDtos { get; set; }
    public required string UserId { get; set; }

  }
}
