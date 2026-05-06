using Jenian.Application.Abstractions.Persistence;
using Jenian.Application.Features.Shifts.Commands;
using Jenian.Application.Features.Shifts.Dtos;
using Jenian.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jenian.Application.Features.Shifts.Services
{
  public class ShiftService : IShiftService
  {
    private readonly IShiftRepository _shiftRepository;

    public ShiftService(
      IShiftRepository shiftRepository
      ) {
      _shiftRepository = shiftRepository;
    }

    public async Task<IEnumerable<ShiftDto>> CreateShiftsAsync(CreateShiftsCommand command, CancellationToken cancellationToken) {

      if (!command.ShiftDtos.Any()) {
        throw new InvalidOperationException("At least one shift must be provided.");
      }

      var shifts = command.ShiftDtos.Select(item => new UserShift {
        UserId = command.UserId,
        StartAt = item.StartAt,
        EndAt = item.EndAt,
        TimeZoneId = item.TimeZoneId,
        UnpaidBreakMinutes = item.UnpaidBreakMinutes,
        PaidBreakMinutes = item.PaidBreakMinutes,
        EntryType = item.EntryType,
        EmploymentType = item.EmploymentType,
        Source = item.Source,

      });

      var addedShifts = await _shiftRepository.AddRangeAsync(shifts, cancellationToken);

      return addedShifts.Select(shift => new ShiftDto {
        Id = shift.Id,
        StartAt = shift.StartAt,
        EndAt = shift.EndAt,
        TimeZoneId = shift.TimeZoneId,
        UnpaidBreakMinutes = shift.UnpaidBreakMinutes,
        PaidBreakMinutes = shift.PaidBreakMinutes,
        EntryType = shift.EntryType,
        EmploymentType = shift.EmploymentType,
        Source = shift.Source
      });
    }

    public async Task DeleteShiftsAsync(DeleteShiftsCommand command, CancellationToken cancellationToken) {

      if (!command.ShiftIds.Any()) {
        throw new InvalidOperationException("At least one shift ID must be provided.");
      }
      await _shiftRepository.RemoveByIdsForUserAsync(command.UserId, command.ShiftIds, cancellationToken);

    }

    public async Task<IEnumerable<ShiftDto>> GetShiftsForUserAsync(GetShiftsForUserCommand command, CancellationToken cancellationToken) {
      if (!command.ShiftIds.Any()) {
        throw new InvalidOperationException("At least one shift ID must be provided.");
      }

      var shifts = await _shiftRepository.GetByIdsForUserAsync(command.UserId, command.ShiftIds, cancellationToken);

      return shifts.Select(shift => new ShiftDto {
        Id = shift.Id,
        StartAt = shift.StartAt,
        EndAt = shift.EndAt,
        TimeZoneId = shift.TimeZoneId,
        UnpaidBreakMinutes = shift.UnpaidBreakMinutes,
        PaidBreakMinutes = shift.PaidBreakMinutes,
        EntryType = shift.EntryType,
        EmploymentType = shift.EmploymentType,
        Source = shift.Source
      });
    }

    public async Task<IEnumerable<ShiftDto>> SaveShiftsAsync(SaveShiftsCommand command, CancellationToken cancellationToken) {
      // 1. Get all ids that need updating
      var updateIds = command.ShiftCommandDtos
          .Where(x => x.Id.HasValue)
          .Select(x => x.Id!.Value)
          .ToList();

      // 2. Load existing shifts for this user
      var existingShifts = await _shiftRepository.GetByIdsForUserAsync(
          command.UserId,
          updateIds,
          cancellationToken);

      var existingShiftMap = existingShifts.ToDictionary(x => x.Id);

      var newShifts = new List<UserShift>();
      var resultShifts = new List<UserShift>();

      foreach (var item in command.ShiftCommandDtos) {
        if (item.Id is null) {
          // CREATE
          var newShift = new UserShift {
            UserId = command.UserId,
            StartAt = item.StartAt,
            EndAt = item.EndAt,
            TimeZoneId = item.TimeZoneId,
            UnpaidBreakMinutes = item.UnpaidBreakMinutes,
            PaidBreakMinutes = item.PaidBreakMinutes,
            EntryType = item.EntryType,
            EmploymentType = item.EmploymentType,
            Source = ShiftSource.Manual
          };

          newShifts.Add(newShift);
          resultShifts.Add(newShift);
        } else {
          // UPDATE
          if (!existingShiftMap.TryGetValue(item.Id.Value, out var existingShift))
            throw new InvalidOperationException("Shift not found.");

          existingShift.StartAt = item.StartAt;
          existingShift.EndAt = item.EndAt;
          existingShift.TimeZoneId = item.TimeZoneId;
          existingShift.UnpaidBreakMinutes = item.UnpaidBreakMinutes;
          existingShift.PaidBreakMinutes = item.PaidBreakMinutes;
          existingShift.EntryType = item.EntryType;
          existingShift.EmploymentType = item.EmploymentType;
          existingShift.UpdatedAtUtc = DateTimeOffset.UtcNow;

          resultShifts.Add(existingShift);
        }
      }

      if (newShifts.Count > 0) {
        await _shiftRepository.AddRangeAsync(newShifts, cancellationToken);
      }

      await _shiftRepository.SaveChangesAsync(cancellationToken);

      return resultShifts.Select(shift => new ShiftDto {
        Id = shift.Id,
        StartAt = shift.StartAt,
        EndAt = shift.EndAt,
        TimeZoneId = shift.TimeZoneId,
        UnpaidBreakMinutes = shift.UnpaidBreakMinutes,
        PaidBreakMinutes = shift.PaidBreakMinutes,
        EntryType = shift.EntryType,
        EmploymentType = shift.EmploymentType,
        Source = shift.Source
      });
    }

    public async Task<IEnumerable<ShiftDto>> UpdateShiftsAsync(UpdateShiftsCommand command, CancellationToken cancellationToken) {
      if (!command.ShiftDtos.Any()) {
        throw new InvalidOperationException("At least one shift must be provided.");
      }

      var shiftsToUpdate = command.ShiftDtos.Select(item => new UserShift {
        UserId = command.UserId,
        StartAt = item.StartAt,
        EndAt = item.EndAt,
        TimeZoneId = item.TimeZoneId,
        UnpaidBreakMinutes = item.UnpaidBreakMinutes,
        PaidBreakMinutes = item.PaidBreakMinutes,
        EntryType = item.EntryType,
        EmploymentType = item.EmploymentType,
        Source = item.Source,
      });

      var updatedShifts = new List<UserShift>();
      foreach (var shift in shiftsToUpdate) {
        var updatedShift = await _shiftRepository.UpdateAsync(shift, cancellationToken);
        updatedShifts.Add(updatedShift);
      }

      return updatedShifts.Select(shift => new ShiftDto {
        Id = shift.Id,
        StartAt = shift.StartAt,
        EndAt = shift.EndAt,
        TimeZoneId = shift.TimeZoneId,
        UnpaidBreakMinutes = shift.UnpaidBreakMinutes,
        PaidBreakMinutes = shift.PaidBreakMinutes,
        EntryType = shift.EntryType,
        EmploymentType = shift.EmploymentType,
        Source = shift.Source
      });
    }
  }
}
