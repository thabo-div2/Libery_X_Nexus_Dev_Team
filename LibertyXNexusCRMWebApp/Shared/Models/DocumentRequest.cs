using Shared.Models.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shared.Models
{
    /// <summary>
    /// A document the advisor has asked a client for, e.g. a certified ID copy.
    /// It is outstanding until the client (or the advisor) uploads a document of that type for the same policy (or general scope).
    /// </summary>
    public class DocumentRequest
    {
        [Key]
        public int DocumentRequestId { get; set; }

        [Required]
        public int ClientId { get; set; }

        [ForeignKey(nameof(ClientId))]
        public Client Client { get; set; } = null!;

        /// <summary>Null means a general client request.</summary>
        public int? PolicyId { get; set; }

        [ForeignKey(nameof(PolicyId))]
        public Policy? Policy { get; set; }

        public DocumentType DocumentType { get; set; }

        /// <summary>Optional note for the client, e.g. "Not older than 3 months".</summary>
        [MaxLength(300)]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Null while the request is still outstanding.</summary>
        public DateTime? FulfilledAt { get; set; }

        /// <summary>The upload that fulfilled the request. Cleared when that document is deleted, reopening the request.</summary>
        public int? FulfilledDocumentId { get; set; }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
