using Jenian.Application.Abstractions.Persistence;
using Jenian.Infrastructure.Persistence.App;

namespace Jenian.Infrastructure.Persistence
{
  public class UnitOfWork : IUnitOfWork
  {
    private readonly JenianDbContext _dbContext;

    public UnitOfWork(JenianDbContext dbContext) {
      _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) {
      return _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<T> ExecuteInTransactionAsync<T>(
      Func<CancellationToken, Task<T>> operation,
      CancellationToken cancellationToken = default) {
      await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

      try {
        var result = await operation(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
      } catch {
        await transaction.RollbackAsync(CancellationToken.None);
        throw;
      }
    }
  }
}
