using Jenian.Application.Abstractions.Messaging;
using Jenian.Application.Features.Telegram.Dtos;
using Jenian.Infrastructure.Concurrency;
using Jenian.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace Jenian.API.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class TelegramController : ControllerBase
  {
    private readonly ITelegramService _telegramService;
    private readonly ILogger<TelegramController> _logger;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly LatestRequestRunner _latestRequestRunner;

    public TelegramController(ITelegramService telegramService, ILogger<TelegramController> logger, UserManager<ApplicationUser> userManager, LatestRequestRunner latestRequestRunner) {
      _telegramService = telegramService;
      _logger = logger;
      _userManager = userManager;
      _latestRequestRunner = latestRequestRunner;
    }

    /* This APIs get hooked to Telegram via
     * curl -X POST "https://api.telegram.org/bot{Telegeram Token}/setWebhook"
     * -d "url=https:{url}/api/telegram/webhook"
     */
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromBody] TelegramUpdate update) {
      _logger.LogInformation("Webhook hit from Telegram!");
      _logger.LogInformation(
    "Telegram webhook: update_id={UpdateId}  at {UtcNow}",
    update.Message!.Chat!.Id, DateTime.UtcNow);
      var msg = update.Message;
      if (msg == null) {
        _logger.LogInformation("Telegram webhook: update has no message.");
        return Ok();
      }

      var chatId = msg?.Chat?.Id ?? msg?.From?.Id ?? 0;
      if (chatId == 0) {
        _logger.LogInformation("Telegram webhook: cannot resolve chatId.");
        return Ok();
      }

      _latestRequestRunner.StartOrRestart(chatId, (sp, ct) => {
        // Resolve TelegramService inside THIS scope:
        var svc = sp.GetRequiredService<ITelegramService>();

        // 'update' is captured from the controller method
        return svc.HandleUpdateAsync(update, ct);
      });

      return Ok(); // Must return 200 or Telegram will retry
    }

    [Authorize]
    [HttpGet("link-token")]
    public async Task<IActionResult> GenerateTelegramLinkToken() {
      // Get userId from accessToken
      var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
      if (userId == null) return NotFound("Cannot Find User Information");

      // find user by userId
      var user = await _userManager.FindByIdAsync(userId);
      if (user == null) return NotFound("Cannot Find User Information");

      // Check if user has linkToken
      if (string.IsNullOrEmpty(user.TelegramLinkToken)) {
        user.TelegramLinkToken = Guid.NewGuid().ToString();
        await _userManager.UpdateAsync(user);

      }

      return Ok(new { linkToken = user.TelegramLinkToken });
    }

    // Can you this function in to check if user linked before adding token
    [Authorize]
    [HttpGet("is-linked")]
    public async Task<IActionResult> CheckIfTelegramLinked() {
      var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
      if (userId == null) return NotFound("Cannot Find User Information");

      var user = await _userManager.FindByIdAsync(userId);
      if (user == null) return NotFound("Cannot Find User Information");

      return Ok(new { isLinked = !string.IsNullOrEmpty(user.TelegramUserId) });
    }


  }
}
