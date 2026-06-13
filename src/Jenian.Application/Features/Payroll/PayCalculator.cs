using Jenian.Application.Features.PaySummaries.Dtos;
using Jenian.Application.Features.Shifts.Dtos;
using Jenian.Domain.Entities;


namespace Jenian.Application.Features.Payroll
{
  public class PayCalculator : IPayCalculator
  {
    private readonly IPublicHolidayService _publicHolidayService;
    private readonly IAwardRateService _awardRateService;

    public PayCalculator(
      IPublicHolidayService publicHolidayService,
      IAwardRateService awardRateService

      ) {
      _publicHolidayService = publicHolidayService;
      _awardRateService = awardRateService;
    }

    public UserDailyPaySummaryDto CalculateDailyPay(List<ShiftDto> shifts, string userId) {


      // Note: 'VIC' later will be determined by user's location or shift location
      var isPublicHoliday = _publicHolidayService.IsPublicHoliday(DateOnly.FromDateTime(shifts[0].StartAt.DateTime), "VIC");
      const decimal baseHourlyRate = 26.55m; //Note: latter need to get from user profile from database or jwt token claims

      var results = new List<TotalPaySummary>();
      foreach (var shift in shifts) {
        if (shift.StartAt.DateTime.Date != shifts[0].StartAt.DateTime.Date) {
          throw new ArgumentException("All shifts must be on the same day for daily pay calculation.");
        }
        results.Add(_awardRateService.GetPaySegmentsForShift(shift, isPublicHoliday, baseHourlyRate));
      }


      var userDailyPaySummary = new UserDailyPaySummaryDto {
        WorkDate = DateOnly.FromDateTime(shifts[0].StartAt.DateTime),
        UserId = userId,
        BaseRateUsed = baseHourlyRate,
        GrossPay = results.Sum(r => r.GrossPay),
        TotalEveningPenaltyMinutes = results.Sum(r => r.TotalEveningPenaltyMinutes),
        TotalOvertimeMinutes = results.Sum(r => r.TotalOvertimeMinutes),
        TotalPayableMinutes = results.Sum(r => r.TotalPayableMinutes),
        TotalUnpaidBreakMinutes = results.Sum(r => r.TotalUnpaidBreakMinutes)
      };

      return userDailyPaySummary;
    }

    public PayCycleDateRange CalculatePayCycleDateRange(PayCycleType userPayCycle, DateOnly anchorStartDate) {
      var todayDate = DateOnly.FromDateTime(DateTime.UtcNow);
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
