using Jenian.Application.Common;
using Jenian.Application.Features.PaySummaries.Dtos;
using Jenian.Application.Features.Shifts.Dtos;
using Jenian.Domain.Entities;


namespace Jenian.Application.Features.Payroll
{
  public class PayCalculator : IPayCalculator
  {
    private readonly IPublicHolidayService _publicHolidayService;
    private readonly IAwardRateService _awardRateService;
    private readonly TimeProvider _timeProvider;

    public PayCalculator(
      IPublicHolidayService publicHolidayService,
      IAwardRateService awardRateService,
      TimeProvider timeProvider

      ) {
      _publicHolidayService = publicHolidayService;
      _awardRateService = awardRateService;
      _timeProvider = timeProvider;
    }

    public UserDailyPaySummaryDto CalculateDailyPay(List<ShiftDto> shifts, string userId) {

      var workDate = ShiftDateHelper.GetWorkDate(shifts[0].StartAt, shifts[0].TimeZoneId);
      // Note: 'VIC' later will be determined by user's location or shift location
      var isPublicHoliday = _publicHolidayService.IsPublicHoliday(workDate, "VIC");
      const decimal baseHourlyRate = 27.81m; //Note: latter need to get from user profile from database or jwt token claims

      var results = new List<TotalPaySummary>();
      foreach (var shift in shifts) {
        if (ShiftDateHelper.GetWorkDate(shift.StartAt, shift.TimeZoneId) != workDate) {
          throw new ArgumentException("All shifts must be on the same day for daily pay calculation.");
        }
        results.Add(_awardRateService.GetPaySegmentsForShift(shift, isPublicHoliday, baseHourlyRate));
      }


      var userDailyPaySummary = new UserDailyPaySummaryDto {
        WorkDate = workDate,
        UserId = userId,
        BaseRateUsed = baseHourlyRate,
        GrossPay = results.Sum(r => r.GrossPay),
        TotalEveningPenaltyMinutes = results.Sum(r => r.TotalEveningPenaltyMinutes),
        TotalOvertimeMinutes = results.Sum(r => r.TotalOvertimeMinutes),
        TotalPayableMinutes = results.Sum(r => r.TotalPayableMinutes),
        TotalPaidBreakMinutes = shifts.Sum(shift => shift.PaidBreakMinutes),
        TotalUnpaidBreakMinutes = results.Sum(r => r.TotalUnpaidBreakMinutes)
      };

      return userDailyPaySummary;
    }

    public PayCycleDateRange CalculatePayCycleDateRange(PayCycleType userPayCycle, DateOnly anchorStartDate) {
      var melbourneTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Australia/Melbourne");
      var melbourneNow = TimeZoneInfo.ConvertTime(_timeProvider.GetUtcNow(), melbourneTimeZone);
      var todayDate = DateOnly.FromDateTime(melbourneNow.DateTime);
      DateOnly cycleStartDate;
      DateOnly cycleEndDate;
      switch (userPayCycle) {
        case PayCycleType.Weekly:
          var daysSinceWeeklyAnchor = todayDate.DayNumber - anchorStartDate.DayNumber;
          var weeklyIndex = Math.Floor(daysSinceWeeklyAnchor / 7.0);
          var currentWeekStart = anchorStartDate.AddDays((int)weeklyIndex * 7);

          cycleStartDate = currentWeekStart;
          cycleEndDate = currentWeekStart.AddDays(6);
          break;

        case PayCycleType.Fortnightly:
          var daysSinceFortnightlyAnchor = todayDate.DayNumber - anchorStartDate.DayNumber;
          var fortnightIndex = Math.Floor(daysSinceFortnightlyAnchor / 14.0);
          var currentFortnightStart = anchorStartDate.AddDays((int)fortnightIndex * 14);

          cycleStartDate = currentFortnightStart;
          cycleEndDate = currentFortnightStart.AddDays(13);
          break;

        case PayCycleType.Monthly:
          cycleStartDate = new DateOnly(todayDate.Year, todayDate.Month, 1);
          cycleEndDate = new DateOnly(
              todayDate.Year,
              todayDate.Month,
              DateTime.DaysInMonth(todayDate.Year, todayDate.Month)
          );
          break;

        default:
          throw new ArgumentOutOfRangeException(nameof(userPayCycle));
      }
      return new PayCycleDateRange(cycleStartDate, cycleEndDate);
    }
  }
}
