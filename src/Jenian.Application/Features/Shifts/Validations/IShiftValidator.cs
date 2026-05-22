using Jenian.Application.Common;
using Jenian.Application.Features.Shifts.Dtos;

namespace Jenian.Application.Features.Shifts.Validations
{
  public interface IShiftValidator
  {
    ValidationResult ValidateSaveShifts(List<ShiftDto> shitfs, DateOnly cycleStartDate, DateOnly cycleEndDate);
  }
}
