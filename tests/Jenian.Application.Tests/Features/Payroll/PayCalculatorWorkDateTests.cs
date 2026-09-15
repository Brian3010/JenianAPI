using Jenian.Application.Common;
using Jenian.Application.Features.Payroll;
using Jenian.Application.Features.Shifts.Dtos;
using Jenian.Domain.Entities;

namespace Jenian.Application.Tests.Features.Payroll;

public class PayCalculatorWorkDateTests
{
  [Theory]
  [InlineData("2026-09-13T22:00:00Z", "2026-09-14T06:00:00Z")]
  [InlineData("2026-09-14T08:00:00+10:00", "2026-09-14T16:00:00+10:00")]
  public void CalculateDailyPay_UsesShiftTimezoneForWorkDate(
    string startAt,
    string endAt) {
    var publicHolidayService = new RecordingPublicHolidayService();
    var calculator = new PayCalculator(
      publicHolidayService,
      new StubAwardRateService(),
      TimeProvider.System);

    var result = calculator.CalculateDailyPay(
      [CreateShift(DateTimeOffset.Parse(startAt), DateTimeOffset.Parse(endAt))],
      "user-123");

    var expectedWorkDate = new DateOnly(2026, 9, 14);
    Assert.Equal(expectedWorkDate, result.WorkDate);
    Assert.Equal(expectedWorkDate, publicHolidayService.RequestedDate);
  }

  [Fact]
  public void CalculateDailyPay_AllowsEquivalentOffsetsOnTheSameLocalWorkDate() {
    var calculator = new PayCalculator(
      new RecordingPublicHolidayService(),
      new StubAwardRateService(),
      TimeProvider.System);

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

    Assert.Equal(new DateOnly(2026, 9, 14), result.WorkDate);
    Assert.Equal(480, result.TotalPayableMinutes);
  }

  [Fact]
  public void WorkDateRange_UsesLocalDateAcrossUtcBoundary() {
    var startAt = new DateTimeOffset(2026, 9, 13, 22, 0, 0, TimeSpan.Zero);
    var cycleStart = new DateOnly(2026, 9, 14);
    var cycleEnd = new DateOnly(2026, 9, 20);
    var (fromUtc, toUtc) = ShiftDateHelper.GetUtcCandidateRange(cycleStart, cycleEnd);

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
  public void CalculateDailyPay_AggregatesPaidBreakMinutes() {
    var calculator = new PayCalculator(
      new RecordingPublicHolidayService(),
      new StubAwardRateService(),
      TimeProvider.System);
    var firstShift = CreateShift(
      new DateTimeOffset(2026, 9, 14, 9, 0, 0, TimeSpan.FromHours(10)),
      new DateTimeOffset(2026, 9, 14, 13, 0, 0, TimeSpan.FromHours(10)));
    firstShift.PaidBreakMinutes = 10;
    var secondShift = CreateShift(
      new DateTimeOffset(2026, 9, 14, 14, 0, 0, TimeSpan.FromHours(10)),
      new DateTimeOffset(2026, 9, 14, 18, 0, 0, TimeSpan.FromHours(10)));
    secondShift.PaidBreakMinutes = 15;

    var result = calculator.CalculateDailyPay([firstShift, secondShift], "user-123");

    Assert.Equal(25, result.TotalPaidBreakMinutes);
  }

  private static ShiftDto CreateShift(DateTimeOffset startAt, DateTimeOffset endAt) =>
    new() {
      StartAt = startAt,
      EndAt = endAt,
      TimeZoneId = "Australia/Melbourne",
      EmploymentType = EmploymentType.FullTime,
      EntryType = ShiftEntryType.Worked
    };

  private sealed class RecordingPublicHolidayService : IPublicHolidayService
  {
    public DateOnly? RequestedDate { get; private set; }

    public bool IsPublicHoliday(DateOnly date, string state) {
      RequestedDate = date;
      return false;
    }
  }

  private sealed class StubAwardRateService : IAwardRateService
  {
    public decimal GetMultiplier(
      DateTimeOffset startTime,
      EmploymentType employmentType,
      bool isPublicHoliday,
      ShiftEntryType shiftEntryType) => throw new NotSupportedException();

    public List<TimeSegment> GetTimeSegmentsForShift(
      DateTimeOffset startTime,
      DateTimeOffset endTime) => throw new NotSupportedException();

    public TotalPaySummary GetPaySegmentsForShift(
      ShiftDto shiftDto,
      bool isPublicHoliday,
      decimal baseHourlyRate) => new(
        TotalPayableMinutes: (int)(shiftDto.EndAt - shiftDto.StartAt).TotalMinutes,
        TotalUnpaidBreakMinutes: shiftDto.UnpaidBreakMinutes,
        TotalOvertimeMinutes: 0,
        TotalEveningPenaltyMinutes: 0,
        GrossPay: 0m);
  }
}
