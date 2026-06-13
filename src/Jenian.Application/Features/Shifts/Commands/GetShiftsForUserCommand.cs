using Jenian.Domain.Entities;

namespace Jenian.Application.Features.Shifts.Commands
{
  public class GetShiftsForUserByDateRangeCommand
  {
    public required string UserId { get; set; }

    public required PayCycleType PayCycleType { get; set; }
  }
}
