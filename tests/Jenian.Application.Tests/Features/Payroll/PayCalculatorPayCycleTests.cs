using Jenian.Application.Features.Payroll;
using Jenian.Application.Features.Shifts.Dtos;
using Jenian.Domain.Entities;

namespace Jenian.Application.Tests.Features.Payroll;

public class PayCalculatorPayCycleTests
{
  private static PayCalculator CreateCalculator(DateTimeOffset utcNow) {
    return new PayCalculator(
      new StubPublicHolidayService(),
      new StubAwardRateService(),
      new FixedTimeProvider(utcNow));
  }

  [Theory]
  [InlineData("2026-09-07T02:00:00Z", "2026-09-07", "2026-09-20")]
  [InlineData("2026-09-13T02:00:00Z", "2026-09-07", "2026-09-20")]
  [InlineData("2026-09-20T02:00:00Z", "2026-09-07", "2026-09-20")]
  [InlineData("2026-09-21T02:00:00Z", "2026-09-21", "2026-10-04")]
  public void CalculatePayCycleDateRange_Fortnightly_HandlesInclusiveBoundaries(
    string utcNow,
    string expectedStart,
    string expectedEnd) {
    var calculator = CreateCalculator(DateTimeOffset.Parse(utcNow));

    var result = calculator.CalculatePayCycleDateRange(
      PayCycleType.Fortnightly,
      new DateOnly(2026, 9, 7));

    Assert.Equal(DateOnly.Parse(expectedStart), result.StartDate);
    Assert.Equal(DateOnly.Parse(expectedEnd), result.EndDate);
  }

  [Fact]
  public void CalculatePayCycleDateRange_Fortnightly_UsesMelbourneDateWhenUtcDateIsPreviousDay() {
    // 2026-09-21 00:30 in Melbourne is 2026-09-20 14:30 UTC.
    var calculator = CreateCalculator(new DateTimeOffset(2026, 9, 20, 14, 30, 0, TimeSpan.Zero));

    var result = calculator.CalculatePayCycleDateRange(
      PayCycleType.Fortnightly,
      new DateOnly(2026, 9, 7));

    Assert.Equal(new DateOnly(2026, 9, 21), result.StartDate);
    Assert.Equal(new DateOnly(2026, 10, 4), result.EndDate);
  }

  [Fact]
  public void CalculatePayCycleDateRange_Weekly_UsesStoredAnchor() {
    var calculator = CreateCalculator(new DateTimeOffset(2026, 9, 13, 2, 0, 0, TimeSpan.Zero));

    var result = calculator.CalculatePayCycleDateRange(
      PayCycleType.Weekly,
      new DateOnly(2026, 9, 7));

    Assert.Equal(new DateOnly(2026, 9, 7), result.StartDate);
    Assert.Equal(new DateOnly(2026, 9, 13), result.EndDate);
  }

  [Fact]
  public void CalculatePayCycleDateRange_Monthly_UsesMelbourneCalendarMonth() {
    var calculator = CreateCalculator(new DateTimeOffset(2026, 9, 30, 14, 30, 0, TimeSpan.Zero));

    var result = calculator.CalculatePayCycleDateRange(
      PayCycleType.Monthly,
      new DateOnly(2020, 1, 1));

    Assert.Equal(new DateOnly(2026, 10, 1), result.StartDate);
    Assert.Equal(new DateOnly(2026, 10, 31), result.EndDate);
  }

  private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
  {
    public override DateTimeOffset GetUtcNow() => utcNow;
  }

  private sealed class StubPublicHolidayService : IPublicHolidayService
  {
    public bool IsPublicHoliday(DateOnly date, string state) => false;
  }

  private sealed class StubAwardRateService : IAwardRateService
  {
    public decimal GetMultiplier(
      DateTimeOffset startTime,
      EmploymentType employmentType,
      bool isPublicHoliday,
      ShiftEntryType shiftEntryType) => throw new NotSupportedException();

    public List<TimeSegment> GetTimeSegmentsForShift(DateTimeOffset startTime, DateTimeOffset endTime) =>
      throw new NotSupportedException();

    public TotalPaySummary GetPaySegmentsForShift(
      ShiftDto shiftDto,
      bool isPublicHoliday,
      decimal baseHourlyRate) => throw new NotSupportedException();
  }
}
