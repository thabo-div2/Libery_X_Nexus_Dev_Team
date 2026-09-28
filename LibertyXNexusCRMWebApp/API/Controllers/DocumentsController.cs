using API.DTOs.Documents;
using API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Shared.Models.Enums;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[Controller]")]
    public class DocumentsController : ControllerBase
    {
        private readonly IDocumentService documentService_;

        public DocumentsController(IDocumentService documentService)
        {
            documentService_ = documentService;
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<DocumentDto>> GetById(int id)
        {
            var document = await documentService_.GetByIdAsync(id);
            return document is null ? NotFound(new{message = $"Document {id} was not found" }) : Ok(document);
        }

        [HttpGet("client/{clientId:int}")]
        public async Task<ActionResult<IEnumerable<DocumentDto>>> GetForClient(int clientId)
        {
            return Ok(await documentService_.GetForClientAsync(clientId));
        }

        [HttpGet("client/{clientId:int}/visible")]
        public async Task<ActionResult<IEnumerable<DocumentDto>>> GetVisibleToClient(int clientId)
        {
            return Ok(await documentService_.GetVisibleToClientAsync(clientId));
        }

        [HttpGet("policy/{policyId:int}")]
        public async Task<ActionResult<IEnumerable<DocumentDto>>> GetForPolicy(int policyId)
        {
            return Ok(await documentService_.GetForPolicyAsync(policyId));
        }

        [HttpGet("type/{documentType}")]
        public async Task<ActionResult<IEnumerable<DocumentDto>>> GetByType(DocumentType documentType)
        {
            return Ok(await documentService_.GetByTypeAsync(documentType));
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(30_000_000)]
        public async Task<ActionResult<DocumentDto>> Upload([FromForm] UploadDocumentRequest request)
        {
            try
            {
                var created = await documentService_.UploadAsync(request);
                return CreatedAtAction(nameof(GetById), new {id = created.DocumentId}, created);
            }
            catch (ArgumentException ex)
            {
              return BadRequest(new {message = ex.Message});
            }
            catch (KeyNotFoundException ex)
            {
               return NotFound(new {message = ex.Message});
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new {message = ex.Message});
            }
        }

        [HttpGet("{id:int}/download-url")]
        public async Task<ActionResult> GetDownloadUrl(int id)
        {
            try
            {
               var uri = await documentService_.GetDownloadUriAsync(id);
                return Ok(new {url = uri.ToString()});
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new {message = ex.Message });
            }
        }

        [HttpPut("{id:int}/visibility")]
        public async Task<ActionResult<DocumentDto>> SetVisibility(int id, [FromBody] UpdateDocumentVisibilityRequest request)
        {
            try
            {
                return Ok(await documentService_.SetVisibilityAsync(id, request.VisibleToClient));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new {message = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await documentService_.DeleteAsync(id);
            return deleted ? NoContent() : NotFound(new{message = $"Document {id} was not found" });
        }
    }
}
