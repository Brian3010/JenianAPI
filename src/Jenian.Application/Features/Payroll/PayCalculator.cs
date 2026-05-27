using Jenian.Application.Features.PaySummaries.Dtos;
using Jenian.Application.Features.Shifts.Dtos;


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
  }
}
