using Jenian.Application.Abstractions.Persistence;
using Jenian.Application.Features.Payroll;
using Jenian.Application.Features.Shifts.Commands;
using Jenian.Application.Features.Shifts.Services;
using Jenian.Domain.Entities;

namespace Jenian.Application.Tests.Features.Shifts.Services;

public class CurrentPayCycleShiftServiceTests
{
  [Fact]
  public async Task GetCurrentPayCycleShiftsAsync_LoadsInclusiveRangeAndMapsResults() {
    const string userId = "user-123";
    var shift = new UserShift {
      UserId = userId,
      StartAt = new DateTimeOffset(2026, 9, 8, 9, 0, 0, TimeSpan.FromHours(10)),
      EndAt = new DateTimeOffset(2026, 9, 8, 17, 0, 0, TimeSpan.FromHours(10)),
      PaidBreakMinutes = 15,
      UnpaidBreakMinutes = 30,
      EmploymentType = EmploymentType.FullTime
    };
    var summary = new UserDailyPaySummary {
      UserId = userId,
      WorkDate = new DateOnly(2026, 9, 8),
      TotalPayableMinutes = 450,
      TotalPaidBreakMinutes = 15,
      TotalUnpaidBreakMinutes = 30,
      TotalEveningPenaltyMinutes = 0,
      TotalOvertimeMinutes = 0,
      BaseRateUsed = 27.81m,
      GrossPay = 208.58m
    };
    var shiftRepository = new RecordingShiftRepository {
      Setting = new PayCycleSetting {
        UserId = userId,
        PayCycleType = PayCycleType.Fortnightly,
        AnchorStartDate = new DateOnly(2026, 9, 7)
      },
      Shifts = [shift]
    };
    var summaryRepository = new RecordingPaySummaryRepository { Summaries = [summary] };
    var service = new ShiftService(
      shiftRepository,
      null!,
      null!,
      null!,
      summaryRepository,
      null!,
      new StubPayCalculator(new DateOnly(2026, 9, 7), new DateOnly(2026, 9, 20)));

    var result = await service.GetCurrentPayCycleShiftsAsync(
      new GetCurrentPayCycleShiftsCommand { UserId = userId },
      CancellationToken.None);

    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Data);
    Assert.True(result.Data.HasPayCycleSettings);
    Assert.Equal("Fortnightly", result.Data.PayCycle);
    Assert.Equal(new DateOnly(2026, 9, 7), result.Data.StartDate);
    Assert.Equal(new DateOnly(2026, 9, 20), result.Data.EndDate);
    Assert.Equal((userId, new DateOnly(2026, 9, 7), new DateOnly(2026, 9, 20)), shiftRepository.RangeRequest);
    Assert.Equal((userId, new DateOnly(2026, 9, 7), new DateOnly(2026, 9, 20)), summaryRepository.RangeRequest);
    Assert.Equal(shift.Id, Assert.Single(result.Data.Shifts).Id);
    Assert.Equal(15, Assert.Single(result.Data.DailySummaries).TotalPaidBreakMinutes);
  }

  [Fact]
  public async Task GetCurrentPayCycleShiftsAsync_MissingSetting_ReturnsSetupStateWithoutRangeQueries() {
    var shiftRepository = new RecordingShiftRepository();
    var summaryRepository = new RecordingPaySummaryRepository();
    var service = new ShiftService(
      shiftRepository,
      null!,
      null!,
      null!,
      summaryRepository,
      null!,
      new StubPayCalculator(new DateOnly(2026, 9, 7), new DateOnly(2026, 9, 20)));

    var result = await service.GetCurrentPayCycleShiftsAsync(
      new GetCurrentPayCycleShiftsCommand { UserId = "user-123" },
      CancellationToken.None);

    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Data);
    Assert.False(result.Data.HasPayCycleSettings);
    Assert.Null(result.Data.PayCycle);
    Assert.Null(result.Data.StartDate);
    Assert.Null(result.Data.EndDate);
    Assert.Empty(result.Data.Shifts);
    Assert.Empty(result.Data.DailySummaries);
    Assert.Null(shiftRepository.RangeRequest);
    Assert.Null(summaryRepository.RangeRequest);
  }

  [Fact]
  public async Task GetCurrentPayCycleSummaryAsync_LoadsInclusiveAggregateRange() {
    const string userId = "user-123";
    var shiftRepository = new RecordingShiftRepository {
      Setting = new PayCycleSetting {
        UserId = userId,
        PayCycleType = PayCycleType.Fortnightly,
        AnchorStartDate = new DateOnly(2026, 9, 7)
      },
      ShiftCount = 3
    };
    var summaryRepository = new RecordingPaySummaryRepository { GrossPayTotal = 1245.50m };
    var service = new ShiftService(
      shiftRepository,
      null!,
      null!,
      null!,
      summaryRepository,
      null!,
      new StubPayCalculator(new DateOnly(2026, 9, 7), new DateOnly(2026, 9, 20)));

    var result = await service.GetCurrentPayCycleSummaryAsync(
      new GetCurrentPayCycleSummaryCommand { UserId = userId },
      CancellationToken.None);

    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Data);
    Assert.True(result.Data.HasPayCycleSettings);
    Assert.Equal("Fortnightly", result.Data.PayCycle);
    Assert.Equal(new DateOnly(2026, 9, 7), result.Data.StartDate);
    Assert.Equal(new DateOnly(2026, 9, 20), result.Data.EndDate);
    Assert.Equal(3, result.Data.ShiftCount);
    Assert.Equal(1245.50m, result.Data.EstimatedGrossPay);
    Assert.Equal((userId, new DateOnly(2026, 9, 7), new DateOnly(2026, 9, 20)), shiftRepository.CountRequest);
    Assert.Equal((userId, new DateOnly(2026, 9, 7), new DateOnly(2026, 9, 20)), summaryRepository.SumRequest);
  }

  [Fact]
  public async Task GetCurrentPayCycleSummaryAsync_MissingSetting_ReturnsSetupStateWithoutAggregateQueries() {
    var shiftRepository = new RecordingShiftRepository();
    var summaryRepository = new RecordingPaySummaryRepository();
    var service = new ShiftService(
      shiftRepository,
      null!,
      null!,
      null!,
      summaryRepository,
      null!,
      new StubPayCalculator(new DateOnly(2026, 9, 7), new DateOnly(2026, 9, 20)));

    var result = await service.GetCurrentPayCycleSummaryAsync(
      new GetCurrentPayCycleSummaryCommand { UserId = "user-123" },
      CancellationToken.None);

    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Data);
    Assert.False(result.Data.HasPayCycleSettings);
    Assert.Null(result.Data.PayCycle);
    Assert.Null(result.Data.StartDate);
    Assert.Null(result.Data.EndDate);
    Assert.Equal(0, result.Data.ShiftCount);
    Assert.Equal(0m, result.Data.EstimatedGrossPay);
    Assert.Null(shiftRepository.CountRequest);
    Assert.Null(summaryRepository.SumRequest);
  }

  private sealed class StubPayCalculator(DateOnly startDate, DateOnly endDate) : IPayCalculator
  {
    public Jenian.Application.Features.PaySummaries.Dtos.UserDailyPaySummaryDto CalculateDailyPay(
      List<Jenian.Application.Features.Shifts.Dtos.ShiftDto> shifts,
      string userId) => throw new NotSupportedException();

    public PayCycleDateRange CalculatePayCycleDateRange(PayCycleType userPayCycle, DateOnly anchorStartDate) =>
      new(startDate, endDate);
  }

  internal sealed class RecordingShiftRepository : IShiftRepository
  {
    public PayCycleSetting? Setting { get; init; }
    public IEnumerable<UserShift> Shifts { get; init; } = [];
    public int ShiftCount { get; init; }
    public (string UserId, DateOnly From, DateOnly To)? RangeRequest { get; private set; }
    public (string UserId, DateOnly From, DateOnly To)? CountRequest { get; private set; }

    public Task<PayCycleSetting?> GetPayCycleSettingByUserIdAsync(string userId, CancellationToken cancellationToken = default) =>
      Task.FromResult(Setting);

    public Task<IEnumerable<UserShift>> GetByIdsAndRangeAsync(string userId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default) {
      RangeRequest = (userId, from, to);
      return Task.FromResult(Shifts);
    }

    public Task<int> CountByUserAndRangeAsync(string userId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default) {
      CountRequest = (userId, from, to);
      return Task.FromResult(ShiftCount);
    }

    public Task AddAsync(UserShift userShift, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task UpdateAsync(UserShift userShift, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task AddRangeAsync(IEnumerable<UserShift> shifts, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<UserShift?> GetByIdForUserAsync(string userId, Guid shiftId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<IEnumerable<UserShift>> GetByIdsForUserAsync(string userId, IEnumerable<Guid> shiftIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<IEnumerable<UserShift>> GetByDateAndUserAsync(string userId, DateOnly date, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task RemoveByIdForUserAsync(string userId, Guid shiftId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task RemoveByIdsForUserAsync(string userId, IEnumerable<Guid> shiftIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task RemoveShiftsByUserIdAsync(string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task UpdatePayCycleSettingsForUserAsync(string userId, PayCycleSetting payCycleSetting, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task RemovePayCycleSettingsByUserIdAsync(string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
  }

  internal sealed class RecordingPaySummaryRepository : IPaySummaryRepository
  {
    public IEnumerable<UserDailyPaySummary> Summaries { get; init; } = [];
    public decimal GrossPayTotal { get; init; }
    public (string UserId, DateOnly From, DateOnly To)? RangeRequest { get; private set; }
    public (string UserId, DateOnly From, DateOnly To)? SumRequest { get; private set; }

    public Task<IEnumerable<UserDailyPaySummary>> GetByIdAndRangeAsync(string userId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default) {
      RangeRequest = (userId, from, to);
      return Task.FromResult(Summaries);
    }

    public Task<decimal> SumGrossPayByUserAndRangeAsync(string userId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default) {
      SumRequest = (userId, from, to);
      return Task.FromResult(GrossPayTotal);
    }

    public Task<UserDailyPaySummary?> GetByDateAndUserAsync(string userId, DateOnly workDate, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task RemoveAsync(UserDailyPaySummary summary, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task AddAsync(UserDailyPaySummary summary, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task RemoveByUserIdAsync(string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
  }
}
