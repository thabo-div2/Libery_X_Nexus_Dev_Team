using Shared.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Shared.Models
{
    /// <summary>
    /// Represents a document associated with a client and a policy.
    /// </summary>
    public class Document
    {
        [Key]
        public int DocumentId { get; set; }

        [Required]
        public int ClientId { get; set; }

        [ForeignKey(nameof(ClientId))]
        public Client Client { get; set; } = null;

        [Required]
        public int PolicyId { get; set; }

        [ForeignKey(nameof(PolicyId))]
        public Policy Policy { get; set; }

        [Required, MaxLength(300)]
        public string FileName { get; set; } = string.Empty;

        [Required, MaxLength(1000)]
        public string BlobReference { get; set; } = string.Empty;

        [MaxLength(100)]
        public string ContentType { get; set; } = string.Empty;

        public long FileSizeBytes { get; set; }

        public DocumentType DocumentType { get; set; } = DocumentType.Other;

        public bool VisibleToClient { get; set; } = false;
        public DateTime? UpdateAt { get; set; }

        [MaxLength(100)]
        public string UploadedBy { get; set; }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
