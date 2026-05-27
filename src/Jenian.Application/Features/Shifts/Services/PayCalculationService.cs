using Jenian.Application.Abstractions.Persistence;
using Jenian.Application.Features.Shifts.Dtos;
using Jenian.Domain.Entities;

namespace Jenian.Application.Features.Shifts.Services
{
  public class PayCalculationService : IPayCalculationService
  {
    private readonly IAwardRateService _awardRateService;
    private readonly IPublicHolidayService _publicHolidayService;
    private readonly IPaySummaryRepository _paySummaryRepository;
    private readonly IShiftRepository _shiftRepository;

    public PayCalculationService(
      IAwardRateService awardRateService,
      IPublicHolidayService publicHolidayService,
      IPaySummaryRepository paySummaryRepository,
      IShiftRepository shiftRepository
      ) {
      _awardRateService = awardRateService;
      _publicHolidayService = publicHolidayService;
      _paySummaryRepository = paySummaryRepository;
      _shiftRepository = shiftRepository;
    }
    public UserDailyPaySummaryDto CalculateDailyPay(List<ShiftDto> shifts, string userId) {

      //Note: 'VIC' later will be determined by user's location or shift location
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

    // Note:
    // UserShift.UserDailyPaySummaryId is the foreign key linking a shift to its daily pay summary (UserShift table).
    //
    // When recalculating pay for a user/date:
    // - Load all remaining shifts for that user/date after create/update/delete has completed, using affectedWorkDates.
    // - If no shifts remain for that date in UserShift table, delete the existing UserDailyPaySummary for that user/date,
    //   or leave no summary for that date.
    // - If a UserDailyPaySummary already exists for that user/date,
    //   update the existing summary and assign its Id to all related shifts.
    // - If no UserDailyPaySummary exists for that user/date,
    //   create a new summary and assign its Id to all related shifts.
    // - Do not calculate from deleted shifts. Deleted shifts only contribute their old work date
    //   to the affectedWorkDates list before deletion
    public async Task RecalculateForDatesAsync(string userId, HashSet<DateOnly> affectedWorkDates, CancellationToken cancellationToken) {

      var summaries = new List<UserDailyPaySummaryDto>();


      // e.g. affectedWorkDates = { 2024-06-01, 2024-06-02 }
      foreach (var workDate in affectedWorkDates) {

        var shiftsForDate = await _shiftRepository.GetByDateAndUserAsync(userId, workDate, cancellationToken);
        var existingSummaryForDate = await _paySummaryRepository.GetByDateAndUserAsync(userId, workDate, cancellationToken);

        // remove summary when no shifts detected
        if (!shiftsForDate.Any()) {
          if (existingSummaryForDate != null) {
            await _paySummaryRepository.RemoveAsync(existingSummaryForDate, cancellationToken);
          }
          continue;
        }

        // recalculate summary when shifts detected
        var shiftsToCalculate = new List<ShiftDto>();
        shiftsToCalculate.AddRange(shiftsForDate.Select(s => new ShiftDto {
          StartAt = s.StartAt,
          EndAt = s.EndAt,
          UnpaidBreakMinutes = s.UnpaidBreakMinutes,
          EmploymentType = s.EmploymentType,
          EntryType = s.EntryType
        }));
        var calculatedDailySummary = CalculateDailyPay(shiftsToCalculate, userId);

        // Update existing summary or create new summary
        var summary = existingSummaryForDate ?? new UserDailyPaySummary {
          UserId = userId,
        };

        summary.WorkDate = calculatedDailySummary.WorkDate;
        summary.BaseRateUsed = calculatedDailySummary.BaseRateUsed;
        summary.GrossPay = calculatedDailySummary.GrossPay;
        summary.TotalEveningPenaltyMinutes = calculatedDailySummary.TotalEveningPenaltyMinutes;
        summary.TotalOvertimeMinutes = calculatedDailySummary.TotalOvertimeMinutes;
        summary.TotalPayableMinutes = calculatedDailySummary.TotalPayableMinutes;
        summary.TotalUnpaidBreakMinutes = calculatedDailySummary.TotalUnpaidBreakMinutes;
        summary.CalculatedAtUtc = DateTimeOffset.UtcNow;

        // attach summaryId to shift
        foreach (var shift in shiftsForDate) {
          shift.UserDailyPaySummaryId = summary.Id;
        }

        if (existingSummaryForDate == null) {
          await _paySummaryRepository.AddAsync(summary, cancellationToken);
        }
      }

    }

  }
}
