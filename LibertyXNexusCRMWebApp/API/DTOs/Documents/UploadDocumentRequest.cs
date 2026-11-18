using Shared.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Documents
{
    public class UploadDocumentRequest
    {
        [Required]
        public int ClientId { get; set; }

        [Range(1, int.MaxValue)]
        public int? PolicyId { get; set; }

        [Required]
        public DocumentType DocumentType { get; set; }

        public bool VisibleToClient { get; set; } = false;

        // Filled in by the API from the signed-in user's token, so the caller doesn't need to send it.
        // It has to be nullable: a non-nullable string counts as required, and an empty form field
        // arrives as null, which made every upload that left it blank fail with a 400.
        [MaxLength(100)]
        public string? UploadedBy { get; set; }

        public IFormFile File { get; set; } = null!;
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
