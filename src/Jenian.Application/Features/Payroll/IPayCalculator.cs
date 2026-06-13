using Jenian.Application.Features.PaySummaries.Dtos;
using Jenian.Application.Features.Shifts.Dtos;
using Jenian.Domain.Entities;

namespace Jenian.Application.Features.Payroll
{
  public record PayCycleDateRange(DateOnly StartDate, DateOnly EndDate);

  public interface IPayCalculator
  {
    public UserDailyPaySummaryDto CalculateDailyPay(List<ShiftDto> shifts, string userId);

    public PayCycleDateRange CalculatePayCycleDateRange(PayCycleType userPayCycle, DateOnly anchorStartDate);
  }
}
