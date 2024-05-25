using Microsoft.AspNetCore.Mvc;

namespace Hikaria.Core.WebAPI.Controllers
{
    [Route("api/gtfo/[controller]/[action]")]
    [ApiController]
    public class AliveController : ControllerBase
    {
        private readonly ILogger<AliveController> _logger;

        public AliveController(ILogger<AliveController> logger)
        {
            _logger = logger;
        }


        [HttpGet]
        public IActionResult CheckAlive()
        {
            return Ok(true);
        }
    }
}
