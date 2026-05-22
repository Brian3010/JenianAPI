using Jenian.Application.Abstractions.Persistence;
using Jenian.Application.Common;
using Jenian.Application.Features.Shifts.Commands;
using Jenian.Application.Features.Shifts.Dtos;
using Jenian.Application.Features.Shifts.Validations;
using Jenian.Domain.Entities;

namespace Jenian.Application.Features.Shifts.Services
{
  public class ShiftService : IShiftService
  {
    private readonly IShiftRepository _shiftRepository;
    private readonly IShiftValidator _shiftValidator;

    public ShiftService(
      IShiftRepository shiftRepository,
      IShiftValidator shiftValidator
      ) {
      _shiftRepository = shiftRepository;
      _shiftValidator = shiftValidator;
    }


    /* Shift Management */
    public async Task<ServiceResult<IEnumerable<ShiftDto>>> CreateShiftsAsync(CreateShiftsCommand command, CancellationToken cancellationToken) {

      if (!command.ShiftDtos.Any()) {
        return ServiceResult<IEnumerable<ShiftDto>>.Failure(["At least one shift must be provided."]);
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

      await _shiftRepository.AddRangeAsync(shifts, cancellationToken);

      return ServiceResult<IEnumerable<ShiftDto>>.Success(shifts.Select(shift => new ShiftDto {
        Id = shift.Id,
        StartAt = shift.StartAt,
        EndAt = shift.EndAt,
        TimeZoneId = shift.TimeZoneId,
        UnpaidBreakMinutes = shift.UnpaidBreakMinutes,
        PaidBreakMinutes = shift.PaidBreakMinutes,
        EntryType = shift.EntryType,
        EmploymentType = shift.EmploymentType,
        Source = shift.Source
      }));

    }

    public async Task<ServiceResult<bool>> DeleteShiftsAsync(DeleteShiftsCommand command, CancellationToken cancellationToken) {

      if (!command.ShiftIds.Any()) {
        return ServiceResult<bool>.Failure(["At least one shift ID must be provided."]);
      }
      await _shiftRepository.RemoveByIdsForUserAsync(command.UserId, command.ShiftIds, cancellationToken);
      return ServiceResult<bool>.Success(true);

    }



    public async Task<ServiceResult<IEnumerable<ShiftDto>>> GetShiftsByUserAndDateRangeAsync(GetShiftsForUserByDateRangeCommand command, CancellationToken cancellationToken) {

      var shifts = await _shiftRepository.GetByIdsAndRangeAsync(command.UserId, command.From, command.To, cancellationToken);


      return ServiceResult<IEnumerable<ShiftDto>>.Success(shifts.Select(shift => new ShiftDto {
        Id = shift.Id,
        StartAt = shift.StartAt,
        EndAt = shift.EndAt,
        TimeZoneId = shift.TimeZoneId,
        UnpaidBreakMinutes = shift.UnpaidBreakMinutes,
        PaidBreakMinutes = shift.PaidBreakMinutes,
      }));


    }


    public async Task<ServiceResult<IEnumerable<ShiftDto>>> SaveShiftsAsync(SaveShiftsCommand command, CancellationToken cancellationToken) {

      // Validate shifts
      var validationResult = _shiftValidator.ValidateSaveShifts(command.ShiftDtos, command.RangeStartDate, command.RangeEndDate);
      if (!validationResult.IsValid) {
        return ServiceResult<IEnumerable<ShiftDto>>.Failure(validationResult.Errors);
      }

      // Get all ids that need updating
      var updateIds = command.ShiftDtos
          .Where(x => x.Id.HasValue)
          .Select(x => x.Id!.Value)
          .ToList();

      // Load existing shifts for this user
      var existingShifts = await _shiftRepository.GetByIdsForUserAsync(
          command.UserId,
          updateIds,
          cancellationToken);

      // Create a map of existing shifts for easy lookup
      var existingShiftMap = existingShifts.ToDictionary(x => x.Id);

      var newShifts = new List<UserShift>();
      var resultShifts = new List<UserShift>();

      // Delete shifts if any
      if (command.DeletedShiftIds.Count != 0) {
        await _shiftRepository.RemoveByIdsForUserAsync(command.UserId, command.DeletedShiftIds, cancellationToken);
      }

      foreach (var item in command.ShiftDtos) {
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
            return ServiceResult<IEnumerable<ShiftDto>>.Failure(
                    [$"Shift '{item.Id.Value}' was not found."]);

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

      // Add new shifts
      if (newShifts.Count > 0) {
        await _shiftRepository.AddRangeAsync(newShifts, cancellationToken);
      }

      await _shiftRepository.SaveChangesAsync(cancellationToken);

      // calculate/re-calculator the cycle pay


      return ServiceResult<IEnumerable<ShiftDto>>.Success(resultShifts.Select(shift => new ShiftDto {
        Id = shift.Id,
        StartAt = shift.StartAt,
        EndAt = shift.EndAt,
        TimeZoneId = shift.TimeZoneId,
        UnpaidBreakMinutes = shift.UnpaidBreakMinutes,
        PaidBreakMinutes = shift.PaidBreakMinutes,
        EntryType = shift.EntryType,
        EmploymentType = shift.EmploymentType,
        Source = shift.Source
      }));
    }

    public async Task<ServiceResult<IEnumerable<ShiftDto>>> UpdateShiftsAsync(UpdateShiftsCommand command, CancellationToken cancellationToken) {
      if (!command.ShiftDtos.Any()) {
        return ServiceResult<IEnumerable<ShiftDto>>.Failure(["At least one shift must be provided."]);
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

      foreach (var shift in shiftsToUpdate) {
        await _shiftRepository.UpdateAsync(shift, cancellationToken);
      }
      return ServiceResult<IEnumerable<ShiftDto>>.Success(shiftsToUpdate.Select(shift => new ShiftDto {
        Id = shift.Id,
        StartAt = shift.StartAt,
        EndAt = shift.EndAt,
        TimeZoneId = shift.TimeZoneId,
        UnpaidBreakMinutes = shift.UnpaidBreakMinutes,
        PaidBreakMinutes = shift.PaidBreakMinutes,
        EntryType = shift.EntryType,
        EmploymentType = shift.EmploymentType,
        Source = shift.Source
      }));

    }


    /* Pay Cycle Settings */

    public async Task<ServiceResult<PayCycleSettingsDto>> UpdatePayCycleSettingsForUserAsync(CreatePayCycleSettingsCommand command, CancellationToken cancellationToken) {

      var payCycleSetting = new PayCycleSetting {
        UserId = command.UserId,
        AnchorStartDate = command.AnchorStartDate,
        PayCycleType = (PayCycleType)command.PayCycleType
      };

      await _shiftRepository.UpdatePayCycleSettingsForUserAsync(command.UserId, payCycleSetting, cancellationToken);

      await _shiftRepository.SaveChangesAsync(cancellationToken);

      return ServiceResult<PayCycleSettingsDto>.Success(new PayCycleSettingsDto {
        HasPayCycleSettings = true,
        AnchorStartDate = payCycleSetting.AnchorStartDate,
        PayCycle = (PayCycleTypeDTO)payCycleSetting.PayCycleType
      });
    }



    public async Task<ServiceResult<PayCycleSettingsDto>> GetCurrentPayCycleSettingsForUserAsync(string userId, CancellationToken cancellationToken) {

      var userPayCycle = await _shiftRepository.GetPayCycleSettingByUserIdAsync(userId, cancellationToken);

      if (userPayCycle == null) {
        return ServiceResult<PayCycleSettingsDto>.Success(new PayCycleSettingsDto {
          HasPayCycleSettings = false,
        });
      }

      var todayDate = DateOnly.FromDateTime(DateTime.UtcNow);
      DateOnly cycleStartDate;
      DateOnly cycleEndDate;
      switch (userPayCycle.PayCycleType) {
        case PayCycleType.Weekly:
          var daysSinceWeeklyAnchor = todayDate.DayNumber - userPayCycle.AnchorStartDate.DayNumber;
          var weeklyIndex = Math.Floor(daysSinceWeeklyAnchor / 7.0);
          var currentWeekStart = userPayCycle.AnchorStartDate.AddDays((int)weeklyIndex * 7);

          cycleStartDate = currentWeekStart;
          cycleEndDate = currentWeekStart.AddDays(6);
          break;

        case PayCycleType.Fortnightly:
          var daysSinceFortnightlyAnchor = todayDate.DayNumber - userPayCycle.AnchorStartDate.DayNumber;
          var fortnightIndex = Math.Floor(daysSinceFortnightlyAnchor / 14.0);
          var currentFortnightStart = userPayCycle.AnchorStartDate.AddDays((int)fortnightIndex * 14);

          cycleStartDate = currentFortnightStart;
          cycleEndDate = currentFortnightStart.AddDays(13);
          break;

        case PayCycleType.Monthly:
          cycleStartDate = new DateOnly(todayDate.Year, todayDate.Month, 1);
          cycleEndDate = new DateOnly(
              todayDate.Year,
              todayDate.Month,
              DateTime.DaysInMonth(todayDate.Year, todayDate.Month)
          );
          break;

        default:
          throw new ArgumentOutOfRangeException(nameof(userPayCycle.PayCycleType));
      }


      return ServiceResult<PayCycleSettingsDto>.Success(new PayCycleSettingsDto {
        HasPayCycleSettings = true,
        AnchorStartDate = userPayCycle.AnchorStartDate,
        PayCycle = (PayCycleTypeDTO)userPayCycle.PayCycleType,
        PayCycleStartDate = cycleStartDate,
        PayCycleEndDate = cycleEndDate
      });

    }

  }
}
