using Jenian.Application.Features.Payroll;
using Jenian.Application.Features.Shifts.Dtos;
using Jenian.Domain.Entities;

namespace Jenian.Application.Tests.Features.Shifts.Services;

public class PharmacyAwardRateServiceTests
{
  private readonly PharmacyAwardRateService _service;
  // Melbourne, Australia timezone
  private static readonly TimeZoneInfo MelbourneTimeZone = TimeZoneInfo.FindSystemTimeZoneById("AUS Eastern Standard Time");

  public PharmacyAwardRateServiceTests() {
    _service = new PharmacyAwardRateService();
  }

  /// <summary>
  /// Helper method to create DateTimeOffset in Melbourne timezone
  /// </summary>
  private static DateTimeOffset CreateMelbourneTime(int year, int month, int day, int hour, int minute) {
    var dateTime = new DateTime(year, month, day, hour, minute, 0);
    var offset = MelbourneTimeZone.GetUtcOffset(dateTime);
    return new DateTimeOffset(dateTime, offset);
  }

  #region GetMultiplier Tests

  // Weekday Tests - Permanent Employees (FullTime & PartTime have same rates)
  [Theory]
  [InlineData(2026, 5, 19, 7, 30, EmploymentType.FullTime, false, ShiftEntryType.Worked, 1.50)] // Monday base - FullTime
  [InlineData(2026, 5, 19, 7, 30, EmploymentType.PartTime, false, ShiftEntryType.Worked, 1.50)] // Monday base - PartTime
  [InlineData(2026, 5, 19, 9, 0, EmploymentType.FullTime, false, ShiftEntryType.Worked, 1.0)] // Monday base - FullTime
  [InlineData(2026, 5, 19, 9, 0, EmploymentType.PartTime, false, ShiftEntryType.Worked, 1.0)] // Monday base - PartTime
  [InlineData(2026, 5, 19, 19, 30, EmploymentType.FullTime, false, ShiftEntryType.Worked, 1.25)] // Monday evening - FullTime
  [InlineData(2026, 5, 19, 19, 30, EmploymentType.PartTime, false, ShiftEntryType.Worked, 1.25)] // Monday evening - PartTime
  [InlineData(2026, 5, 19, 21, 30, EmploymentType.FullTime, false, ShiftEntryType.Worked, 1.50)] // Monday late - FullTime
  [InlineData(2026, 5, 19, 21, 30, EmploymentType.PartTime, false, ShiftEntryType.Worked, 1.50)] // Monday late - PartTime
  [InlineData(2026, 5, 19, 6, 0, EmploymentType.FullTime, false, ShiftEntryType.Worked, 1.50)] // Monday early - FullTime
  [InlineData(2026, 5, 19, 6, 0, EmploymentType.PartTime, false, ShiftEntryType.Worked, 1.50)] // Monday early - PartTime
  public void GetMultiplier_Weekday_PermanentEmployees_ReturnsCorrectMultiplier(
      int year, int month, int day, int hour, int minute,
      EmploymentType employmentType,
      bool isPublicHoliday,
      ShiftEntryType shiftEntryType,
      decimal expectedMultiplier) {
    // Arrange
    var startTime = CreateMelbourneTime(year, month, day, hour, minute);

    // Act
    var result = _service.GetMultiplier(startTime, employmentType, isPublicHoliday, shiftEntryType);

    // Assert
    Assert.Equal(expectedMultiplier, result);
  }

  // Weekday Tests - Casual Employees (different rates)
  [Theory]
  [InlineData(2026, 5, 19, 7, 30, EmploymentType.Casual, false, ShiftEntryType.Worked, 1.75)] // Monday base casual
  [InlineData(2026, 5, 19, 9, 0, EmploymentType.Casual, false, ShiftEntryType.Worked, 1.25)] // Monday base casual
  [InlineData(2026, 5, 19, 19, 30, EmploymentType.Casual, false, ShiftEntryType.Worked, 1.50)] // Monday evening casual
  [InlineData(2026, 5, 19, 21, 30, EmploymentType.Casual, false, ShiftEntryType.Worked, 1.75)] // Monday late casual
  [InlineData(2026, 5, 19, 6, 0, EmploymentType.Casual, false, ShiftEntryType.Worked, 1.75)] // Monday early casual
  public void GetMultiplier_Weekday_Casual_ReturnsCorrectMultiplier(
      int year, int month, int day, int hour, int minute,
      EmploymentType employmentType,
      bool isPublicHoliday,
      ShiftEntryType shiftEntryType,
      decimal expectedMultiplier) {
    // Arrange
    var startTime = CreateMelbourneTime(year, month, day, hour, minute);

    // Act
    var result = _service.GetMultiplier(startTime, employmentType, isPublicHoliday, shiftEntryType);

    // Assert
    Assert.Equal(expectedMultiplier, result);
  }

  // Saturday Tests - Permanent Employees (FullTime & PartTime have same rates)
  [Theory]
  [InlineData(2026, 5, 23, 7, 30, EmploymentType.FullTime, false, ShiftEntryType.Worked, 2.00)] // Saturday early - FullTime
  [InlineData(2026, 5, 23, 7, 30, EmploymentType.PartTime, false, ShiftEntryType.Worked, 2.00)] // Saturday early - PartTime
  [InlineData(2026, 5, 23, 9, 0, EmploymentType.FullTime, false, ShiftEntryType.Worked, 1.25)] // Saturday regular - FullTime
  [InlineData(2026, 5, 23, 9, 0, EmploymentType.PartTime, false, ShiftEntryType.Worked, 1.25)] // Saturday regular - PartTime
  [InlineData(2026, 5, 23, 18, 30, EmploymentType.FullTime, false, ShiftEntryType.Worked, 1.50)] // Saturday evening - FullTime
  [InlineData(2026, 5, 23, 18, 30, EmploymentType.PartTime, false, ShiftEntryType.Worked, 1.50)] // Saturday evening - PartTime
  [InlineData(2026, 5, 23, 21, 30, EmploymentType.FullTime, false, ShiftEntryType.Worked, 1.75)] // Saturday late - FullTime
  [InlineData(2026, 5, 23, 21, 30, EmploymentType.PartTime, false, ShiftEntryType.Worked, 1.75)] // Saturday late - PartTime
  public void GetMultiplier_Saturday_PermanentEmployees_ReturnsCorrectMultiplier(
      int year, int month, int day, int hour, int minute,
      EmploymentType employmentType,
      bool isPublicHoliday,
      ShiftEntryType shiftEntryType,
      decimal expectedMultiplier) {
    // Arrange
    var startTime = CreateMelbourneTime(year, month, day, hour, minute);

    // Act
    var result = _service.GetMultiplier(startTime, employmentType, isPublicHoliday, shiftEntryType);

    // Assert
    Assert.Equal(expectedMultiplier, result);
  }

  // Saturday Tests - Casual Employees (different rates)
  [Theory]
  [InlineData(2026, 5, 23, 7, 30, EmploymentType.Casual, false, ShiftEntryType.Worked, 2.25)] // Saturday early casual
  [InlineData(2026, 5, 23, 9, 0, EmploymentType.Casual, false, ShiftEntryType.Worked, 1.50)] // Saturday regular casual
  [InlineData(2026, 5, 23, 18, 30, EmploymentType.Casual, false, ShiftEntryType.Worked, 1.75)] // Saturday evening casual
  [InlineData(2026, 5, 23, 21, 30, EmploymentType.Casual, false, ShiftEntryType.Worked, 2.00)] // Saturday late casual
  public void GetMultiplier_Saturday_Casual_ReturnsCorrectMultiplier(
      int year, int month, int day, int hour, int minute,
      EmploymentType employmentType,
      bool isPublicHoliday,
      ShiftEntryType shiftEntryType,
      decimal expectedMultiplier) {
    // Arrange
    var startTime = CreateMelbourneTime(year, month, day, hour, minute);

    // Act
    var result = _service.GetMultiplier(startTime, employmentType, isPublicHoliday, shiftEntryType);

    // Assert
    Assert.Equal(expectedMultiplier, result);
  }

  // Sunday Tests - Permanent Employees (FullTime & PartTime have same rates)
  [Theory]
  [InlineData(2026, 5, 24, 6, 0, EmploymentType.FullTime, false, ShiftEntryType.Worked, 2.00)] // Sunday early - FullTime
  [InlineData(2026, 5, 24, 6, 0, EmploymentType.PartTime, false, ShiftEntryType.Worked, 2.00)] // Sunday early - PartTime
  [InlineData(2026, 5, 24, 12, 0, EmploymentType.FullTime, false, ShiftEntryType.Worked, 1.50)] // Sunday regular - FullTime
  [InlineData(2026, 5, 24, 12, 0, EmploymentType.PartTime, false, ShiftEntryType.Worked, 1.50)] // Sunday regular - PartTime
  [InlineData(2026, 5, 24, 22, 0, EmploymentType.FullTime, false, ShiftEntryType.Worked, 2.00)] // Sunday late - FullTime
  [InlineData(2026, 5, 24, 22, 0, EmploymentType.PartTime, false, ShiftEntryType.Worked, 2.00)] // Sunday late - PartTime
  public void GetMultiplier_Sunday_PermanentEmployees_ReturnsCorrectMultiplier(
      int year, int month, int day, int hour, int minute,
      EmploymentType employmentType,
      bool isPublicHoliday,
      ShiftEntryType shiftEntryType,
      decimal expectedMultiplier) {
    // Arrange
    var startTime = CreateMelbourneTime(year, month, day, hour, minute);

    // Act
    var result = _service.GetMultiplier(startTime, employmentType, isPublicHoliday, shiftEntryType);

    // Assert
    Assert.Equal(expectedMultiplier, result);
  }

  // Sunday Tests - Casual Employees (different rates)
  [Theory]
  [InlineData(2026, 5, 24, 6, 0, EmploymentType.Casual, false, ShiftEntryType.Worked, 2.25)] // Sunday early casual
  [InlineData(2026, 5, 24, 12, 0, EmploymentType.Casual, false, ShiftEntryType.Worked, 1.75)] // Sunday regular casual
  [InlineData(2026, 5, 24, 22, 0, EmploymentType.Casual, false, ShiftEntryType.Worked, 2.25)] // Sunday late casual
  public void GetMultiplier_Sunday_Casual_ReturnsCorrectMultiplier(
      int year, int month, int day, int hour, int minute,
      EmploymentType employmentType,
      bool isPublicHoliday,
      ShiftEntryType shiftEntryType,
      decimal expectedMultiplier) {
    // Arrange
    var startTime = CreateMelbourneTime(year, month, day, hour, minute);

    // Act
    var result = _service.GetMultiplier(startTime, employmentType, isPublicHoliday, shiftEntryType);

    // Assert
    Assert.Equal(expectedMultiplier, result);
  }

  // Public Holiday Tests - Both permanent and casual
  [Theory]
  [InlineData(2026, 5, 19, 9, 0, EmploymentType.FullTime, true, ShiftEntryType.Worked, 2.25)] // Public holiday full-time
  [InlineData(2026, 5, 19, 9, 0, EmploymentType.PartTime, true, ShiftEntryType.Worked, 2.25)] // Public holiday part-time
  [InlineData(2026, 5, 19, 9, 0, EmploymentType.Casual, true, ShiftEntryType.Worked, 2.50)] // Public holiday casual
  [InlineData(2026, 5, 23, 9, 0, EmploymentType.FullTime, true, ShiftEntryType.Worked, 2.25)] // Public holiday Saturday full-time
  [InlineData(2026, 5, 23, 9, 0, EmploymentType.PartTime, true, ShiftEntryType.Worked, 2.25)] // Public holiday Saturday part-time
  [InlineData(2026, 5, 23, 9, 0, EmploymentType.Casual, true, ShiftEntryType.Worked, 2.50)] // Public holiday Saturday casual
  public void GetMultiplier_PublicHoliday_ReturnsCorrectMultiplier(
      int year, int month, int day, int hour, int minute,
      EmploymentType employmentType,
      bool isPublicHoliday,
      ShiftEntryType shiftEntryType,
      decimal expectedMultiplier) {
    // Arrange
    var startTime = CreateMelbourneTime(year, month, day, hour, minute);

    // Act
    var result = _service.GetMultiplier(startTime, employmentType, isPublicHoliday, shiftEntryType);

    // Assert
    Assert.Equal(expectedMultiplier, result);
  }

  // Annual Leave Tests - Only Permanent Employees (FullTime & PartTime)
  [Theory]
  [InlineData(2026, 5, 19, 9, 0, EmploymentType.FullTime, false, ShiftEntryType.Leave, 1.175)] // Annual leave weekday base - FullTime
  [InlineData(2026, 5, 19, 9, 0, EmploymentType.PartTime, false, ShiftEntryType.Leave, 1.175)] // Annual leave weekday base - PartTime
  [InlineData(2026, 5, 19, 6, 0, EmploymentType.FullTime, false, ShiftEntryType.Leave, 1.50)] // Annual leave weekday early - FullTime
  [InlineData(2026, 5, 19, 6, 0, EmploymentType.PartTime, false, ShiftEntryType.Leave, 1.50)] // Annual leave weekday early - PartTime
  [InlineData(2026, 5, 19, 19, 30, EmploymentType.FullTime, false, ShiftEntryType.Leave, 1.25)] // Annual leave weekday evening - FullTime
  [InlineData(2026, 5, 19, 19, 30, EmploymentType.PartTime, false, ShiftEntryType.Leave, 1.25)] // Annual leave weekday evening - PartTime
  [InlineData(2026, 5, 23, 9, 0, EmploymentType.FullTime, false, ShiftEntryType.Leave, 1.25)] // Annual leave Saturday - FullTime
  [InlineData(2026, 5, 23, 9, 0, EmploymentType.PartTime, false, ShiftEntryType.Leave, 1.25)] // Annual leave Saturday - PartTime
  [InlineData(2026, 5, 24, 12, 0, EmploymentType.FullTime, false, ShiftEntryType.Leave, 1.50)] // Annual leave Sunday - FullTime
  [InlineData(2026, 5, 24, 12, 0, EmploymentType.PartTime, false, ShiftEntryType.Leave, 1.50)] // Annual leave Sunday - PartTime
  public void GetMultiplier_AnnualLeave_PermanentEmployees_ReturnsCorrectMultiplier(
      int year, int month, int day, int hour, int minute,
      EmploymentType employmentType,
      bool isPublicHoliday,
      ShiftEntryType shiftEntryType,
      decimal expectedMultiplier) {
    // Arrange
    var startTime = CreateMelbourneTime(year, month, day, hour, minute);

    // Act
    var result = _service.GetMultiplier(startTime, employmentType, isPublicHoliday, shiftEntryType);

    // Assert
    Assert.Equal(expectedMultiplier, result);
  }

  #endregion

  #region GetTimeSegmentsForShift Tests

  [Fact]
  public void GetTimeSegmentsForShift_WithinSingleTimeSlot_ReturnsSingleSegment() {
    // Arrange
    var startTime = CreateMelbourneTime(2026, 5, 19, 9, 0);
    var endTime = CreateMelbourneTime(2026, 5, 19, 12, 0);

    // Act
    var result = _service.GetTimeSegmentsForShift(startTime, endTime);

    // Assert
    Assert.Single(result);
    Assert.Equal(startTime, result[0].Start);
    Assert.Equal(endTime, result[0].End);
    Assert.Equal(3, result[0].Hours);
  }

  [Fact]
  public void GetTimeSegmentsForShift_SpanningMultipleTimeSlots_ReturnsMultipleSegments() {
    // Arrange
    var startTime = CreateMelbourneTime(2026, 5, 19, 6, 0);
    var endTime = CreateMelbourneTime(2026, 5, 19, 9, 0);

    // Act
    var result = _service.GetTimeSegmentsForShift(startTime, endTime);

    // Assert
    // Should split at 07:00 and 08:00
    Assert.Equal(3, result.Count);

    // First segment: 06:00 - 07:00
    Assert.Equal(new TimeSpan(6, 0, 0), result[0].Start.TimeOfDay);
    Assert.Equal(new TimeSpan(7, 0, 0), result[0].End.TimeOfDay);
    Assert.Equal(1, result[0].Hours);

    // Second segment: 07:00 - 08:00
    Assert.Equal(new TimeSpan(7, 0, 0), result[1].Start.TimeOfDay);
    Assert.Equal(new TimeSpan(8, 0, 0), result[1].End.TimeOfDay);
    Assert.Equal(1, result[1].Hours);

    // Third segment: 08:00 - 09:00
    Assert.Equal(new TimeSpan(8, 0, 0), result[2].Start.TimeOfDay);
    Assert.Equal(new TimeSpan(9, 0, 0), result[2].End.TimeOfDay);
    Assert.Equal(1, result[2].Hours);
  }

  [Fact]
  public void GetTimeSegmentsForShift_FullDayShift_ReturnsMultipleSegments() {
    // Arrange
    var startTime = CreateMelbourneTime(2026, 5, 19, 7, 0);
    var endTime = CreateMelbourneTime(2026, 5, 19, 22, 0);

    // Act
    var result = _service.GetTimeSegmentsForShift(startTime, endTime);

    // Assert
    Assert.NotEmpty(result);

    // Verify that segments are continuous
    for (int i = 0; i < result.Count - 1; i++) {
      Assert.Equal(result[i].End, result[i + 1].Start);
    }

    // Verify total hours
    var totalHours = result.Sum(s => s.Hours);
    Assert.Equal(15, totalHours); // 7:00 to 22:00 = 15 hours
  }

  [Fact]
  public void GetTimeSegmentsForShift_EveningShift_ReturnsCorrectSegments() {
    // Arrange
    var startTime = CreateMelbourneTime(2026, 5, 19, 18, 30);
    var endTime = CreateMelbourneTime(2026, 5, 19, 22, 0);

    // Act
    var result = _service.GetTimeSegmentsForShift(startTime, endTime);

    // Assert
    Assert.NotEmpty(result);
    Assert.Equal(startTime, result[0].Start);
    Assert.Equal(endTime, result[result.Count - 1].End);
  }

  [Fact]
  public void GetTimeSegmentsForShift_ShortShift_ReturnsSingleSegment() {
    // Arrange
    var startTime = CreateMelbourneTime(2026, 5, 19, 10, 0);
    var endTime = CreateMelbourneTime(2026, 5, 19, 11, 0);

    // Act
    var result = _service.GetTimeSegmentsForShift(startTime, endTime);

    // Assert
    Assert.Single(result);
    Assert.Equal(1, result[0].Hours);
  }

  [Fact]
  public void GetTimeSegmentsForShift_StartAndEndTimesArePreserved() {
    // Arrange
    var startTime = CreateMelbourneTime(2026, 5, 19, 14, 30);
    var endTime = CreateMelbourneTime(2026, 5, 19, 20, 45);

    // Act
    var result = _service.GetTimeSegmentsForShift(startTime, endTime);

    // Assert
    Assert.Equal(startTime, result[0].Start);
    Assert.Equal(endTime, result[result.Count - 1].End);
  }

  [Fact]
  public void GetTimeSegmentsForShift_SpanningAllTimeSlots_ReturnsAllSegments() {
    // Arrange
    var startTime = CreateMelbourneTime(2026, 5, 19, 6, 0);
    var endTime = CreateMelbourneTime(2026, 5, 19, 23, 0);

    // Act
    var result = _service.GetTimeSegmentsForShift(startTime, endTime);

    // Assert - should split at 07:00, 08:00, 18:00, 19:00, 21:00
    Assert.Equal(6, result.Count);

    // Verify segments
    Assert.Equal(new TimeSpan(6, 0, 0), result[0].Start.TimeOfDay); // 06:00-07:00
    Assert.Equal(new TimeSpan(7, 0, 0), result[1].Start.TimeOfDay); // 07:00-08:00
    Assert.Equal(new TimeSpan(8, 0, 0), result[2].Start.TimeOfDay); // 08:00-18:00
    Assert.Equal(new TimeSpan(18, 0, 0), result[3].Start.TimeOfDay); // 18:00-19:00
    Assert.Equal(new TimeSpan(19, 0, 0), result[4].Start.TimeOfDay); // 19:00-21:00
    Assert.Equal(new TimeSpan(21, 0, 0), result[5].Start.TimeOfDay); // 21:00-23:00

    // Verify total hours
    var totalHours = result.Sum(s => s.Hours);
    Assert.Equal(17, totalHours); // 06:00 to 23:00 = 17 hours
  }

  #endregion

  #region CalculateGrossPayForShift Tests
  [Theory]
  [InlineData(2026, 5, 19, 9, 0, 2026, 5, 19, 17, 0, EmploymentType.FullTime, false, ShiftEntryType.Worked, ShiftSource.Manual, 150.00)] // Tues 9:00 - 17:00 
  [InlineData(2026, 5, 19, 13, 0, 2026, 5, 19, 21, 0, EmploymentType.PartTime, false, ShiftEntryType.Worked, ShiftSource.Manual, 160.00)] // Tues 13:00 - 21:00
  [InlineData(2026, 5, 19, 08, 0, 2026, 5, 19, 16, 0, EmploymentType.FullTime, false, ShiftEntryType.Worked, ShiftSource.Manual, 150.00)] // Tues 08:00 - 16:00
  [InlineData(2026, 5, 23, 11, 0, 2026, 5, 23, 21, 0, EmploymentType.PartTime, false, ShiftEntryType.Worked, ShiftSource.Manual, 252.50)] // Sat 11:00 - 21:00
  [InlineData(2026, 5, 24, 11, 0, 2026, 5, 24, 21, 0, EmploymentType.PartTime, false, ShiftEntryType.Worked, ShiftSource.Manual, 285.00)] // Sun 11:00 - 21:00
  [InlineData(2026, 5, 18, 11, 0, 2026, 5, 18, 21, 0, EmploymentType.Casual, false, ShiftEntryType.Worked, ShiftSource.Manual, 247.50)] // Monday 11:00 - 21:00
  [InlineData(2026, 5, 23, 11, 0, 2026, 5, 23, 21, 0, EmploymentType.Casual, false, ShiftEntryType.Worked, ShiftSource.Manual, 300.00)] // Saturday 11:00 - 21:00
  public void CalculateGrossPayForShift_WorkedShift_ReturnsCorrectGrossPay(
    int startYear, int startMonth, int startDay, int startHour, int startMinute,
    int endYear, int endMonth, int endDay, int endHour, int endMinute,
    EmploymentType employmentType, bool isPublicHoliday, ShiftEntryType entryType, ShiftSource source, decimal expectedGrossPay
    ) {
    // Arrange
    var shiftDto = new ShiftDto {
      StartAt = CreateMelbourneTime(startYear, startMonth, startDay, startHour, startMinute),
      EndAt = CreateMelbourneTime(endYear, endMonth, endDay, endHour, endMinute),
      TimeZoneId = "Australia/Melbourne",
      UnpaidBreakMinutes = 30,
      PaidBreakMinutes = 0,
      EntryType = entryType,
      EmploymentType = employmentType,
      Source = source
    };
    bool isPublicHolidayFlag = isPublicHoliday;
    decimal baseHourlyRate = 20.00m;
    // Act
    var grossPay = _service.GetPaySegmentsForShift(shiftDto, isPublicHoliday, baseHourlyRate);


    // Assert
    Assert.Equal(expectedGrossPay, grossPay.GrossPay);
  }


  [Theory]
  [InlineData(2026, 5, 19, 9, 0, 2026, 5, 19, 17, 0, EmploymentType.FullTime, false, ShiftEntryType.Leave, ShiftSource.Manual, 176.25)] // Tues 9:00 - 17:00 - fulltime annual leave
  [InlineData(2026, 5, 19, 9, 0, 2026, 5, 19, 17, 0, EmploymentType.PartTime, false, ShiftEntryType.Leave, ShiftSource.Manual, 176.25)] // Tues 9:00 - 17:00 - parttime annual leave
  [InlineData(2026, 5, 19, 6, 0, 2026, 5, 19, 14, 0, EmploymentType.FullTime, false, ShiftEntryType.Leave, ShiftSource.Manual, 189.25)] // Tues 6:00 - 14:00 - fulltime early annual leave
  [InlineData(2026, 5, 24, 11, 0, 2026, 5, 24, 21, 0, EmploymentType.FullTime, false, ShiftEntryType.Leave, ShiftSource.Manual, 285.00)] // Sun 11:00 - 21:00 - fulltime annual leave
  public void CalculateGrossPayForShift_AnnualLeave_ReturnsCorrectGrossPay(
    int startYear, int startMonth, int startDay, int startHour, int startMinute,
    int endYear, int endMonth, int endDay, int endHour, int endMinute,
    EmploymentType employmentType, bool isPublicHoliday, ShiftEntryType entryType, ShiftSource source, decimal expectedGrossPay
    ) {
    // Arrange
    var shiftDto = new ShiftDto {
      StartAt = CreateMelbourneTime(startYear, startMonth, startDay, startHour, startMinute),
      EndAt = CreateMelbourneTime(endYear, endMonth, endDay, endHour, endMinute),
      TimeZoneId = "Australia/Melbourne",
      UnpaidBreakMinutes = 30,
      PaidBreakMinutes = 0,
      EntryType = entryType,
      EmploymentType = employmentType,
      Source = source
    };
    bool isPublicHolidayFlag = isPublicHoliday;
    decimal baseHourlyRate = 20.00m;
    // Act
    var grossPay = _service.GetPaySegmentsForShift(shiftDto, isPublicHolidayFlag, baseHourlyRate);
    // Assert
    Assert.Equal(expectedGrossPay, grossPay.GrossPay);
  }
  #endregion

}