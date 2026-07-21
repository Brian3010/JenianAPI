using Jenian.Application.Common;
using Jenian.Application.Features.Shifts.Dtos;
using Jenian.Domain.Entities;

namespace Jenian.Application.Features.Payroll
{

  // Pharmacy Award ordinary-hours multipliers.
  // Format: time = Full-time/Part-time | Casual
  // base multiplier = 1.0 | 1.25
  //
  // Mon-Fri: 07:00-08:00 = 1.50 | 1.75
  //          19:00-21:00 = 1.25 | 1.50
  //          21:00-24:00 = 1.50 | 1.75
  //
  // Saturday: 07:00-08:00 = 2.00 | 2.25
  //           08:00-18:00 = 1.25 | 1.50
  //           18:00-21:00 = 1.50 | 1.75
  //           21:00-24:00 = 1.75 | 2.00
  //
  // Sunday: 07:00-21:00 = 1.50 | 1.75
  //         Before 07:00 or after 21:00 = 2.00 | 2.25
  //
  // Public holiday: Any time = 2.25 | 2.50
  //
  // Paid non-worked:
  //   Sick/personal/carer's leave = 1.00 | unpaid
  //   Public holiday not worked   = 1.00 | unpaid
  //
  // Annual leave:
  //   Full-time/Part-time only
  //   Ordinary time = 1.175
  //   Penalty time = same as Full-time/Part-time ordinary-hours multiplier
  //   Public holiday during annual leave = 1.00, not deducted from annual leave
  //   Casual = unpaid
  // pay = hrs x baseRate x multiplier


  public class PharmacyAwardRateService : IAwardRateService
  {
    public TotalPaySummary GetPaySegmentsForShift(ShiftDto shiftDto, bool isPublicHoliday, decimal baseHourlyRate) {
      // convert to local Timezone
      var timeZone = ShiftDateHelper.GetTimeZoneInfo(shiftDto.TimeZoneId);
      var localStartAt = TimeZoneInfo.ConvertTime(shiftDto.StartAt, timeZone);
      var localEndAt = TimeZoneInfo.ConvertTime(shiftDto.EndAt, timeZone);


      // Get segments without break adjustment
      List<TimeSegment> segments = GetTimeSegmentsForShift(localStartAt, localEndAt);
      decimal totalGrossPay = 0.0m;

      var regularHoursStart = TimeSpan.Parse("08:00");
      var regularHoursEnd = TimeSpan.Parse("18:00");
      decimal breakHoursRemaining = shiftDto.UnpaidBreakMinutes / 60.0m;

      foreach (var segment in segments) {
        decimal multiplier = GetMultiplier(segment.Start, shiftDto.EmploymentType, isPublicHoliday, shiftDto.EntryType);
        var paidHours = segment.Hours; // e.g. 3 hours in segment

        // Deduct unpaid break ONLY from segments between 08:00-18:00
        if (breakHoursRemaining > 0 &&
            segment.Start.TimeOfDay >= regularHoursStart &&
            segment.End.TimeOfDay <= regularHoursEnd) {

          var hoursToDeduct = Math.Min(paidHours, breakHoursRemaining);
          paidHours -= hoursToDeduct;
          breakHoursRemaining -= hoursToDeduct;
        }

        decimal segmentPay = paidHours * Math.Round((baseHourlyRate * multiplier), 2, MidpointRounding.AwayFromZero);
        totalGrossPay += segmentPay;
      }

      var totalPayableMinutes = segments.Sum(s => s.Hours * 60) - shiftDto.UnpaidBreakMinutes;
      var totalOvertimeMinutes = 0; // Overtime will depend on actual business rule
      var totalEveningPenaltyMinutes = segments.Where(s => s.Start.DayOfWeek != DayOfWeek.Sunday && (
        (s.Start.TimeOfDay >= TimeSpan.Parse("19:00") && s.Start.TimeOfDay < TimeSpan.Parse("21:00")) ||
        (s.End.TimeOfDay > TimeSpan.Parse("19:00") && s.End.TimeOfDay <= TimeSpan.Parse("21:00"))
      )).Sum(s => s.Hours * 60);
      var totalUnpaidBreakMinutes = shiftDto.UnpaidBreakMinutes;


      return new TotalPaySummary(
        TotalPayableMinutes: (int)totalPayableMinutes,
        TotalUnpaidBreakMinutes: totalUnpaidBreakMinutes,
        TotalOvertimeMinutes: (int)totalOvertimeMinutes,
        TotalEveningPenaltyMinutes: (int)totalEveningPenaltyMinutes,
        GrossPay: totalGrossPay
       );
    }

    public decimal GetMultiplier(DateTimeOffset startTime, EmploymentType employmentType, bool isPublicHoliday, ShiftEntryType shiftEntryType) {
      decimal multiplier = 1.0m;
      DayType dayType = startTime.DayOfWeek switch {
        DayOfWeek.Saturday => DayType.Saturday,
        DayOfWeek.Sunday => DayType.Sunday,
        _ => DayType.WeekDay
      };
      bool isPemanentEmployed = employmentType == EmploymentType.FullTime || employmentType == EmploymentType.PartTime;

      if (isPublicHoliday && isPemanentEmployed && shiftEntryType == ShiftEntryType.Worked) {
        multiplier = 2.25m;
      } else if (isPublicHoliday && !isPemanentEmployed && shiftEntryType == ShiftEntryType.Worked) {
        multiplier = 2.50m;
      } else {

        switch (isPemanentEmployed, shiftEntryType, dayType) {
          // Part-time/Full-time employees
          case (true, ShiftEntryType.Worked, DayType.WeekDay): {
              if (startTime.TimeOfDay < TimeSpan.Parse("08:00") || startTime.TimeOfDay >= TimeSpan.Parse("21:00")) multiplier = 1.50m;
              else if (startTime.TimeOfDay >= TimeSpan.Parse("19:00") && startTime.TimeOfDay < TimeSpan.Parse("21:00")) multiplier = 1.25m;
            }
            break;
          case (true, ShiftEntryType.Worked, DayType.Saturday): {
              if (startTime.TimeOfDay < TimeSpan.Parse("08:00")) multiplier = 2.00m;
              else if (startTime.TimeOfDay >= TimeSpan.Parse("18:00") && startTime.TimeOfDay < TimeSpan.Parse("21:00")) multiplier = 1.50m;
              else if (startTime.TimeOfDay > TimeSpan.Parse("21:00") && startTime.TimeOfDay <= TimeSpan.Parse("23:59:59")) multiplier = 1.75m;
              else multiplier = 1.25m;
            }
            break;
          case (true, ShiftEntryType.Worked, DayType.Sunday): {
              if (startTime.TimeOfDay < TimeSpan.Parse("07:00") || startTime.TimeOfDay > TimeSpan.Parse("21:00")) multiplier = 2.00m;
              else multiplier = 1.50m;
            }
            break;
          case (true, ShiftEntryType.Leave, DayType.WeekDay): {
              if (startTime.TimeOfDay < TimeSpan.Parse("08:00") || startTime.TimeOfDay >= TimeSpan.Parse("21:00")) multiplier = 1.50m;
              else if (startTime.TimeOfDay >= TimeSpan.Parse("19:00") && startTime.TimeOfDay < TimeSpan.Parse("21:00")) multiplier = 1.25m;
              else multiplier = 1.175m;
            }
            break;
          case (true, ShiftEntryType.Leave, DayType.Saturday): {
              if (startTime.TimeOfDay < TimeSpan.Parse("08:00")) multiplier = 2.00m;
              else if (startTime.TimeOfDay >= TimeSpan.Parse("18:00") && startTime.TimeOfDay <= TimeSpan.Parse("21:00")) multiplier = 1.50m;
              else if (startTime.TimeOfDay > TimeSpan.Parse("21:00") && startTime.TimeOfDay <= TimeSpan.Parse("23:59:59")) multiplier = 1.75m;
              else multiplier = 1.25m;
            }
            break;
          case (true, ShiftEntryType.Leave, DayType.Sunday): {
              if (startTime.TimeOfDay < TimeSpan.Parse("07:00") || startTime.TimeOfDay > TimeSpan.Parse("21:00")) multiplier = 2.00m;
              else multiplier = 1.50m;
            }
            break;

          // Casual employees
          case (false, ShiftEntryType.Worked, DayType.WeekDay): {
              if (startTime.TimeOfDay < TimeSpan.Parse("08:00") || startTime.TimeOfDay > TimeSpan.Parse("21:00")) multiplier = 1.75m;
              else if (startTime.TimeOfDay >= TimeSpan.Parse("19:00") && startTime.TimeOfDay <= TimeSpan.Parse("21:00")) multiplier = 1.50m;
              else multiplier = 1.25m;
            }
            break;
          case (false, ShiftEntryType.Worked, DayType.Saturday): {
              if (startTime.TimeOfDay < TimeSpan.Parse("08:00")) multiplier = 2.25m;
              else if (startTime.TimeOfDay >= TimeSpan.Parse("18:00") && startTime.TimeOfDay <= TimeSpan.Parse("21:00")) multiplier = 1.75m;
              else if (startTime.TimeOfDay > TimeSpan.Parse("21:00") && startTime.TimeOfDay <= TimeSpan.Parse("23:59:59")) multiplier = 2.00m;
              else multiplier = 1.50m;
            }
            break;
          case (false, ShiftEntryType.Worked, DayType.Sunday): {
              if (startTime.TimeOfDay < TimeSpan.Parse("07:00") || startTime.TimeOfDay > TimeSpan.Parse("21:00")) multiplier = 2.25m;
              else multiplier = 1.75m;
            }
            break;

        }
      }
      return multiplier;
    }

    public List<TimeSegment> GetTimeSegmentsForShift(DateTimeOffset startTime, DateTimeOffset endTime) {
      var timeSlots = new TimeSpan[] {
        TimeSpan.Parse("07:00"),
        TimeSpan.Parse("08:00"),
        TimeSpan.Parse("18:00"),
        TimeSpan.Parse("19:00"),
        TimeSpan.Parse("21:00"),
      };

      var segmentList = new List<TimeSegment>();
      var from = startTime.TimeOfDay;
      var to = endTime.TimeOfDay;

      // Create segments based on pay rate boundaries
      for (int i = 0; i < timeSlots.Length && timeSlots[i] < to; i++) {
        if (from < timeSlots[i]) {
          segmentList.Add(new TimeSegment(
            new DateTimeOffset(startTime.Date + from, startTime.Offset),
            new DateTimeOffset(startTime.Date + timeSlots[i], startTime.Offset),
            (decimal)(timeSlots[i] - from).TotalHours
          ));
          from = timeSlots[i];
        }
      }

      // Add final segment
      segmentList.Add(new TimeSegment(
        new DateTimeOffset(startTime.Date + from, startTime.Offset),
        new DateTimeOffset(startTime.Date + to, startTime.Offset),
        (decimal)(to - from).TotalHours
      ));

      return segmentList;
    }
  }
}
