using Jenian.Application.Common;
using Jenian.Application.Features.Shifts.Commands;
using Jenian.Application.Features.Shifts.Dtos;

namespace Jenian.Application.Features.Shifts.Services
{
  public interface IShiftService
  {

    /* Shift Management */
    Task<ServiceResult<IEnumerable<ShiftDto>>> CreateShiftsAsync(CreateShiftsCommand command, CancellationToken cancellationToken);

    Task<ServiceResult<IEnumerable<ShiftDto>>> UpdateShiftsAsync(UpdateShiftsCommand command, CancellationToken cancellationToken);

    Task<ServiceResult<ShiftSummaryResult>> SaveShiftsAsync(
        SaveShiftsCommand command,
        CancellationToken cancellationToken);

    Task<ServiceResult<bool>> DeleteShiftsAsync(DeleteShiftsCommand command, CancellationToken cancellationToken);
    Task<ServiceResult<ShiftSummaryResult>> GetShiftsByUserAndDateRangeAsync(GetShiftsForUserByDateRangeCommand command, CancellationToken cancellationToken);


    /* Pay Cycle Settings */
    Task<ServiceResult<PayCycleSettingsDto>> GetCurrentPayCycleSettingsForUserAsync(string userId, CancellationToken cancellationToken);

    Task<ServiceResult<PayCycleSettingsDto>> UpdatePayCycleSettingsForUserAsync(CreatePayCycleSettingsCommand command, CancellationToken cancellationToken);


  }
}
