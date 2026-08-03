using Jenian.Application.Common;
using Jenian.Application.Features.Shifts.Dtos;

namespace Jenian.Application.Features.Shifts.Validations
{
  public class ShiftValidator : IShiftValidator
  {
    public ValidationResult ValidateSaveShifts(List<ShiftDto> shitfs, DateOnly cycleStartDate, DateOnly cycleEndDate) {
      List<string> errors = [];
      HashSet<(DateTimeOffset, DateTimeOffset endTime)>
         seenShifts = [];


      foreach (var shift in shitfs) {
        var key = (shift.StartAt, shift.EndAt);
        var workDate = ShiftDateHelper.GetWorkDate(shift.StartAt, shift.TimeZoneId);
        if (!seenShifts.Add(key)) {
          errors.Add($"Duplicate shift found, starting on {workDate}.");
        }

        if (shift.StartAt >= shift.EndAt) {
          errors.Add($"Shift starting on {workDate} has StartAt greater than or equal to EndAt.");
        }
        if (string.IsNullOrEmpty(shift.TimeZoneId)) {
          errors.Add($"Shift starting on {workDate} has an empty TimeZoneId.");
        }
        var durationMinutes = (shift.EndAt - shift.StartAt).TotalMinutes;
        var totalBreakMinutes = shift.UnpaidBreakMinutes + shift.PaidBreakMinutes;

        if (totalBreakMinutes > durationMinutes) {
          errors.Add($"Shift starting on {workDate} has break minutes exceeding shift duration.");
        }

        DateOnly startDate = ShiftDateHelper.GetWorkDate(shift.StartAt, shift.TimeZoneId);
        DateOnly endDate = ShiftDateHelper.GetWorkDate(shift.EndAt, shift.TimeZoneId);

        //NOTE: The following is temporary fix. startDate and endDate should be in the same timezone as the cycleStartDate and cycleEndDate. Currently, they are in MelbourneTimezone.


        if (startDate < cycleStartDate || endDate > cycleEndDate) {
          errors.Add($"Shift starting at {workDate} has StartAt or EndAt outside of the cycle date range.");
        }

      }

      return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors);
    }
  }
}
