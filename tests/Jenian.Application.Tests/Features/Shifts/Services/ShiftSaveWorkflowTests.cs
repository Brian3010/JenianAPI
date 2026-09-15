using Jenian.Application.Abstractions.Persistence;
using Jenian.Application.Common;
using Jenian.Application.Features.Payroll;
using Jenian.Application.Features.PaySummaries.Services;
using Jenian.Application.Features.Shifts.Commands;
using Jenian.Application.Features.Shifts.Dtos;
using Jenian.Application.Features.Shifts.Services;
using Jenian.Application.Features.Shifts.Validations;
using Jenian.Domain.Entities;

namespace Jenian.Application.Tests.Features.Shifts.Services;

public class ShiftSaveWorkflowTests
{
  [Fact]
  public async Task ApplyAsync_AddsUpdatesDeletesAndReturnsEveryAffectedWorkDate() {
    const string userId = "user-123";
    var shiftToUpdate = CreateStoredShift(
      userId,
      new DateTimeOffset(2026, 9, 13, 22, 0, 0, TimeSpan.Zero));
    var shiftToDelete = CreateStoredShift(
      userId,
      new DateTimeOffset(2026, 9, 15, 22, 0, 0, TimeSpan.Zero));
    var repository = new RecordingShiftRepository {
      ExistingShifts = [shiftToUpdate, shiftToDelete]
    };
    var service = new ShiftMutationService(repository);
    var command = new SaveShiftsCommand {
      UserId = userId,
      RangeStartDate = new DateOnly(2026, 9, 14),
      RangeEndDate = new DateOnly(2026, 9, 20),
      ShiftDtos = [
        CreateShiftDto(
          shiftToUpdate.Id,
          new DateTimeOffset(2026, 9, 14, 22, 0, 0, TimeSpan.Zero)),
        CreateShiftDto(
          null,
          new DateTimeOffset(2026, 9, 16, 22, 0, 0, TimeSpan.Zero))
      ],
      DeletedShiftIds = [shiftToDelete.Id]
    };

    var result = await service.ApplyAsync(command, CancellationToken.None);

    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Data);
    Assert.Equal(
      [
        new DateOnly(2026, 9, 14),
        new DateOnly(2026, 9, 15),
        new DateOnly(2026, 9, 16),
        new DateOnly(2026, 9, 17)
      ],
      result.Data.AffectedWorkDates.OrderBy(date => date));
    Assert.Equal(new DateTimeOffset(2026, 9, 14, 22, 0, 0, TimeSpan.Zero), shiftToUpdate.StartAt);
    Assert.Equal([shiftToDelete.Id], repository.RemovedIds);
    Assert.Equal(ShiftSource.Manual, Assert.Single(repository.AddedShifts).Source);
  }

  [Fact]
  public async Task ApplyAsync_MissingExistingIdFailsBeforeAnyMutation() {
    var missingId = Guid.NewGuid();
    var repository = new RecordingShiftRepository();
    var service = new ShiftMutationService(repository);
    var command = new SaveShiftsCommand {
      UserId = "user-123",
      RangeStartDate = new DateOnly(2026, 9, 14),
      RangeEndDate = new DateOnly(2026, 9, 20),
      ShiftDtos = [CreateShiftDto(
        missingId,
        new DateTimeOffset(2026, 9, 13, 22, 0, 0, TimeSpan.Zero))]
    };

    var result = await service.ApplyAsync(command, CancellationToken.None);

    Assert.False(result.IsSuccess);
    Assert.Contains(missingId.ToString(), Assert.Single(result.Errors));
    Assert.Empty(repository.AddedShifts);
    Assert.Empty(repository.RemovedIds);
  }

  [Fact]
  public async Task SaveShiftsAsync_FlushesShiftsThenRecalculatesAndCommits() {
    var events = new List<string>();
    var affectedDate = new DateOnly(2026, 9, 14);
    var shiftRepository = new RecordingShiftRepository { Events = events };
    var summaryRepository = new RecordingPaySummaryRepository(events);
    var unitOfWork = new RecordingUnitOfWork(events);
    var service = new ShiftService(
      shiftRepository,
      new SuccessfulShiftValidator(),
      new StubShiftMutationService(events, affectedDate),
      new RecordingPayCalculationService(events),
      summaryRepository,
      unitOfWork,
      null!);

    var result = await service.SaveShiftsAsync(CreateEmptyCommand(), CancellationToken.None);

    Assert.True(result.IsSuccess);
    Assert.Equal(
      [
        "transaction:start",
        "mutate",
        "save",
        "recalculate:2026-09-14",
        "save",
        "load:shifts",
        "load:summaries",
        "transaction:commit"
      ],
      events);
  }

  [Fact]
  public async Task SaveShiftsAsync_RecalculationFailureRollsBackTransaction() {
    var events = new List<string>();
    var service = new ShiftService(
      new RecordingShiftRepository { Events = events },
      new SuccessfulShiftValidator(),
      new StubShiftMutationService(events, new DateOnly(2026, 9, 14)),
      new RecordingPayCalculationService(events, throwOnRecalculation: true),
      new RecordingPaySummaryRepository(events),
      new RecordingUnitOfWork(events),
      null!);

    await Assert.ThrowsAsync<InvalidOperationException>(
      () => service.SaveShiftsAsync(CreateEmptyCommand(), CancellationToken.None));

    Assert.Equal(
      [
        "transaction:start",
        "mutate",
        "save",
        "recalculate:2026-09-14",
        "transaction:rollback"
      ],
      events);
  }

  private static SaveShiftsCommand CreateEmptyCommand() => new() {
    UserId = "user-123",
    RangeStartDate = new DateOnly(2026, 9, 14),
    RangeEndDate = new DateOnly(2026, 9, 20),
    ShiftDtos = []
  };

  private static UserShift CreateStoredShift(string userId, DateTimeOffset startAt) => new() {
    UserId = userId,
    StartAt = startAt,
    EndAt = startAt.AddHours(8),
    TimeZoneId = "Australia/Melbourne",
    EmploymentType = EmploymentType.FullTime,
    EntryType = ShiftEntryType.Worked
  };

  private static ShiftDto CreateShiftDto(Guid? id, DateTimeOffset startAt) => new() {
    Id = id,
    StartAt = startAt,
    EndAt = startAt.AddHours(8),
    TimeZoneId = "Australia/Melbourne",
    EmploymentType = EmploymentType.FullTime,
    EntryType = ShiftEntryType.Worked
  };

  private sealed class SuccessfulShiftValidator : IShiftValidator
  {
    public ValidationResult ValidateSaveShifts(
      List<ShiftDto> shifts,
      DateOnly cycleStartDate,
      DateOnly cycleEndDate) => ValidationResult.Success();
  }

  private sealed class StubShiftMutationService(
    List<string> events,
    DateOnly affectedDate) : IShiftMutationService
  {
    public Task<ServiceResult<ShiftMutationResult>> ApplyAsync(
      SaveShiftsCommand command,
      CancellationToken cancellationToken) {
      events.Add("mutate");
      return Task.FromResult(ServiceResult<ShiftMutationResult>.Success(
        new ShiftMutationResult([affectedDate])));
    }
  }

  private sealed class RecordingPayCalculationService(
    List<string> events,
    bool throwOnRecalculation = false) : IPayCalculationService
  {
    public Task RecalculateForDatesAsync(
      string userId,
      HashSet<DateOnly> affectedWorkDates,
      CancellationToken cancellationToken) {
      events.Add($"recalculate:{Assert.Single(affectedWorkDates):yyyy-MM-dd}");
      if (throwOnRecalculation) {
        throw new InvalidOperationException("Pay calculation failed.");
      }

      return Task.CompletedTask;
    }
  }

  private sealed class RecordingUnitOfWork(List<string> events) : IUnitOfWork
  {
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) {
      events.Add("save");
      return Task.FromResult(1);
    }

    public async Task<T> ExecuteInTransactionAsync<T>(
      Func<CancellationToken, Task<T>> operation,
      CancellationToken cancellationToken = default) {
      events.Add("transaction:start");
      try {
        var result = await operation(cancellationToken);
        events.Add("transaction:commit");
        return result;
      } catch {
        events.Add("transaction:rollback");
        throw;
      }
    }
  }

  private sealed class RecordingPaySummaryRepository(List<string> events) : IPaySummaryRepository
  {
    public Task<IEnumerable<UserDailyPaySummary>> GetByIdAndRangeAsync(
      string userId,
      DateOnly from,
      DateOnly to,
      CancellationToken cancellationToken = default) {
      events.Add("load:summaries");
      return Task.FromResult<IEnumerable<UserDailyPaySummary>>([]);
    }

    public Task<UserDailyPaySummary?> GetByDateAndUserAsync(string userId, DateOnly workDate, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task RemoveAsync(UserDailyPaySummary summary, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task AddAsync(UserDailyPaySummary summary, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task RemoveByUserIdAsync(string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<decimal> SumGrossPayByUserAndRangeAsync(string userId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default) => throw new NotSupportedException();
  }

  private sealed class RecordingShiftRepository : IShiftRepository
  {
    public List<string>? Events { get; init; }
    public IEnumerable<UserShift> ExistingShifts { get; init; } = [];
    public List<UserShift> AddedShifts { get; } = [];
    public List<Guid> RemovedIds { get; } = [];

    public Task<IEnumerable<UserShift>> GetByIdsForUserAsync(
      string userId,
      IEnumerable<Guid> shiftIds,
      CancellationToken cancellationToken = default) {
      var ids = shiftIds.ToHashSet();
      return Task.FromResult(ExistingShifts.Where(
        shift => shift.UserId == userId && ids.Contains(shift.Id)));
    }

    public Task AddRangeAsync(
      IEnumerable<UserShift> shifts,
      CancellationToken cancellationToken = default) {
      AddedShifts.AddRange(shifts);
      return Task.CompletedTask;
    }

    public Task RemoveByIdsForUserAsync(
      string userId,
      IEnumerable<Guid> shiftIds,
      CancellationToken cancellationToken = default) {
      RemovedIds.AddRange(shiftIds);
      return Task.CompletedTask;
    }

    public Task<IEnumerable<UserShift>> GetByIdsAndRangeAsync(
      string userId,
      DateOnly from,
      DateOnly to,
      CancellationToken cancellationToken = default) {
      Events?.Add("load:shifts");
      return Task.FromResult<IEnumerable<UserShift>>([]);
    }

    public Task AddAsync(UserShift userShift, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task UpdateAsync(UserShift userShift, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<UserShift?> GetByIdForUserAsync(string userId, Guid shiftId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<int> CountByUserAndRangeAsync(string userId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<IEnumerable<UserShift>> GetByDateAndUserAsync(string userId, DateOnly date, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task RemoveByIdForUserAsync(string userId, Guid shiftId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task RemoveShiftsByUserIdAsync(string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<PayCycleSetting?> GetPayCycleSettingByUserIdAsync(string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task UpdatePayCycleSettingsForUserAsync(string userId, PayCycleSetting payCycleSetting, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task RemovePayCycleSettingsByUserIdAsync(string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
  }
}
