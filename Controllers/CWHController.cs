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
    public async Task<IActionResult> handleReport([FromBody] CWHReportRequestDTOs CWHReportRequest) {

      // process the request and make a report
      // process the deliveryScreenshot
      // send the report to telegram


      return Ok();
    }

  }
}
