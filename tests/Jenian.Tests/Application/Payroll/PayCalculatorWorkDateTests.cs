using Moq;
using Jenian.Application.Common;
using Jenian.Application.Features.Payroll;
using Jenian.Application.Features.Shifts.Dtos;
using Jenian.Domain.Entities;

namespace Jenian.Tests.Application.Payroll
{
  public class PayCalculatorWorkDateTests
  {
    [Theory]
    [InlineData("2026-09-13T22:00:00Z", "2026-09-14T06:00:00Z")]
    [InlineData("2026-09-14T08:00:00+10:00", "2026-09-14T16:00:00+10:00")]
    public void CalculateDailyPay_UtcOrLocalOffset_UsesShiftTimezoneForWorkDate(
      string startAt,
      string endAt) {
      // Arrange
      var publicHolidayService = CreateHolidayService();
      var calculator = new PayCalculator(
        publicHolidayService.Object,
        CreateAwardService(),
        TimeProvider.System);

      // Act

      var result = calculator.CalculateDailyPay(
        [CreateShift(DateTimeOffset.Parse(startAt), DateTimeOffset.Parse(endAt))],
        "user-123");

      var expectedWorkDate = new DateOnly(2026, 9, 14);
      Assert.Equal(expectedWorkDate, result.WorkDate);
      publicHolidayService.Verify(x => x.IsPublicHoliday(expectedWorkDate, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void CalculateDailyPay_EquivalentOffsets_AggregatesSameLocalWorkDate() {
      // Arrange
      var calculator = new PayCalculator(
        CreateHolidayService().Object,
        CreateAwardService(),
        TimeProvider.System);

      // Act

      var result = calculator.CalculateDailyPay(
        [
          CreateShift(
            new DateTimeOffset(2026, 9, 13, 22, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 14, 2, 0, 0, TimeSpan.Zero)),
          CreateShift(
            new DateTimeOffset(2026, 9, 14, 13, 0, 0, TimeSpan.FromHours(10)),
            new DateTimeOffset(2026, 9, 14, 17, 0, 0, TimeSpan.FromHours(10)))
        ],
        "user-123");

      // Assert
      Assert.Equal(new DateOnly(2026, 9, 14), result.WorkDate);
      Assert.Equal(480, result.TotalPayableMinutes);
    }

    [Fact]
    public void IsWorkDateInRange_UtcBoundary_UsesLocalDate() {
      // Arrange
      var startAt = new DateTimeOffset(2026, 9, 13, 22, 0, 0, TimeSpan.Zero);
      var cycleStart = new DateOnly(2026, 9, 14);
      var cycleEnd = new DateOnly(2026, 9, 20);
      var (fromUtc, toUtc) = ShiftDateHelper.GetUtcCandidateRange(cycleStart, cycleEnd);

      // Assert
      Assert.True(startAt >= fromUtc && startAt < toUtc);
      Assert.True(ShiftDateHelper.IsWorkDateInRange(
        startAt,
        "Australia/Melbourne",
        cycleStart,
        cycleEnd));
      Assert.False(ShiftDateHelper.IsWorkDateInRange(
        startAt,
        "Australia/Melbourne",
        cycleStart.AddDays(-7),
        cycleStart.AddDays(-1)));
    }

    [Fact]
    public void CalculateDailyPay_MultipleShifts_AggregatesPaidBreakMinutes() {
      // Arrange
      var calculator = new PayCalculator(
        CreateHolidayService().Object,
        CreateAwardService(),
        TimeProvider.System);
      var firstShift = CreateShift(
        new DateTimeOffset(2026, 9, 14, 9, 0, 0, TimeSpan.FromHours(10)),
        new DateTimeOffset(2026, 9, 14, 13, 0, 0, TimeSpan.FromHours(10)));
      firstShift.PaidBreakMinutes = 10;
      var secondShift = CreateShift(
        new DateTimeOffset(2026, 9, 14, 14, 0, 0, TimeSpan.FromHours(10)),
        new DateTimeOffset(2026, 9, 14, 18, 0, 0, TimeSpan.FromHours(10)));
      secondShift.PaidBreakMinutes = 15;

      // Act

      var result = calculator.CalculateDailyPay([firstShift, secondShift], "user-123");

      // Assert
      Assert.Equal(25, result.TotalPaidBreakMinutes);
    }

    [Fact]
    public void WorkDateRange_AfterDaylightSavingTransition_UsesMelbourneDate() {
      // Arrange
      Assert.Equal(new DateOnly(2026, 10, 5), ShiftDateHelper.GetWorkDate(
        DateTimeOffset.Parse("2026-10-04T13:30:00Z"), "Australia/Melbourne"));
    }
    private static ShiftDto CreateShift(DateTimeOffset startAt, DateTimeOffset endAt) =>
      new() {
        StartAt = startAt,
        EndAt = endAt,
        TimeZoneId = "Australia/Melbourne",
        EmploymentType = EmploymentType.FullTime,
        EntryType = ShiftEntryType.Worked
      };

    private static Mock<IPublicHolidayService> CreateHolidayService() {
      var mock = new Mock<IPublicHolidayService>(MockBehavior.Strict);
      mock.Setup(x => x.IsPublicHoliday(It.IsAny<DateOnly>(), It.IsAny<string>())).Returns(false);
      return mock;
    }
    private static IAwardRateService CreateAwardService() {
      var mock = new Mock<IAwardRateService>(MockBehavior.Strict);
      mock.Setup(x => x.GetPaySegmentsForShift(It.IsAny<ShiftDto>(), It.IsAny<bool>(), It.IsAny<decimal>()))
        .Returns((ShiftDto shift, bool holiday, decimal rate) => new TotalPaySummary(
          (int)(shift.EndAt - shift.StartAt).TotalMinutes, shift.UnpaidBreakMinutes, 0, 0, 0m));
      return mock.Object;
    }
  }
}
