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
  }
}
