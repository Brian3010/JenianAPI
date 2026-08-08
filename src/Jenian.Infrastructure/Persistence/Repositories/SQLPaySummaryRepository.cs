using Jenian.Application.Abstractions.Persistence;
using Jenian.Application.Common;
using Jenian.Domain.Entities;
using Jenian.Infrastructure.Persistence.App;
using Microsoft.EntityFrameworkCore;

namespace Jenian.Infrastructure.Persistence.Repositories
{
  public class SQLPaySummaryRepository : IPaySummaryRepository
  {
    private readonly JenianDbContext _dbContext;

    public SQLPaySummaryRepository(
      JenianDbContext dbContext
      ) {
      _dbContext = dbContext;
    }
    public async Task AddAsync(UserDailyPaySummary summary, CancellationToken cancellationToken = default) {
      await _dbContext.AddAsync(summary, cancellationToken);
    }

    public async Task<UserDailyPaySummary?> GetByDateAndUserAsync(string userId, DateOnly workDate, CancellationToken cancellationToken = default) {

      return await _dbContext.UserDailyPaySummaries.FirstOrDefaultAsync(s => s.UserId == userId && s.WorkDate == workDate, cancellationToken);

    }

    public async Task<IEnumerable<UserDailyPaySummary>> GetByIdAndRangeAsync(string userId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default) {
      var fromDateTimeOffSet = DateOnly.FromDateTime(ShiftDateHelper.ToDateTimeOffsetStartOfDay(from, "Australia/Melbourne").UtcDateTime);
      // Add 1 day because the upper boundary should usually be exclusive.
      var toDateTimeOffSet = DateOnly.FromDateTime(ShiftDateHelper.ToDateTimeOffsetStartOfDay(to.AddDays(1), "Australia/Melbourne").UtcDateTime);
      return await _dbContext.UserDailyPaySummaries
        .Where(s => s.UserId == userId && s.WorkDate >= fromDateTimeOffSet && s.WorkDate <= toDateTimeOffSet)
        .AsNoTracking()
        .ToListAsync(cancellationToken);
    }

    public async Task RemoveAsync(UserDailyPaySummary summary, CancellationToken cancellationToken = default) {
      await _dbContext.UserDailyPaySummaries.Where(s => s.Id == summary.Id).ExecuteDeleteAsync(cancellationToken);
    }

    public async Task RemoveByUserIdAsync(string userId, CancellationToken cancellationToken = default) {
      await _dbContext.UserDailyPaySummaries.Where(s => s.UserId == userId).ExecuteDeleteAsync(cancellationToken);
    }
  }
}
