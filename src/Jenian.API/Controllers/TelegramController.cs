using Jenian.API.Contracts.Common;
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
      var msg = update.Message;
      if (msg == null) {
        _logger.LogInformation("Telegram webhook: update has no message.");
        return Ok();
      }
      _logger.LogInformation("Telegram webhook: chat_id={ChatId} at {UtcNow}", msg.Chat?.Id, DateTime.UtcNow);

      var chatId = msg?.Chat?.Id ?? msg?.From?.Id ?? 0;
      if (chatId == 0) {
        _logger.LogInformation("Telegram webhook: cannot resolve chatId.");
        return Ok();
      }

      var cts = new CancellationTokenSource();
      await _telegramService.HandleUpdateAsync(update, cts.Token);
      //_latestRequestRunner.StartOrRestart(chatId, (sp, ct) => {
      //  // Resolve TelegramService inside THIS scope:
      //  var svc = sp.GetRequiredService<ITelegramService>();

      //  // 'update' is captured from the controller method
      //  return svc.HandleUpdateAsync(update, ct);
      //});

      return Ok(); // Must return 200 or Telegram will retry
    }

    [Authorize]
    [HttpGet("link-token")]
    public async Task<IActionResult> GenerateTelegramLinkToken() {
      // Get userId from accessToken
      var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
      var claimValue = User.FindFirst("IsDemoUser")?.Value;
      var isDemoUser = bool.TryParse(claimValue, out var parsedValue)
    && parsedValue;
      if (userId == null) return NotFound(ApiResponse<object>.Fail(["Cannot find user information."]));

      // find user by userId
      var user = await _userManager.FindByIdAsync(userId);
      if (user == null) return NotFound(ApiResponse<object>.Fail(["Cannot find user information."]));

      // update telegramUserId to someValue if isDemoUser is true
      if (isDemoUser) {
        user.TelegramUserId = "DemoTelegramUserID";
        await _userManager.UpdateAsync(user);
        return Ok(ApiResponse<object>.Ok(new { linkToken = "demo-token" }));
      }

      // Check if user has linkToken
      if (string.IsNullOrEmpty(user.TelegramLinkToken)) {
        user.TelegramLinkToken = Guid.NewGuid().ToString();
        await _userManager.UpdateAsync(user);

      }

      return Ok(ApiResponse<object>.Ok(new { linkToken = user.TelegramLinkToken }));
    }

    // Can you this function in to check if user linked before adding token
    [Authorize]
    [HttpGet("is-linked")]
    public async Task<IActionResult> CheckIfTelegramLinked() {
      var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
      if (userId == null) return NotFound(ApiResponse<object>.Fail(["Cannot find user information."]));

      var user = await _userManager.FindByIdAsync(userId);
      if (user == null) return NotFound(ApiResponse<object>.Fail(["Cannot find user information."]));

      return Ok(ApiResponse<object>.Ok(new { isLinked = !string.IsNullOrEmpty(user.TelegramUserId) }));
    }


  }
}
