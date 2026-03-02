using JenianAPI.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JenianAPI.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class HomeController : ControllerBase
  {



    [HttpGet("health")]
    public IActionResult Get() {
      return Ok("Welcome to Jenian API!");
    }


    [HttpGet("sqlDb-test")]
    public async Task<IActionResult> GetSqlDbConnection(JenianDbContext dbContext) {
      try {
        var canConnect = await dbContext.Database.CanConnectAsync();
        return Ok(new { DatabaseConnected = canConnect });
      } catch (Exception ex) {

        return Problem(ex.Message);
      }
    }


  }
}
