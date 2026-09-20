using Shared.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Shared.Models
{
    public class ClientQuery
    {
        [Key]
        public int QueryId { get; set; }

        [Required]
        public int ClientId { get; set; }

        [ForeignKey(nameof(ClientId))]
        public Client Client { get; set; } = null;

        public int? AdvisorId { get; set; }

        [ForeignKey(nameof(AdvisorId))]
        public Advisor? Advisor { get; set; }

        [Required, MaxLength(300)]
        public string Subject { get; set; } = string.Empty;

        [Required, MaxLength(4000)]
        public string Message { get; set; } = string.Empty;

        [MaxLength(4000)]
        public string? Response { get; set; } = string.Empty;

        public QueryStatus Status { get; set; } = QueryStatus.Open;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RespondedAt { get; set; }
    }
}
