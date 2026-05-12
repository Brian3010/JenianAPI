using Jenian.Application.Features.Shifts.Dtos;

namespace Jenian.Application.Features.Shifts.Commands
{
  public class SaveShiftsCommand
  {
    public DateOnly RangeStartDate { get; set; }
    public DateOnly RangeEndDate { get; set; }
    public required string UserId { get; set; }
    public required List<ShiftDto> ShiftDtos { get; set; }

    public List<Guid> DeletedShiftIds { get; set; } = [];
  }

}
