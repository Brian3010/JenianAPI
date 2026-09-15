using Jenian.Application.Abstractions.Persistence;
using Jenian.Application.Common;
using Jenian.Domain.Entities;
using Jenian.Infrastructure.Persistence.App;
using Microsoft.EntityFrameworkCore;

namespace Jenian.Infrastructure.Persistence.Repositories
{
  public class SQLShiftRepository : IShiftRepository
  {
    private readonly JenianDbContext _dbContext;
    private readonly ILogger<SQLShiftRepository> _logger;

    public SQLShiftRepository(JenianDbContext dbContext, ILogger<SQLShiftRepository> logger) {
      _dbContext = dbContext;
      _logger = logger;
    }
    public Task AddAsync(UserShift userShift, CancellationToken cancellationToken = default) {
      throw new NotImplementedException();
    }

    public async Task UpdatePayCycleSettingsForUserAsync(string userId, PayCycleSetting payCycleSetting, CancellationToken cancellationToken = default) {
      var existingSetting = await _dbContext.PayCycleSettings.FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
      if (existingSetting != null) {
        existingSetting.PayCycleType = payCycleSetting.PayCycleType;
        existingSetting.AnchorStartDate = payCycleSetting.AnchorStartDate;
        existingSetting.UpdatedAtUtc = DateTimeOffset.UtcNow;

      } else {
        await _dbContext.PayCycleSettings.AddAsync(payCycleSetting, cancellationToken);
      }
    }

    public async Task AddRangeAsync(IEnumerable<UserShift> shifts, CancellationToken cancellationToken = default) {
      await _dbContext.UserShifts.AddRangeAsync(shifts, cancellationToken);
    }

    public Task<UserShift?> GetByIdForUserAsync(string userId, Guid shiftId, CancellationToken cancellationToken = default) {
      throw new NotImplementedException();
    }

    public async Task<IEnumerable<UserShift>> GetByIdsAndRangeAsync(string userId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default) {
      var candidates = await GetRangeCandidatesAsync(userId, from, to, cancellationToken);

      return candidates
        .Where(shift => ShiftDateHelper.IsWorkDateInRange(
          shift.StartAt,
          shift.TimeZoneId,
          from,
          to))
        .OrderBy(shift => shift.StartAt)
        .ToList();
    }

    public async Task<int> CountByUserAndRangeAsync(string userId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default) {
      var candidates = await GetRangeCandidatesAsync(userId, from, to, cancellationToken);

      return candidates.Count(shift => ShiftDateHelper.IsWorkDateInRange(
        shift.StartAt,
        shift.TimeZoneId,
        from,
        to));
    }

    public Task<IEnumerable<UserShift>> GetByUserAndDateRangeAsync(string userId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default) {
      throw new NotImplementedException();
    }

    public async Task<PayCycleSetting?> GetPayCycleSettingByUserIdAsync(string userId, CancellationToken cancellationToken = default) {
      return await _dbContext.PayCycleSettings.FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
    }

    public Task RemoveByIdForUserAsync(string userId, Guid shiftId, CancellationToken cancellationToken = default) {
      throw new NotImplementedException();
    }

    public async Task RemoveByIdsForUserAsync(string userId, IEnumerable<Guid> shiftIds, CancellationToken cancellationToken = default) {
      await _dbContext.UserShifts.Where(s => s.UserId == userId && shiftIds.Contains(s.Id)).ExecuteDeleteAsync(cancellationToken);
    }

    //private static bool IsDuplicateShiftViolation(DbUpdateException exception) {
    //  return exception.InnerException is SqlException sqlException &&
    //         (sqlException.Number == 2601 || sqlException.Number == 2627) &&
    //         sqlException.Message.Contains("IX_UserShifts_UserId_StartAt_EndAt");
    //}

    //public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) {
    //  try {
    //    return await _dbContext.SaveChangesAsync(cancellationToken);
    //  } catch (DbUpdateException ex) when (IsDuplicateShiftViolation(ex)) {
    //    throw new DuplicateShiftException();
    //  }
    //}

    public Task UpdateAsync(UserShift userShift, CancellationToken cancellationToken = default) {
      throw new NotImplementedException();
    }

    public async Task<IEnumerable<UserShift>> GetByIdsForUserAsync(string userId, IEnumerable<Guid> shiftIds, CancellationToken cancellationToken = default) {
      return await _dbContext.UserShifts.Where(s => s.UserId == userId && shiftIds.Contains(s.Id)).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<UserShift>> GetByDateAndUserAsync(string userId, DateOnly date, CancellationToken cancellationToken = default) {
      var candidates = await GetRangeCandidatesAsync(userId, date, date, cancellationToken);

      return candidates
        .Where(shift => ShiftDateHelper.GetWorkDate(shift.StartAt, shift.TimeZoneId) == date)
        .ToList();
    }

    public async Task RemoveShiftsByUserIdAsync(string userId, CancellationToken cancellationToken = default) {
      await _dbContext.UserShifts.Where(s => s.UserId == userId).ExecuteDeleteAsync(cancellationToken);
    }

    public async Task RemovePayCycleSettingsByUserIdAsync(string userId, CancellationToken cancellationToken = default) {
      await _dbContext.PayCycleSettings.Where(s => s.UserId == userId).ExecuteDeleteAsync(cancellationToken);
    }

    private async Task<List<UserShift>> GetRangeCandidatesAsync(
      string userId,
      DateOnly from,
      DateOnly to,
      CancellationToken cancellationToken) {
      var (fromUtc, toUtc) = ShiftDateHelper.GetUtcCandidateRange(from, to);

      return await _dbContext.UserShifts
        .Where(shift => shift.UserId == userId)
        .Where(shift => shift.StartAt >= fromUtc && shift.StartAt < toUtc)
        .ToListAsync(cancellationToken);
    }
  }
}
