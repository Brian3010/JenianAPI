using Jenian.Application.Features.Shifts.Dtos;
using Jenian.Domain.Entities;

namespace Jenian.Application.Features.Shifts.Services
{

  public enum DayType
  {
    WeekDay,
    Saturday,
    Sunday
  }
  public record TimeSegment(
    DateTimeOffset Start,
    DateTimeOffset End,
    int Hours
  );

  public record TotalPaySummary(
    int TotalPayableMinutes,
    int TotalUnpaidBreakMinutes,
    int TotalOvertimeMinutes,
    int TotalEveningPenaltyMinutes,
    decimal GrossPay
  );

  public interface IAwardRateService
  {
    decimal GetMultiplier(
      DateTimeOffset startTime,
      EmploymentType employmentType,
      bool isPublicHoliday,
      ShiftEntryType shiftEntryType
      );



    List<TimeSegment> GetTimeSegmentsForShift(DateTimeOffset startTime, DateTimeOffset endTime);

    TotalPaySummary GetPaySegmentsForShift(
      ShiftDto shiftDto,
      bool isPublicHoliday,
      decimal baseHourlyRate
      );

  }
}

