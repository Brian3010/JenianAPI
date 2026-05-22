using Jenian.Application.Common;
using Jenian.Application.Features.Shifts.Dtos;

namespace Jenian.Application.Features.Shifts.Validations
{
  public class ShiftValidator : IShiftValidator
  {
    public ValidationResult ValidateSaveShifts(List<ShiftDto> shitfs, DateOnly cycleStartDate, DateOnly cycleEndDate) {
      List<string> errors = [];

      foreach (var shift in shitfs) {
        if (shift.StartAt >= shift.EndAt) {
          errors.Add($"Shift with Id {shift.Id} has StartAt greater than or equal to EndAt.");
        }
        if (string.IsNullOrEmpty(shift.TimeZoneId)) {
          errors.Add($"Shift with Id {shift.Id} has an empty TimeZoneId.");
        }
        var durationMinutes = (shift.EndAt - shift.StartAt).TotalMinutes;
        var totalBreakMinutes = shift.UnpaidBreakMinutes + shift.PaidBreakMinutes;

        if (totalBreakMinutes > durationMinutes) {
          errors.Add($"Shift with Id {shift.Id} has break minutes exceeding shift duration.");
        }

        var startDate = DateOnly.FromDateTime(shift.StartAt.DateTime);
        var endDate = DateOnly.FromDateTime(shift.EndAt.DateTime);

        if (startDate < cycleStartDate || endDate > cycleEndDate) {
          errors.Add($"Shift with Id {shift.Id} has StartAt or EndAt outside of the cycle date range.");
        }

      }

      return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors);
    }
  }
}
