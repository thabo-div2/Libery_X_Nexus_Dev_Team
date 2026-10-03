using API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>
    /// Controller for managing market information-related operations. Provides endpoints to retrieve market information based on user queries.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MarketInformationController : ControllerBase
    {
        private readonly IMarketInformationService _marketInformationService;

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Initializes a new instance of the <see cref="MarketInformationController"/> class with the specified market information service.
        /// </summary>
        /// <param name="marketInformationService"></param>
        public MarketInformationController(IMarketInformationService marketInformationService)
        {
            _marketInformationService = marketInformationService;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Retrieves market information based on the provided query string. 
        /// The query string should contain a market-related question or topic. 
        /// If the query is empty or null, a bad request response is returned. 
        /// If the service is unavailable, a 503 Service Unavailable response is returned.
        /// </summary>
        /// <param name="q"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
