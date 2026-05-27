using Jenian.Application.Abstractions.Persistence;
using Jenian.Application.Features.Payroll;
using Jenian.Application.Features.PaySummaries.Dtos;
using Jenian.Application.Features.Shifts.Dtos;
using Jenian.Domain.Entities;

namespace Jenian.Application.Features.PaySummaries.Services
{
  public class PayCalculationService : IPayCalculationService
  {
    private readonly IPaySummaryRepository _paySummaryRepository;
    private readonly IShiftRepository _shiftRepository;
    private readonly IPayCalculator _payCalculator;

    public PayCalculationService(
      IPaySummaryRepository paySummaryRepository,
      IShiftRepository shiftRepository,
      IPayCalculator payCalculator
      ) {
      _paySummaryRepository = paySummaryRepository;
      _shiftRepository = shiftRepository;
      _payCalculator = payCalculator;
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
        var calculatedDailySummary = _payCalculator.CalculateDailyPay(shiftsToCalculate, userId);

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
