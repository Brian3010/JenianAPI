using Jenian.Infrastructure.Persistence.App;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jenian.API.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class HomeController : ControllerBase
  {



    [HttpGet("health")]
    public IActionResult Get() {
      return Ok("Welcome to Jenian API!");
    }

  }
}
