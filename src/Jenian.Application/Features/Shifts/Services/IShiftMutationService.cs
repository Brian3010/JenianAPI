using Jenian.Application.Common;
using Jenian.Application.Features.Shifts.Commands;

namespace Jenian.Application.Features.Shifts.Services
{
  public record ShiftMutationResult(HashSet<DateOnly> AffectedWorkDates);

  public interface IShiftMutationService
  {
    Task<ServiceResult<ShiftMutationResult>> ApplyAsync(
      SaveShiftsCommand command,
      CancellationToken cancellationToken);
  }
}
