using static Jenian.Tests.TestSupport.MelbourneTime;
﻿using Jenian.Application.Features.Payroll;
using Jenian.Application.Features.Shifts.Dtos;
using Jenian.Domain.Entities;

namespace Jenian.Tests.Application.Payroll
{
  public class PharmacyAwardMultiplierTests
  {
    private readonly PharmacyAwardRateService _service;

    public PharmacyAwardMultiplierTests() {
      _service = new PharmacyAwardRateService();
    }

    // Weekday Tests - Permanent Employees (FullTime & PartTime have same rates)
    public static TheoryData<DateTimeOffset, EmploymentType, bool, ShiftEntryType, decimal> GetMultiplier_Weekday_PermanentEmployees_ReturnsCorrectMultiplierCases => new() {
      { CreateMelbourneTime(2026, 5, 19, 7, 30), EmploymentType.FullTime, false, ShiftEntryType.Worked, 1.50m },
      { CreateMelbourneTime(2026, 5, 19, 7, 30), EmploymentType.PartTime, false, ShiftEntryType.Worked, 1.50m },
      { CreateMelbourneTime(2026, 5, 19, 9, 0), EmploymentType.FullTime, false, ShiftEntryType.Worked, 1.0m },
      { CreateMelbourneTime(2026, 5, 19, 9, 0), EmploymentType.PartTime, false, ShiftEntryType.Worked, 1.0m },
      { CreateMelbourneTime(2026, 5, 19, 19, 30), EmploymentType.FullTime, false, ShiftEntryType.Worked, 1.25m },
      { CreateMelbourneTime(2026, 5, 19, 19, 30), EmploymentType.PartTime, false, ShiftEntryType.Worked, 1.25m },
      { CreateMelbourneTime(2026, 5, 19, 21, 30), EmploymentType.FullTime, false, ShiftEntryType.Worked, 1.50m },
      { CreateMelbourneTime(2026, 5, 19, 21, 30), EmploymentType.PartTime, false, ShiftEntryType.Worked, 1.50m },
      { CreateMelbourneTime(2026, 5, 19, 6, 0), EmploymentType.FullTime, false, ShiftEntryType.Worked, 1.50m },
      { CreateMelbourneTime(2026, 5, 19, 6, 0), EmploymentType.PartTime, false, ShiftEntryType.Worked, 1.50m },
    };

    [Theory]
    [MemberData(nameof(GetMultiplier_Weekday_PermanentEmployees_ReturnsCorrectMultiplierCases))]
    public void GetMultiplier_Weekday_PermanentEmployees_ReturnsCorrectMultiplier(
        DateTimeOffset startTime,
        EmploymentType employmentType,
        bool isPublicHoliday,
        ShiftEntryType shiftEntryType,
        decimal expectedMultiplier) {
      // Arrange

      // Act
      var result = _service.GetMultiplier(startTime, employmentType, isPublicHoliday, shiftEntryType);

      // Assert
      Assert.Equal(expectedMultiplier, result);
    }

    // Weekday Tests - Casual Employees (different rates)
    public static TheoryData<DateTimeOffset, EmploymentType, bool, ShiftEntryType, decimal> GetMultiplier_Weekday_Casual_ReturnsCorrectMultiplierCases => new() {
      { CreateMelbourneTime(2026, 5, 19, 7, 30), EmploymentType.Casual, false, ShiftEntryType.Worked, 1.75m },
      { CreateMelbourneTime(2026, 5, 19, 9, 0), EmploymentType.Casual, false, ShiftEntryType.Worked, 1.25m },
      { CreateMelbourneTime(2026, 5, 19, 19, 30), EmploymentType.Casual, false, ShiftEntryType.Worked, 1.50m },
      { CreateMelbourneTime(2026, 5, 19, 21, 30), EmploymentType.Casual, false, ShiftEntryType.Worked, 1.75m },
      { CreateMelbourneTime(2026, 5, 19, 6, 0), EmploymentType.Casual, false, ShiftEntryType.Worked, 1.75m },
    };

    [Theory]
    [MemberData(nameof(GetMultiplier_Weekday_Casual_ReturnsCorrectMultiplierCases))]
    public void GetMultiplier_Weekday_Casual_ReturnsCorrectMultiplier(
        DateTimeOffset startTime,
        EmploymentType employmentType,
        bool isPublicHoliday,
        ShiftEntryType shiftEntryType,
        decimal expectedMultiplier) {
      // Arrange

      // Act
      var result = _service.GetMultiplier(startTime, employmentType, isPublicHoliday, shiftEntryType);

      // Assert
      Assert.Equal(expectedMultiplier, result);
    }

    // Saturday Tests - Permanent Employees (FullTime & PartTime have same rates)
    public static TheoryData<DateTimeOffset, EmploymentType, bool, ShiftEntryType, decimal> GetMultiplier_Saturday_PermanentEmployees_ReturnsCorrectMultiplierCases => new() {
      { CreateMelbourneTime(2026, 5, 23, 7, 30), EmploymentType.FullTime, false, ShiftEntryType.Worked, 2.00m },
      { CreateMelbourneTime(2026, 5, 23, 7, 30), EmploymentType.PartTime, false, ShiftEntryType.Worked, 2.00m },
      { CreateMelbourneTime(2026, 5, 23, 9, 0), EmploymentType.FullTime, false, ShiftEntryType.Worked, 1.25m },
      { CreateMelbourneTime(2026, 5, 23, 9, 0), EmploymentType.PartTime, false, ShiftEntryType.Worked, 1.25m },
      { CreateMelbourneTime(2026, 5, 23, 18, 30), EmploymentType.FullTime, false, ShiftEntryType.Worked, 1.50m },
      { CreateMelbourneTime(2026, 5, 23, 18, 30), EmploymentType.PartTime, false, ShiftEntryType.Worked, 1.50m },
      { CreateMelbourneTime(2026, 5, 23, 21, 30), EmploymentType.FullTime, false, ShiftEntryType.Worked, 1.75m },
      { CreateMelbourneTime(2026, 5, 23, 21, 30), EmploymentType.PartTime, false, ShiftEntryType.Worked, 1.75m },
    };

    [Theory]
    [MemberData(nameof(GetMultiplier_Saturday_PermanentEmployees_ReturnsCorrectMultiplierCases))]
    public void GetMultiplier_Saturday_PermanentEmployees_ReturnsCorrectMultiplier(
        DateTimeOffset startTime,
        EmploymentType employmentType,
        bool isPublicHoliday,
        ShiftEntryType shiftEntryType,
        decimal expectedMultiplier) {
      // Arrange

      // Act
      var result = _service.GetMultiplier(startTime, employmentType, isPublicHoliday, shiftEntryType);

      // Assert
      Assert.Equal(expectedMultiplier, result);
    }

    // Saturday Tests - Casual Employees (different rates)
    public static TheoryData<DateTimeOffset, EmploymentType, bool, ShiftEntryType, decimal> GetMultiplier_Saturday_Casual_ReturnsCorrectMultiplierCases => new() {
      { CreateMelbourneTime(2026, 5, 23, 7, 30), EmploymentType.Casual, false, ShiftEntryType.Worked, 2.25m },
      { CreateMelbourneTime(2026, 5, 23, 9, 0), EmploymentType.Casual, false, ShiftEntryType.Worked, 1.50m },
      { CreateMelbourneTime(2026, 5, 23, 18, 30), EmploymentType.Casual, false, ShiftEntryType.Worked, 1.75m },
      { CreateMelbourneTime(2026, 5, 23, 21, 30), EmploymentType.Casual, false, ShiftEntryType.Worked, 2.00m },
    };

    [Theory]
    [MemberData(nameof(GetMultiplier_Saturday_Casual_ReturnsCorrectMultiplierCases))]
    public void GetMultiplier_Saturday_Casual_ReturnsCorrectMultiplier(
        DateTimeOffset startTime,
        EmploymentType employmentType,
        bool isPublicHoliday,
        ShiftEntryType shiftEntryType,
        decimal expectedMultiplier) {
      // Arrange

      // Act
      var result = _service.GetMultiplier(startTime, employmentType, isPublicHoliday, shiftEntryType);

      // Assert
      Assert.Equal(expectedMultiplier, result);
    }

    // Sunday Tests - Permanent Employees (FullTime & PartTime have same rates)
    public static TheoryData<DateTimeOffset, EmploymentType, bool, ShiftEntryType, decimal> GetMultiplier_Sunday_PermanentEmployees_ReturnsCorrectMultiplierCases => new() {
      { CreateMelbourneTime(2026, 5, 24, 6, 0), EmploymentType.FullTime, false, ShiftEntryType.Worked, 2.00m },
      { CreateMelbourneTime(2026, 5, 24, 6, 0), EmploymentType.PartTime, false, ShiftEntryType.Worked, 2.00m },
      { CreateMelbourneTime(2026, 5, 24, 12, 0), EmploymentType.FullTime, false, ShiftEntryType.Worked, 1.50m },
      { CreateMelbourneTime(2026, 5, 24, 12, 0), EmploymentType.PartTime, false, ShiftEntryType.Worked, 1.50m },
      { CreateMelbourneTime(2026, 5, 24, 22, 0), EmploymentType.FullTime, false, ShiftEntryType.Worked, 2.00m },
      { CreateMelbourneTime(2026, 5, 24, 22, 0), EmploymentType.PartTime, false, ShiftEntryType.Worked, 2.00m },
    };

    [Theory]
    [MemberData(nameof(GetMultiplier_Sunday_PermanentEmployees_ReturnsCorrectMultiplierCases))]
    public void GetMultiplier_Sunday_PermanentEmployees_ReturnsCorrectMultiplier(
        DateTimeOffset startTime,
        EmploymentType employmentType,
        bool isPublicHoliday,
        ShiftEntryType shiftEntryType,
        decimal expectedMultiplier) {
      // Arrange

      // Act
      var result = _service.GetMultiplier(startTime, employmentType, isPublicHoliday, shiftEntryType);

      // Assert
      Assert.Equal(expectedMultiplier, result);
    }

    // Sunday Tests - Casual Employees (different rates)
    public static TheoryData<DateTimeOffset, EmploymentType, bool, ShiftEntryType, decimal> GetMultiplier_Sunday_Casual_ReturnsCorrectMultiplierCases => new() {
      { CreateMelbourneTime(2026, 5, 24, 6, 0), EmploymentType.Casual, false, ShiftEntryType.Worked, 2.25m },
      { CreateMelbourneTime(2026, 5, 24, 12, 0), EmploymentType.Casual, false, ShiftEntryType.Worked, 1.75m },
      { CreateMelbourneTime(2026, 5, 24, 22, 0), EmploymentType.Casual, false, ShiftEntryType.Worked, 2.25m },
    };

    [Theory]
    [MemberData(nameof(GetMultiplier_Sunday_Casual_ReturnsCorrectMultiplierCases))]
    public void GetMultiplier_Sunday_Casual_ReturnsCorrectMultiplier(
        DateTimeOffset startTime,
        EmploymentType employmentType,
        bool isPublicHoliday,
        ShiftEntryType shiftEntryType,
        decimal expectedMultiplier) {
      // Arrange

      // Act
      var result = _service.GetMultiplier(startTime, employmentType, isPublicHoliday, shiftEntryType);

      // Assert
      Assert.Equal(expectedMultiplier, result);
    }

    // Public Holiday Tests - Both permanent and casual
    public static TheoryData<DateTimeOffset, EmploymentType, bool, ShiftEntryType, decimal> GetMultiplier_PublicHoliday_ReturnsCorrectMultiplierCases => new() {
      { CreateMelbourneTime(2026, 5, 19, 9, 0), EmploymentType.FullTime, true, ShiftEntryType.Worked, 2.25m },
      { CreateMelbourneTime(2026, 5, 19, 9, 0), EmploymentType.PartTime, true, ShiftEntryType.Worked, 2.25m },
      { CreateMelbourneTime(2026, 5, 19, 9, 0), EmploymentType.Casual, true, ShiftEntryType.Worked, 2.50m },
      { CreateMelbourneTime(2026, 5, 23, 9, 0), EmploymentType.FullTime, true, ShiftEntryType.Worked, 2.25m },
      { CreateMelbourneTime(2026, 5, 23, 9, 0), EmploymentType.PartTime, true, ShiftEntryType.Worked, 2.25m },
      { CreateMelbourneTime(2026, 5, 23, 9, 0), EmploymentType.Casual, true, ShiftEntryType.Worked, 2.50m },
    };

    [Theory]
    [MemberData(nameof(GetMultiplier_PublicHoliday_ReturnsCorrectMultiplierCases))]
    public void GetMultiplier_PublicHoliday_ReturnsCorrectMultiplier(
        DateTimeOffset startTime,
        EmploymentType employmentType,
        bool isPublicHoliday,
        ShiftEntryType shiftEntryType,
        decimal expectedMultiplier) {
      // Arrange

      // Act
      var result = _service.GetMultiplier(startTime, employmentType, isPublicHoliday, shiftEntryType);

      // Assert
      Assert.Equal(expectedMultiplier, result);
    }

    // Annual Leave Tests - Only Permanent Employees (FullTime & PartTime)
    public static TheoryData<DateTimeOffset, EmploymentType, bool, ShiftEntryType, decimal> GetMultiplier_AnnualLeave_PermanentEmployees_ReturnsCorrectMultiplierCases => new() {
      { CreateMelbourneTime(2026, 5, 19, 9, 0), EmploymentType.FullTime, false, ShiftEntryType.Leave, 1.175m },
      { CreateMelbourneTime(2026, 5, 19, 9, 0), EmploymentType.PartTime, false, ShiftEntryType.Leave, 1.175m },
      { CreateMelbourneTime(2026, 5, 19, 6, 0), EmploymentType.FullTime, false, ShiftEntryType.Leave, 1.50m },
      { CreateMelbourneTime(2026, 5, 19, 6, 0), EmploymentType.PartTime, false, ShiftEntryType.Leave, 1.50m },
      { CreateMelbourneTime(2026, 5, 19, 19, 30), EmploymentType.FullTime, false, ShiftEntryType.Leave, 1.25m },
      { CreateMelbourneTime(2026, 5, 19, 19, 30), EmploymentType.PartTime, false, ShiftEntryType.Leave, 1.25m },
      { CreateMelbourneTime(2026, 5, 23, 9, 0), EmploymentType.FullTime, false, ShiftEntryType.Leave, 1.25m },
      { CreateMelbourneTime(2026, 5, 23, 9, 0), EmploymentType.PartTime, false, ShiftEntryType.Leave, 1.25m },
      { CreateMelbourneTime(2026, 5, 24, 12, 0), EmploymentType.FullTime, false, ShiftEntryType.Leave, 1.50m },
      { CreateMelbourneTime(2026, 5, 24, 12, 0), EmploymentType.PartTime, false, ShiftEntryType.Leave, 1.50m },
    };

    [Theory]
    [MemberData(nameof(GetMultiplier_AnnualLeave_PermanentEmployees_ReturnsCorrectMultiplierCases))]
    public void GetMultiplier_AnnualLeave_PermanentEmployees_ReturnsCorrectMultiplier(
        DateTimeOffset startTime,
        EmploymentType employmentType,
        bool isPublicHoliday,
        ShiftEntryType shiftEntryType,
        decimal expectedMultiplier) {
      // Arrange

      // Act
      var result = _service.GetMultiplier(startTime, employmentType, isPublicHoliday, shiftEntryType);

      // Assert
      Assert.Equal(expectedMultiplier, result);
    }

  }
}
