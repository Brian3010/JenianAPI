using Jenian.Application.Features.Shifts.Dtos;

namespace Jenian.Application.Features.Shifts.Services
{

  public interface IPayCalculationService
  {
    public UserDailyPaySummaryDto CalculateDailyPay(List<ShiftDto> shifts, string userId);
    Task RecalculateForDatesAsync(string userId, HashSet<DateOnly> affectedWorkDates, CancellationToken cancellationToken);

  }
}
