using Jenian.Application.Abstractions.Persistence;
using Jenian.Application.Common.Exceptions;
using Jenian.Domain.Entities;
using Jenian.Infrastructure.Persistence.App;
using Microsoft.Data.SqlClient;
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

    private static DateTimeOffset ToDateTimeOffsetStartOfDay(
    DateOnly date,
    string timeZoneId) {
      var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

      var localDateTime = date.ToDateTime(TimeOnly.MinValue);

      var offset = timeZone.GetUtcOffset(localDateTime);

      return new DateTimeOffset(localDateTime, offset);
    }

    public async Task<IEnumerable<UserShift>> GetByIdsAndRangeAsync(string userId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default) {
      const string timeZoneId = "Australia/Melbourne";
      var fromDateTimeOffset = ToDateTimeOffsetStartOfDay(from, timeZoneId);

      // Add 1 day because the upper boundary should usually be exclusive.
      var toDateTimeOffset = ToDateTimeOffsetStartOfDay(to.AddDays(1), timeZoneId);
      var shifts = await _dbContext.UserShifts
       .Where(x => x.UserId == userId)
       .Where(x => x.StartAt >= fromDateTimeOffset && x.StartAt < toDateTimeOffset).OrderBy(x => x.StartAt)
       .ToListAsync(cancellationToken);
      return shifts;
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

    private static bool IsDuplicateShiftViolation(DbUpdateException exception) {
      return exception.InnerException is SqlException sqlException &&
             (sqlException.Number == 2601 || sqlException.Number == 2627) &&
             sqlException.Message.Contains("IX_UserShifts_UserId_StartAt_EndAt");
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) {
      try {
        return await _dbContext.SaveChangesAsync(cancellationToken);
      } catch (DbUpdateException ex) when (IsDuplicateShiftViolation(ex)) {
        throw new DuplicateShiftException();
      }
    }

    public Task UpdateAsync(UserShift userShift, CancellationToken cancellationToken = default) {
      throw new NotImplementedException();
    }

    public async Task<IEnumerable<UserShift>> GetByIdsForUserAsync(string userId, IEnumerable<Guid> shiftIds, CancellationToken cancellationToken = default) {
      return await _dbContext.UserShifts.Where(s => s.UserId == userId && shiftIds.Contains(s.Id)).ToListAsync(cancellationToken);
    }
  }
}
