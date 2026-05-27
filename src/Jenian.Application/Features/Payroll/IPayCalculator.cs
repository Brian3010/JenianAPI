using Jenian.Application.Features.PaySummaries.Dtos;
using Jenian.Application.Features.Shifts.Dtos;

namespace Jenian.Application.Features.Payroll
{
  public interface IPayCalculator
  {
    public UserDailyPaySummaryDto CalculateDailyPay(List<ShiftDto> shifts, string userId);

  }
}
