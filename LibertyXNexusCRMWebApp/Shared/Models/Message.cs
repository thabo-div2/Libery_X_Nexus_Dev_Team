using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Shared.Models
{
    /// <summary>
    /// Represents a message exchanged between a client and an advisor.
    /// </summary>
    public class Message
    {
        [Key]
        public int MessageId { get; set; }

        public int ClientId { get; set; }

        [ForeignKey(nameof(ClientId))]
        public Client Client { get; set; } = null!;

        public int AdvisorId { get; set; }

        [ForeignKey(nameof(AdvisorId))]
        public Advisor Advisor { get; set; } = null!;

        public bool FromAdvisor { get; set; }

        [Required, MaxLength(4000)]
        public string Text { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
