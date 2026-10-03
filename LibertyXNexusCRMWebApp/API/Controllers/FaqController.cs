using API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>
    /// Controller for managing Frequently Asked Questions (FAQs). Provides endpoints to retrieve all FAQs and search for specific FAQs based on a query string.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FaqController : ControllerBase
    {
        private readonly IFaqService _faqService;

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Initializes a new instance of the <see cref="FaqController"/> class with the specified FAQ service.
        /// </summary>
        /// <param name="faqService"></param>
        public FaqController(IFaqService faqService)
        {
            _faqService = faqService;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Retrieves all frequently asked questions (FAQs) from the system. This endpoint is accessible to authenticated users and returns a list of FAQs in the response.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var faqs = await _faqService.GetAllAsync();
            return Ok(faqs);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Searches for frequently asked questions (FAQs) based on the provided query string. This endpoint is accessible to authenticated users and returns a list of matching FAQs in the response.
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string q)
        {
            var results = await _faqService.SearchAsync(q);
            return Ok(results);
        }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
