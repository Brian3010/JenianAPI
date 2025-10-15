using Microsoft.AspNetCore.Mvc;

namespace JenianAPI.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class ReportAssistantController : ControllerBase
  {
    private readonly ILogger<ReportAssistantController> _logger;

    public ReportAssistantController(ILogger<ReportAssistantController> logger) {
      _logger = logger;
    }







  }
}
