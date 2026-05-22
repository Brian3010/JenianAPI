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
  public interface IAwardRateService
  {
    decimal GetMultiplier(
      DateTimeOffset startTime,
      EmploymentType employmentType,
      bool isPublicHoliday,
      ShiftEntryType shiftEntryType
      );



    List<TimeSegment> GetTimeSegmentsForShift(DateTimeOffset startTime, DateTimeOffset endTime);

    decimal CalculateGrossPayForShift(
      ShiftDto shiftDto,
      bool isPublicHoliday,
      decimal baseHourlyRate
      );

  }
}
