using Jenian.Application.Features.PaySummaries.Dtos;

namespace Jenian.Application.Features.Shifts.Dtos
{
  public class CurrentPayCycleShiftSummaryResult
  {
    public bool HasPayCycleSettings { get; set; }
    public string? PayCycle { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public IEnumerable<ShiftDto> Shifts { get; set; } = [];
    public IEnumerable<UserDailyPaySummaryDto> DailySummaries { get; set; } = [];
  }
}
