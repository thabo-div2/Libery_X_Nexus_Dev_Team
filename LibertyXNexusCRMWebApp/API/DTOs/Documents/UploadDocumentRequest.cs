using Shared.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Documents
{
    public class UploadDocumentRequest
    {
        [Required]
        public int ClientId { get; set; }

        [Required]
        public int PolicyId { get; set; }

        [Required]
        public DocumentType DocumentType { get; set; }

        public bool VisibleToClient { get; set; } = false;

        // Filled in by the API from the signed-in user's token, so the caller doesn't need to send it.
        [MaxLength(100)]
        public string UploadedBy { get; set; } = string.Empty;

        public IFormFile File { get; set; } = null!;
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
