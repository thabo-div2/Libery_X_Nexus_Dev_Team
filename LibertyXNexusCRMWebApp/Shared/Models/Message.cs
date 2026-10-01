using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Shared.Models
{
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
