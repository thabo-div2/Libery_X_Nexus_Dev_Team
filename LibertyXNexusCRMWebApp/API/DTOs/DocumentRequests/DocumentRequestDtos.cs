using Shared.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace API.DTOs.DocumentRequests
{
    /// <summary>
    /// A document the advisor asked a client for.
    /// </summary>
    public class DocumentRequestDto
    {
        public int DocumentRequestId { get; set; }

        public int ClientId { get; set; }

        public int? PolicyId { get; set; }
        public string? PolicyName { get; set; }

        public string DocumentType { get; set; } = string.Empty;

        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? FulfilledAt { get; set; }

        public bool IsFulfilled { get; set; }
    }

    /// <summary>
    /// Sent by the advisor to ask a client for a document.
    /// </summary>
    public class CreateDocumentRequestRequest
    {
        [Range(1, int.MaxValue)]
        public int? PolicyId { get; set; }

        [Required]
        public DocumentType DocumentType { get; set; }

        [MaxLength(300)]
        public string? Note { get; set; }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
