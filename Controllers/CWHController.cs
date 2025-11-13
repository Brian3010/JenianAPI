using JenianAPI.Dtos.CwhDtos;
using Microsoft.AspNetCore.Mvc;

namespace JenianAPI.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class CWHController : ControllerBase
  {
    private readonly ILogger<CWHController> _logger;

    public CWHController(ILogger<CWHController> logger) {
      _logger = logger;
    }



    [HttpPost("eod-report")]
    public async Task<IActionResult> HandleReport([FromBody] CWHReportRequestDTOs CWHReportRequest) {
      if (CWHReportRequest == null) {
        return BadRequest("Invalid report data.");
      }
      // process the StockUpdate - process the DeliveryScreenShots


      // process the NightTasks
      // process the AislesFacing
      // process the Cleaning
      // process the GeneralCheck

      return Ok();
    }

  }
}
