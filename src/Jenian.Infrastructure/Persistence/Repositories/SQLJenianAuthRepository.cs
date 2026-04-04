using Jenian.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Jenian.Infrastructure.Persistence.Repositories
{
  public class SQLJenianAuthRepository
  {
    private readonly ILogger<SQLJenianAuthRepository> _logger;
    private readonly UserManager<ApplicationUser> _userManager;


    public SQLJenianAuthRepository(UserManager<ApplicationUser> userManager, ILogger<SQLJenianAuthRepository> logger) {
      _userManager = userManager;
      _logger = logger;
    }

    public async Task<bool> IsTelegramConnectedAsync(string userId) {
      var userConnectedToTelegram = await _userManager.Users
        .AsNoTracking()
        .Where(u => u.Id == userId)
        .Select(u => u.TelegramUserId)
        .SingleOrDefaultAsync();
      if (userConnectedToTelegram == null) return false;
      return true;
    }

  }
}
