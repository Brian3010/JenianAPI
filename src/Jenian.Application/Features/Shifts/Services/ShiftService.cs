using Jenian.Application.Abstractions.Persistence;
using Jenian.Application.Common;
using Jenian.Application.Features.Payroll;
using Jenian.Application.Features.PaySummaries.Dtos;
using Jenian.Application.Features.PaySummaries.Services;
using Jenian.Application.Features.Shifts.Commands;
using Jenian.Application.Features.Shifts.Dtos;
using Jenian.Application.Features.Shifts.Validations;
using Jenian.Domain.Entities;

namespace Jenian.Application.Features.Shifts.Services
{
  public class ShiftService : IShiftService
  {
    private readonly IShiftRepository _shiftRepository;
    private readonly IShiftValidator _shiftValidator;
    private readonly IShiftMutationService _shiftMutationService;
    private readonly IPayCalculationService _payCalculationService;
    private readonly IPaySummaryRepository _paySummaryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPayCalculator _payCalculator;

    public ShiftService(
      IShiftRepository shiftRepository,
      IShiftValidator shiftValidator,
      IShiftMutationService shiftMutationService,
      IPayCalculationService payCalculationService,
      IPaySummaryRepository paySummaryRepository,
      IUnitOfWork unitOfWork,
      IPayCalculator payCalculator

      ) {
      _shiftRepository = shiftRepository;
      _shiftValidator = shiftValidator;
      _shiftMutationService = shiftMutationService;
      _payCalculationService = payCalculationService;
      _paySummaryRepository = paySummaryRepository;
      _unitOfWork = unitOfWork;
      _payCalculator = payCalculator;
    }

    /* Shift Management */
    public async Task<ServiceResult<ShiftSummaryResult>> GetShiftsByUserAndDateRangeAsync(GetShiftsForUserByDateRangeCommand command, CancellationToken cancellationToken) {

      // validate the payUserCycle param, making sure it match the user's pay cycle settings
      var userPaySetting = await _shiftRepository.GetPayCycleSettingByUserIdAsync(command.UserId, cancellationToken);
      if (userPaySetting == null) {
        return ServiceResult<ShiftSummaryResult>.Failure(
                [$"User has not set up pay cycle settings."]);
      }

      var calculatedCycle = _payCalculator.CalculatePayCycleDateRange(userPaySetting.PayCycleType, userPaySetting.AnchorStartDate);
      if (calculatedCycle == null) return ServiceResult<ShiftSummaryResult>.Failure(["Failed to calculate pay cycle."]);


      var shifts = await _shiftRepository.GetByIdsAndRangeAsync(command.UserId, calculatedCycle.StartDate, calculatedCycle.EndDate, cancellationToken);
      var summaries = await _paySummaryRepository.GetByIdAndRangeAsync(command.UserId, calculatedCycle.StartDate, calculatedCycle.EndDate, cancellationToken);


      return ServiceResult<ShiftSummaryResult>.Success(new ShiftSummaryResult {
        Shifts = shifts.Select(shift => new ShiftDto {
          Id = shift.Id,
          StartAt = shift.StartAt,
          EndAt = shift.EndAt,
          TimeZoneId = shift.TimeZoneId,
          UnpaidBreakMinutes = shift.UnpaidBreakMinutes,
          PaidBreakMinutes = shift.PaidBreakMinutes,
          EntryType = shift.EntryType,
          EmploymentType = shift.EmploymentType,
          Source = shift.Source
        }),
        DailySummaries = summaries.Select(summary => new UserDailyPaySummaryDto {
          UserId = summary.UserId,
          WorkDate = summary.WorkDate,
          BaseRateUsed = summary.BaseRateUsed,
          GrossPay = summary.GrossPay,
          TotalEveningPenaltyMinutes = summary.TotalEveningPenaltyMinutes,
          TotalOvertimeMinutes = summary.TotalOvertimeMinutes,
          TotalPayableMinutes = summary.TotalPayableMinutes,
          TotalPaidBreakMinutes = summary.TotalPaidBreakMinutes,
          TotalUnpaidBreakMinutes = summary.TotalUnpaidBreakMinutes
        })
      });


    }

    // Save shift changes and their derived daily summaries as one atomic operation.
    public async Task<ServiceResult<ShiftSummaryResult>> SaveShiftsAsync(SaveShiftsCommand command, CancellationToken cancellationToken) {
      var validationResult = _shiftValidator.ValidateSaveShifts(command.ShiftDtos, command.RangeStartDate, command.RangeEndDate);
      if (!validationResult.IsValid) {
        return ServiceResult<ShiftSummaryResult>.Failure(validationResult.Errors);
      }

      return await _unitOfWork.ExecuteInTransactionAsync(async transactionCancellationToken => {
        var mutationResult = await _shiftMutationService.ApplyAsync(
          command,
          transactionCancellationToken);
        if (!mutationResult.IsSuccess || mutationResult.Data == null) {
          return ServiceResult<ShiftSummaryResult>.Failure(mutationResult.Errors);
        }

        // The recalculation queries the database, so shift changes must be flushed
        // first. The enclosing transaction keeps both flushes atomic.
        await _unitOfWork.SaveChangesAsync(transactionCancellationToken);
        await _payCalculationService.RecalculateForDatesAsync(
          command.UserId,
          mutationResult.Data.AffectedWorkDates,
          transactionCancellationToken);
        await _unitOfWork.SaveChangesAsync(transactionCancellationToken);

        var shifts = await _shiftRepository.GetByIdsAndRangeAsync(
          command.UserId,
          command.RangeStartDate,
          command.RangeEndDate,
          transactionCancellationToken);
        var summaries = await _paySummaryRepository.GetByIdAndRangeAsync(
          command.UserId,
          command.RangeStartDate,
          command.RangeEndDate,
          transactionCancellationToken);

        return ServiceResult<ShiftSummaryResult>.Success(new ShiftSummaryResult {
          Shifts = shifts.Select(shift => new ShiftDto {
            Id = shift.Id,
            StartAt = shift.StartAt,
            EndAt = shift.EndAt,
            TimeZoneId = shift.TimeZoneId,
            UnpaidBreakMinutes = shift.UnpaidBreakMinutes,
            PaidBreakMinutes = shift.PaidBreakMinutes,
            EntryType = shift.EntryType,
            EmploymentType = shift.EmploymentType,
            Source = shift.Source
          }).ToList(),
          DailySummaries = summaries.Select(summary => new UserDailyPaySummaryDto {
            UserId = summary.UserId,
            WorkDate = summary.WorkDate,
            BaseRateUsed = summary.BaseRateUsed,
            GrossPay = summary.GrossPay,
            TotalEveningPenaltyMinutes = summary.TotalEveningPenaltyMinutes,
            TotalOvertimeMinutes = summary.TotalOvertimeMinutes,
            TotalPayableMinutes = summary.TotalPayableMinutes,
            TotalPaidBreakMinutes = summary.TotalPaidBreakMinutes,
            TotalUnpaidBreakMinutes = summary.TotalUnpaidBreakMinutes
          }).ToList()
        });
      }, cancellationToken);
    }


    /* Pay Cycle Settings */

    public async Task<ServiceResult<PayCycleSettingsDto>> UpdatePayCycleSettingsForUserAsync(CreatePayCycleSettingsCommand command, CancellationToken cancellationToken) {
      var payCycleSetting = new PayCycleSetting {
        UserId = command.UserId,
        AnchorStartDate = command.AnchorStartDate,
        PayCycleType = (PayCycleType)command.PayCycleType
      };

      await _shiftRepository.UpdatePayCycleSettingsForUserAsync(command.UserId, payCycleSetting, cancellationToken);

      await _unitOfWork.SaveChangesAsync(cancellationToken);

      return ServiceResult<PayCycleSettingsDto>.Success(new PayCycleSettingsDto {
        HasPayCycleSettings = true,
        AnchorStartDate = payCycleSetting.AnchorStartDate,
        PayCycle = (PayCycleTypeDTO)payCycleSetting.PayCycleType
      });
    }



    public async Task<ServiceResult<PayCycleSettingsDto>> GetCurrentPayCycleSettingsForUserAsync(string userId, CancellationToken cancellationToken) {

      var userPayCycle = await _shiftRepository.GetPayCycleSettingByUserIdAsync(userId, cancellationToken);

      if (userPayCycle == null) {
        return ServiceResult<PayCycleSettingsDto>.Success(new PayCycleSettingsDto {
          HasPayCycleSettings = false,
        });
      }

      var todayDate = DateOnly.FromDateTime(DateTime.UtcNow);
      DateOnly cycleStartDate;
      DateOnly cycleEndDate;
      switch (userPayCycle.PayCycleType) {
        case PayCycleType.Weekly:
          var daysSinceWeeklyAnchor = todayDate.DayNumber - userPayCycle.AnchorStartDate.DayNumber;
          var weeklyIndex = Math.Floor(daysSinceWeeklyAnchor / 7.0);
          var currentWeekStart = userPayCycle.AnchorStartDate.AddDays((int)weeklyIndex * 7);

          cycleStartDate = currentWeekStart;
          cycleEndDate = currentWeekStart.AddDays(6);
          break;

        case PayCycleType.Fortnightly:
          var daysSinceFortnightlyAnchor = todayDate.DayNumber - userPayCycle.AnchorStartDate.DayNumber;
          var fortnightIndex = Math.Floor(daysSinceFortnightlyAnchor / 14.0);
          var currentFortnightStart = userPayCycle.AnchorStartDate.AddDays((int)fortnightIndex * 14);

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
          throw new ArgumentOutOfRangeException(nameof(userPayCycle.PayCycleType));
      }

      // when user has set up pay cycle settings
      // TODO: these 2 callers can be optimized by combining them into a single query to reduce database calls
      var paySummary = await _paySummaryRepository.GetByIdAndRangeAsync(userId, cycleStartDate, cycleEndDate, cancellationToken);
      var shifts = await _shiftRepository.GetByIdsAndRangeAsync(userId, cycleStartDate, cycleEndDate, cancellationToken);

      return ServiceResult<PayCycleSettingsDto>.Success(new PayCycleSettingsDto {
        HasPayCycleSettings = true,
        AnchorStartDate = userPayCycle.AnchorStartDate,
        PayCycle = (PayCycleTypeDTO)userPayCycle.PayCycleType,
        PayCycleStartDate = cycleStartDate,
        PayCycleEndDate = cycleEndDate,
        EstimatedGrossPay = paySummary.Sum(summary => summary.GrossPay),
        ShiftCountInCycle = shifts.Count()
      });

    }

  }
}
