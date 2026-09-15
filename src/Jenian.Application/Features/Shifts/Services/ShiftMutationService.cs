using Jenian.Application.Abstractions.Persistence;
using Jenian.Application.Common;
using Jenian.Application.Features.Shifts.Commands;
using Jenian.Domain.Entities;

namespace Jenian.Application.Features.Shifts.Services
{
  public class ShiftMutationService : IShiftMutationService
  {
    private readonly IShiftRepository _shiftRepository;

    public ShiftMutationService(IShiftRepository shiftRepository) {
      _shiftRepository = shiftRepository;
    }

    public async Task<ServiceResult<ShiftMutationResult>> ApplyAsync(
      SaveShiftsCommand command,
      CancellationToken cancellationToken) {
      var idsToUpdate = command.ShiftDtos
        .Where(shift => shift.Id.HasValue)
        .Select(shift => shift.Id!.Value)
        .ToHashSet();
      var idsToDelete = command.DeletedShiftIds.ToHashSet();
      var overlappingIds = idsToUpdate.Intersect(idsToDelete).ToList();

      if (overlappingIds.Count != 0) {
        return ServiceResult<ShiftMutationResult>.Failure(
          overlappingIds.Select(id => $"Shift '{id}' cannot be updated and deleted in the same request.").ToList());
      }

      var requestedExistingIds = idsToUpdate.Union(idsToDelete).ToList();
      var existingShifts = (await _shiftRepository.GetByIdsForUserAsync(
        command.UserId,
        requestedExistingIds,
        cancellationToken)).ToList();
      var existingShiftIds = existingShifts.Select(shift => shift.Id).ToHashSet();
      var missingIds = requestedExistingIds.Where(id => !existingShiftIds.Contains(id)).ToList();

      if (missingIds.Count != 0) {
        return ServiceResult<ShiftMutationResult>.Failure(
          missingIds.Select(id => $"Shift '{id}' was not found.").ToList());
      }

      var affectedWorkDates = command.ShiftDtos
        .Select(shift => ShiftDateHelper.GetWorkDate(shift.StartAt, shift.TimeZoneId))
        .ToHashSet();
      affectedWorkDates.UnionWith(existingShifts.Select(
        shift => ShiftDateHelper.GetWorkDate(shift.StartAt, shift.TimeZoneId)));

      var shiftsToUpdate = existingShifts
        .Where(shift => idsToUpdate.Contains(shift.Id))
        .ToDictionary(shift => shift.Id);
      var newShifts = new List<UserShift>();

      foreach (var item in command.ShiftDtos) {
        if (item.Id is null) {
          newShifts.Add(new UserShift {
            UserId = command.UserId,
            StartAt = item.StartAt,
            EndAt = item.EndAt,
            TimeZoneId = item.TimeZoneId,
            UnpaidBreakMinutes = item.UnpaidBreakMinutes,
            PaidBreakMinutes = item.PaidBreakMinutes,
            EntryType = item.EntryType,
            EmploymentType = item.EmploymentType,
            Source = ShiftSource.Manual
          });
          continue;
        }

        var existingShift = shiftsToUpdate[item.Id.Value];
        existingShift.StartAt = item.StartAt;
        existingShift.EndAt = item.EndAt;
        existingShift.TimeZoneId = item.TimeZoneId;
        existingShift.UnpaidBreakMinutes = item.UnpaidBreakMinutes;
        existingShift.PaidBreakMinutes = item.PaidBreakMinutes;
        existingShift.EntryType = item.EntryType;
        existingShift.EmploymentType = item.EmploymentType;
        existingShift.UpdatedAtUtc = DateTimeOffset.UtcNow;
      }

      if (idsToDelete.Count != 0) {
        await _shiftRepository.RemoveByIdsForUserAsync(
          command.UserId,
          idsToDelete,
          cancellationToken);
      }

      if (newShifts.Count != 0) {
        await _shiftRepository.AddRangeAsync(newShifts, cancellationToken);
      }

      return ServiceResult<ShiftMutationResult>.Success(
        new ShiftMutationResult(affectedWorkDates));
    }
  }
}
