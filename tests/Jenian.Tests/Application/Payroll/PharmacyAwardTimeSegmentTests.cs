using static Jenian.Tests.TestSupport.MelbourneTime;
﻿using Jenian.Application.Features.Payroll;
using Jenian.Application.Features.Shifts.Dtos;
using Jenian.Domain.Entities;

namespace Jenian.Tests.Application.Payroll
{
  public class PharmacyAwardTimeSegmentTests
  {
    private readonly PharmacyAwardRateService _service;

    public PharmacyAwardTimeSegmentTests() {
      _service = new PharmacyAwardRateService();
    }

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
    public void GetTimeSegmentsForShift_MultipleSegments_PreservesStartAndEnd() {
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

  }
}
