using Jenian.Application.Features.PaySummaries.Dtos;

namespace Jenian.Application.Features.Shifts.Dtos
{
  public class ShiftSummaryResult
  {
    public IEnumerable<ShiftDto> Shifts { get; set; } = [];
    public IEnumerable<UserDailyPaySummaryDto> DailySummaries { get; set; } = [];
  }
}
