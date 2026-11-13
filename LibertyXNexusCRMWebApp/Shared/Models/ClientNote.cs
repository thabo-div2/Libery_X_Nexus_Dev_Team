using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shared.Models
{
    /// <summary>
    /// A private note the advisor keeps about a client, e.g. a call summary. Clients never see these.
    /// </summary>
    public class ClientNote
    {
        [Key]
        public int ClientNoteId { get; set; }

        [Required]
        public int ClientId { get; set; }

        [ForeignKey(nameof(ClientId))]
        public Client Client { get; set; } = null!;

        /// <summary>The advisor who wrote the note.</summary>
        public int AdvisorId { get; set; }

        [Required, MaxLength(2000)]
        public string Text { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
