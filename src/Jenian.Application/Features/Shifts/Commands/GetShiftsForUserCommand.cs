namespace Jenian.Application.Features.Shifts.Commands
{
  public class GetShiftsForUserByDateRangeCommand
  {
    public required string UserId { get; set; }

    public required DateOnly From { get; set; }
    public required DateOnly To { get; set; }
  }
}
