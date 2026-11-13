using Moq;
using Jenian.Tests.TestSupport;
using Jenian.Application.Features.Payroll;
using Jenian.Application.Features.Shifts.Dtos;
using Jenian.Domain.Entities;

namespace Jenian.Tests.Application.Payroll
{
  public class PayCalculatorPayCycleTests
  {
    [Fact]
    public void CalculatePayCycleDateRange_WeeklyAtRollover_ReturnsNextWeek() {
      // Arrange
      var calculator = CreateCalculator(DateTimeOffset.Parse("2026-09-13T14:30:00Z"));
      // Act
      var result = calculator.CalculatePayCycleDateRange(PayCycleType.Weekly, new DateOnly(2026, 9, 7));
      Assert.Equal(new DateOnly(2026, 9, 14), result.StartDate);
      Assert.Equal(new DateOnly(2026, 9, 20), result.EndDate);
    }
    private static PayCalculator CreateCalculator(DateTimeOffset utcNow) {
      return new PayCalculator(
        new Mock<IPublicHolidayService>(MockBehavior.Strict).Object,
        new Mock<IAwardRateService>(MockBehavior.Strict).Object,
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
      // Arrange
      var calculator = CreateCalculator(DateTimeOffset.Parse(utcNow));

      // Act

      var result = calculator.CalculatePayCycleDateRange(
        PayCycleType.Fortnightly,
        new DateOnly(2026, 9, 7));

      // Assert
      Assert.Equal(DateOnly.Parse(expectedStart), result.StartDate);
      Assert.Equal(DateOnly.Parse(expectedEnd), result.EndDate);
    }

    [Fact]
    public void CalculatePayCycleDateRange_Fortnightly_UsesMelbourneDateWhenUtcDateIsPreviousDay() {
      // Arrange
      // 2026-09-21 00:30 in Melbourne is 2026-09-20 14:30 UTC.
      var calculator = CreateCalculator(new DateTimeOffset(2026, 9, 20, 14, 30, 0, TimeSpan.Zero));

      // Act

      var result = calculator.CalculatePayCycleDateRange(
        PayCycleType.Fortnightly,
        new DateOnly(2026, 9, 7));

      // Assert
      Assert.Equal(new DateOnly(2026, 9, 21), result.StartDate);
      Assert.Equal(new DateOnly(2026, 10, 4), result.EndDate);
    }

    [Fact]
    public void CalculatePayCycleDateRange_Weekly_UsesStoredAnchor() {
      // Arrange
      var calculator = CreateCalculator(new DateTimeOffset(2026, 9, 13, 2, 0, 0, TimeSpan.Zero));

      // Act

      var result = calculator.CalculatePayCycleDateRange(
        PayCycleType.Weekly,
        new DateOnly(2026, 9, 7));

      // Assert
      Assert.Equal(new DateOnly(2026, 9, 7), result.StartDate);
      Assert.Equal(new DateOnly(2026, 9, 13), result.EndDate);
    }

    [Fact]
    public void CalculatePayCycleDateRange_Monthly_UsesMelbourneCalendarMonth() {
      // Arrange
      var calculator = CreateCalculator(new DateTimeOffset(2026, 9, 30, 14, 30, 0, TimeSpan.Zero));

      // Act

      var result = calculator.CalculatePayCycleDateRange(
        PayCycleType.Monthly,
        new DateOnly(2020, 1, 1));

      // Assert
      Assert.Equal(new DateOnly(2026, 10, 1), result.StartDate);
      Assert.Equal(new DateOnly(2026, 10, 31), result.EndDate);
    }

  }
}
