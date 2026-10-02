using API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MarketInformationController : ControllerBase
    {
        private readonly IMarketInformationService _marketInformationService;

        public MarketInformationController(IMarketInformationService marketInformationService)
        {
            _marketInformationService = marketInformationService;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string q, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return BadRequest("A market information question is required.");
            }

            try
            {
                var result = await _marketInformationService.GetAsync(q, cancellationToken);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, ex.Message);
            }
        }
    }
}
