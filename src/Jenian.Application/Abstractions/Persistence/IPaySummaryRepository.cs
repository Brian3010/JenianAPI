using Jenian.Domain.Entities;

namespace Jenian.Application.Abstractions.Persistence
{
  public interface IPaySummaryRepository
  {
    Task<UserDailyPaySummary?> GetByDateAndUserAsync(string userId, DateOnly workDate, CancellationToken cancellationToken = default);
    Task RemoveAsync(UserDailyPaySummary summary, CancellationToken cancellationToken = default);
    Task AddAsync(UserDailyPaySummary summary, CancellationToken cancellationToken = default);


    Task RemoveByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    Task<IEnumerable<UserDailyPaySummary>> GetByIdAndRangeAsync(string userId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
  }
}
