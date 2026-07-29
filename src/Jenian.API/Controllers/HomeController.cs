using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jenian.API.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class HomeController : ControllerBase
  {



    [HttpGet("health")]
    [DisableRateLimiting]
    public IActionResult Get() {
      return NoContent();
    }

  }
}
