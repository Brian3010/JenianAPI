using Jenian.Application.Abstractions.Persistence;
using Jenian.Infrastructure.Persistence.Auth;
using Microsoft.EntityFrameworkCore;

namespace Jenian.Infrastructure.Persistence.Repositories
{
  public class SQLRefreshTokenRepository : IRefreshTokenRepository
  {
    private readonly JenianAuthDbContext _dbAuthContext;

    public SQLRefreshTokenRepository(JenianAuthDbContext dbAuthContext) {
      _dbAuthContext = dbAuthContext;
    }
    public async Task RemoveByUserIdAsync(string userId, CancellationToken cancellationToken = default) {
      await _dbAuthContext.RefreshTokens
        .Where(rt => rt.UserId == userId)
        .ExecuteDeleteAsync(cancellationToken);

    }
  }
}
