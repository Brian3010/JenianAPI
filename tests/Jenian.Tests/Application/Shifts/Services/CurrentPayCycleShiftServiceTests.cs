using Moq;
using Jenian.Tests.TestSupport;
using Jenian.Application.Abstractions.Persistence;
using Jenian.Application.Features.Payroll;
using Jenian.Application.Features.Shifts.Commands;
using Jenian.Application.Features.Shifts.Services;
using Jenian.Domain.Entities;

namespace Jenian.Tests.Application.Shifts.Services
{
  public class CurrentPayCycleShiftServiceTests
  {
    private static readonly DateOnly Start = new(2026, 9, 7);
    private static readonly DateOnly End = new(2026, 9, 20);

    [Fact]
    public async Task GetCurrentPayCycleShiftsAsync_ConfiguredCycle_QueriesInclusiveRangeAndMapsResults() {
      // Arrange
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
      var context = new ShiftServiceContext();
      context.Shifts.Setup(x => x.GetPayCycleSettingByUserIdAsync(userId, CancellationToken.None))
        .ReturnsAsync(new PayCycleSetting {
          UserId = userId,
          PayCycleType = PayCycleType.Fortnightly,
          AnchorStartDate = new DateOnly(2026, 9, 7)
        });
      context.Calculator.Setup(x => x.CalculatePayCycleDateRange(PayCycleType.Fortnightly, Start)).Returns(new PayCycleDateRange(Start, End));
      context.Shifts.Setup(x => x.GetByIdsAndRangeAsync(userId, Start, End, CancellationToken.None)).ReturnsAsync([shift]);
      context.Summaries.Setup(x => x.GetByIdAndRangeAsync(userId, Start, End, CancellationToken.None)).ReturnsAsync([summary]);
      var service = context.CreateService();

      // Act

      var result = await service.GetCurrentPayCycleShiftsAsync(
        new GetCurrentPayCycleShiftsCommand { UserId = userId },
        CancellationToken.None);

      // Assert
      Assert.True(result.IsSuccess);
      Assert.NotNull(result.Data);
      Assert.True(result.Data.HasPayCycleSettings);
      Assert.Equal("Fortnightly", result.Data.PayCycle);
      Assert.Equal(new DateOnly(2026, 9, 7), result.Data.StartDate);
      Assert.Equal(new DateOnly(2026, 9, 20), result.Data.EndDate);
      context.Shifts.Verify(x => x.GetByIdsAndRangeAsync(userId, Start, End, CancellationToken.None), Times.Once);
      context.Summaries.Verify(x => x.GetByIdAndRangeAsync(userId, Start, End, CancellationToken.None), Times.Once);
      var mapped = Assert.Single(result.Data.Shifts);
      Assert.Equal((shift.Id, shift.StartAt, shift.EndAt, shift.TimeZoneId, shift.PaidBreakMinutes,
        shift.UnpaidBreakMinutes, shift.EntryType, shift.EmploymentType, shift.Source),
        (mapped.Id, mapped.StartAt, mapped.EndAt, mapped.TimeZoneId, mapped.PaidBreakMinutes,
        mapped.UnpaidBreakMinutes, mapped.EntryType, mapped.EmploymentType, mapped.Source));
      var daily = Assert.Single(result.Data.DailySummaries);
      Assert.Equal((summary.UserId, summary.WorkDate, summary.BaseRateUsed, summary.GrossPay,
        summary.TotalPaidBreakMinutes, summary.TotalUnpaidBreakMinutes, summary.TotalPayableMinutes,
        summary.TotalEveningPenaltyMinutes, summary.TotalOvertimeMinutes),
        (daily.UserId, daily.WorkDate, daily.BaseRateUsed, daily.GrossPay, daily.TotalPaidBreakMinutes,
        daily.TotalUnpaidBreakMinutes, daily.TotalPayableMinutes, daily.TotalEveningPenaltyMinutes, daily.TotalOvertimeMinutes));
    }

    [Fact]
    public async Task GetCurrentPayCycleShiftsAsync_MissingSetting_ReturnsSetupStateWithoutRangeQueries() {
      // Arrange
      const string userId = "user-123";
      var context = new ShiftServiceContext();
      context.Shifts.Setup(x => x.GetPayCycleSettingByUserIdAsync(userId, CancellationToken.None)).ReturnsAsync((PayCycleSetting?)null);

      var service = context.CreateService();

      // Act

      var result = await service.GetCurrentPayCycleShiftsAsync(
        new GetCurrentPayCycleShiftsCommand { UserId = "user-123" },
        CancellationToken.None);

      // Assert
      Assert.True(result.IsSuccess);
      Assert.NotNull(result.Data);
      Assert.False(result.Data.HasPayCycleSettings);
      Assert.Null(result.Data.PayCycle);
      Assert.Null(result.Data.StartDate);
      Assert.Null(result.Data.EndDate);
      Assert.Empty(result.Data.Shifts);
      Assert.Empty(result.Data.DailySummaries);
      context.Shifts.Verify(x => x.GetByIdsAndRangeAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Never);
      context.Summaries.Verify(x => x.GetByIdAndRangeAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetCurrentPayCycleSummaryAsync_ConfiguredCycle_QueriesInclusiveAggregateRange() {
      // Arrange
      const string userId = "user-123";
      var context = new ShiftServiceContext();
      context.Shifts.Setup(x => x.GetPayCycleSettingByUserIdAsync(userId, CancellationToken.None))
        .ReturnsAsync(new PayCycleSetting {
          UserId = userId,
          PayCycleType = PayCycleType.Fortnightly,
          AnchorStartDate = new DateOnly(2026, 9, 7)
        });
      context.Calculator.Setup(x => x.CalculatePayCycleDateRange(PayCycleType.Fortnightly, Start)).Returns(new PayCycleDateRange(Start, End));
      context.Shifts.Setup(x => x.CountByUserAndRangeAsync(userId, Start, End, CancellationToken.None)).ReturnsAsync(3);
      context.Summaries.Setup(x => x.SumGrossPayByUserAndRangeAsync(userId, Start, End, CancellationToken.None)).ReturnsAsync(1245.50m);
      var service = context.CreateService();

      // Act

      var result = await service.GetCurrentPayCycleSummaryAsync(
        new GetCurrentPayCycleSummaryCommand { UserId = userId },
        CancellationToken.None);

      // Assert
      Assert.True(result.IsSuccess);
      Assert.NotNull(result.Data);
      Assert.True(result.Data.HasPayCycleSettings);
      Assert.Equal("Fortnightly", result.Data.PayCycle);
      Assert.Equal(new DateOnly(2026, 9, 7), result.Data.StartDate);
      Assert.Equal(new DateOnly(2026, 9, 20), result.Data.EndDate);
      Assert.Equal(3, result.Data.ShiftCount);
      Assert.Equal(1245.50m, result.Data.EstimatedGrossPay);
      context.Shifts.Verify(x => x.CountByUserAndRangeAsync(userId, Start, End, CancellationToken.None), Times.Once);
      context.Summaries.Verify(x => x.SumGrossPayByUserAndRangeAsync(userId, Start, End, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task GetCurrentPayCycleSummaryAsync_MissingSetting_ReturnsSetupStateWithoutAggregateQueries() {
      // Arrange
      const string userId = "user-123";
      var context = new ShiftServiceContext();
      context.Shifts.Setup(x => x.GetPayCycleSettingByUserIdAsync(userId, CancellationToken.None)).ReturnsAsync((PayCycleSetting?)null);

      var service = context.CreateService();

      // Act

      var result = await service.GetCurrentPayCycleSummaryAsync(
        new GetCurrentPayCycleSummaryCommand { UserId = "user-123" },
        CancellationToken.None);

      // Assert
      Assert.True(result.IsSuccess);
      Assert.NotNull(result.Data);
      Assert.False(result.Data.HasPayCycleSettings);
      Assert.Null(result.Data.PayCycle);
      Assert.Null(result.Data.StartDate);
      Assert.Null(result.Data.EndDate);
      Assert.Equal(0, result.Data.ShiftCount);
      Assert.Equal(0m, result.Data.EstimatedGrossPay);
      context.Shifts.Verify(x => x.CountByUserAndRangeAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Never);
      context.Summaries.Verify(x => x.SumGrossPayByUserAndRangeAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetCurrentPayCycle_ConfiguredWithoutShifts_ReturnsEmptyResultsAndZeroTotals() {
      // Arrange
      var context = new ShiftServiceContext();
      const string userId = "user-123";
      context.Shifts.Setup(x => x.GetPayCycleSettingByUserIdAsync(userId, CancellationToken.None))
        .ReturnsAsync(new PayCycleSetting { UserId = userId, PayCycleType = PayCycleType.Fortnightly, AnchorStartDate = Start });
      context.Calculator.Setup(x => x.CalculatePayCycleDateRange(PayCycleType.Fortnightly, Start)).Returns(new PayCycleDateRange(Start, End));
      context.Shifts.Setup(x => x.GetByIdsAndRangeAsync(userId, Start, End, CancellationToken.None)).ReturnsAsync([]);
      context.Summaries.Setup(x => x.GetByIdAndRangeAsync(userId, Start, End, CancellationToken.None)).ReturnsAsync([]);
      context.Shifts.Setup(x => x.CountByUserAndRangeAsync(userId, Start, End, CancellationToken.None)).ReturnsAsync(0);
      context.Summaries.Setup(x => x.SumGrossPayByUserAndRangeAsync(userId, Start, End, CancellationToken.None)).ReturnsAsync(0m);
      var service = context.CreateService();

      // Act

      var shifts = await service.GetCurrentPayCycleShiftsAsync(new() { UserId = userId }, CancellationToken.None);
      var summary = await service.GetCurrentPayCycleSummaryAsync(new() { UserId = userId }, CancellationToken.None);

      // Assert
      Assert.True(shifts.IsSuccess);
      Assert.NotNull(shifts.Data);
      Assert.True(shifts.Data.HasPayCycleSettings);
      Assert.Empty(shifts.Data.Shifts);
      Assert.Empty(shifts.Data.DailySummaries);
      Assert.True(summary.IsSuccess);
      Assert.NotNull(summary.Data);
      Assert.True(summary.Data.HasPayCycleSettings);
      Assert.Equal(0, summary.Data.ShiftCount);
      Assert.Equal(0m, summary.Data.EstimatedGrossPay);
    }
  }
}
