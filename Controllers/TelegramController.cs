using JenianAPI.Dtos.TelegramDtos;
using JenianAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace JenianAPI.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class TelegramController : ControllerBase
  {
    private readonly TelegramService _telegramService;
    private readonly ILogger<TelegramController> _logger;

    public TelegramController(TelegramService telegramService, ILogger<TelegramController> logger) {
      _telegramService = telegramService;
      _logger = logger;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromBody] TelegramUpdate update) {
      _logger.LogInformation("Webhook hit from Telegram!");
      await _telegramService.HandleUpdateAsync(update);
      return Ok(); // Must return 200 or Telegram will retry
    }

  }
}
