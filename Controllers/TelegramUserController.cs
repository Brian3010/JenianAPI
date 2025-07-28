using JenianAPI.Models.AuthModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JenianAPI.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class TelegramUserController : ControllerBase
  {
    private readonly UserManager<ApplicationUser> _userManager;

    public TelegramUserController(UserManager<ApplicationUser> userManager) {
      _userManager = userManager;
    }


    [HttpGet("link-token")]
    public async Task<IActionResult> GenerateTelegramLinkToken() {
      // Get userId from accessToken
      var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
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


    [HttpGet("is-linked")]
    public async Task<IActionResult> CheckIfTelegramLinked() {
      var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
      if (userId == null) return NotFound("Cannot Find User Information");

      var user = await _userManager.FindByIdAsync(userId);
      if (user == null) return NotFound("Cannot Find User Information");

      return Ok(new { isLinked = !string.IsNullOrEmpty(user.TelegramUserId) });
    }




  }
}
