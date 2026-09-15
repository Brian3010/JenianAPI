using Jenian.Application.Common;
using Jenian.Application.Features.Shifts.Commands;
using Jenian.Application.Features.Shifts.Dtos;

namespace Jenian.Application.Features.Shifts.Services
{
  public interface IShiftService
  {

    /* Shift Management */
    Task<ServiceResult<ShiftSummaryResult>> SaveShiftsAsync(
        SaveShiftsCommand command,
        CancellationToken cancellationToken);
    Task<ServiceResult<ShiftSummaryResult>> GetShiftsByUserAndDateRangeAsync(GetShiftsForUserByDateRangeCommand command, CancellationToken cancellationToken);

    Task<ServiceResult<CurrentPayCycleShiftSummaryResult>> GetCurrentPayCycleShiftsAsync(
      GetCurrentPayCycleShiftsCommand command,
      CancellationToken cancellationToken);

    Task<ServiceResult<CurrentPayCycleSummaryResult>> GetCurrentPayCycleSummaryAsync(
      GetCurrentPayCycleSummaryCommand command,
      CancellationToken cancellationToken);


    /* Pay Cycle Settings */
    Task<ServiceResult<PayCycleSettingsDto>> GetCurrentPayCycleSettingsForUserAsync(string userId, CancellationToken cancellationToken);

    Task<ServiceResult<PayCycleSettingsDto>> UpdatePayCycleSettingsForUserAsync(CreatePayCycleSettingsCommand command, CancellationToken cancellationToken);


  }
}
