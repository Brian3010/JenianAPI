using static Jenian.Tests.TestSupport.MelbourneTime;
﻿using Jenian.Application.Features.Payroll;
using Jenian.Application.Features.Shifts.Dtos;
using Jenian.Domain.Entities;

namespace Jenian.Tests.Application.Payroll
{
  public class PharmacyAwardPayCalculationTests
  {
    private readonly PharmacyAwardRateService _service;

    public PharmacyAwardPayCalculationTests() {
      _service = new PharmacyAwardRateService();
    }

    public static TheoryData<DateTimeOffset, DateTimeOffset, EmploymentType, bool, ShiftEntryType, decimal> GetPaySegmentsForShift_WorkedShift_ReturnsCorrectGrossPayCases => new() {
      { CreateMelbourneTime(2026, 5, 19, 9, 0), CreateMelbourneTime(2026, 5, 19, 17, 0), EmploymentType.FullTime, false, ShiftEntryType.Worked, 150.00m },
      { CreateMelbourneTime(2026, 5, 19, 13, 0), CreateMelbourneTime(2026, 5, 19, 21, 0), EmploymentType.PartTime, false, ShiftEntryType.Worked, 160.00m },
      { CreateMelbourneTime(2026, 5, 19, 08, 0), CreateMelbourneTime(2026, 5, 19, 16, 0), EmploymentType.FullTime, false, ShiftEntryType.Worked, 150.00m },
      { CreateMelbourneTime(2026, 5, 23, 11, 0), CreateMelbourneTime(2026, 5, 23, 21, 0), EmploymentType.PartTime, false, ShiftEntryType.Worked, 252.50m },
      { CreateMelbourneTime(2026, 5, 24, 11, 0), CreateMelbourneTime(2026, 5, 24, 21, 0), EmploymentType.PartTime, false, ShiftEntryType.Worked, 285.00m },
      { CreateMelbourneTime(2026, 5, 18, 11, 0), CreateMelbourneTime(2026, 5, 18, 21, 0), EmploymentType.Casual, false, ShiftEntryType.Worked, 247.50m },
      { CreateMelbourneTime(2026, 5, 23, 11, 0), CreateMelbourneTime(2026, 5, 23, 21, 0), EmploymentType.Casual, false, ShiftEntryType.Worked, 300.00m },
    };

    [Theory]
    [MemberData(nameof(GetPaySegmentsForShift_WorkedShift_ReturnsCorrectGrossPayCases))]
    public void GetPaySegmentsForShift_WorkedShift_ReturnsCorrectGrossPay(
      DateTimeOffset start, DateTimeOffset end, EmploymentType employmentType,
      bool isPublicHoliday, ShiftEntryType entryType, decimal expectedGrossPay
      ) {
      // Arrange
      var shiftDto = new ShiftDto {
        StartAt = start,
        EndAt = end,
        TimeZoneId = "Australia/Melbourne",
        UnpaidBreakMinutes = 30,
        PaidBreakMinutes = 0,
        EntryType = entryType,
        EmploymentType = employmentType,
        Source = ShiftSource.Manual
      };
      decimal baseHourlyRate = 20.00m;
      // Act
      var grossPay = _service.GetPaySegmentsForShift(shiftDto, isPublicHoliday, baseHourlyRate);

      // Assert
      Assert.Equal(expectedGrossPay, grossPay.GrossPay);
    }

    public static TheoryData<DateTimeOffset, DateTimeOffset, EmploymentType, bool, ShiftEntryType, decimal> GetPaySegmentsForShift_AnnualLeave_ReturnsCorrectGrossPayCases => new() {
      { CreateMelbourneTime(2026, 5, 19, 9, 0), CreateMelbourneTime(2026, 5, 19, 17, 0), EmploymentType.FullTime, false, ShiftEntryType.Leave, 176.25m },
      { CreateMelbourneTime(2026, 5, 19, 9, 0), CreateMelbourneTime(2026, 5, 19, 17, 0), EmploymentType.PartTime, false, ShiftEntryType.Leave, 176.25m },
      { CreateMelbourneTime(2026, 5, 19, 6, 0), CreateMelbourneTime(2026, 5, 19, 14, 0), EmploymentType.FullTime, false, ShiftEntryType.Leave, 189.25m },
      { CreateMelbourneTime(2026, 5, 24, 11, 0), CreateMelbourneTime(2026, 5, 24, 21, 0), EmploymentType.FullTime, false, ShiftEntryType.Leave, 285.00m },
    };

    [Theory]
    [MemberData(nameof(GetPaySegmentsForShift_AnnualLeave_ReturnsCorrectGrossPayCases))]
    public void GetPaySegmentsForShift_AnnualLeave_ReturnsCorrectGrossPay(
      DateTimeOffset start, DateTimeOffset end, EmploymentType employmentType,
      bool isPublicHoliday, ShiftEntryType entryType, decimal expectedGrossPay
      ) {
      // Arrange
      var shiftDto = new ShiftDto {
        StartAt = start,
        EndAt = end,
        TimeZoneId = "Australia/Melbourne",
        UnpaidBreakMinutes = 30,
        PaidBreakMinutes = 0,
        EntryType = entryType,
        EmploymentType = employmentType,
        Source = ShiftSource.Manual
      };
      decimal baseHourlyRate = 20.00m;
      // Act
      var grossPay = _service.GetPaySegmentsForShift(shiftDto, isPublicHoliday, baseHourlyRate);
      // Assert
      Assert.Equal(expectedGrossPay, grossPay.GrossPay);
    }
  }
}
