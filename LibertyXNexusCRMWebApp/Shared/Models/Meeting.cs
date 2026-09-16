using Shared.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Shared.Models
{
    public class Meeting
    {
        [Key]
        public int MeetingId { get; set; }

        [Required]
        public int ClientId { get; set; }

        [ForeignKey(nameof(ClientId))]
        public Client Client { get; set; } = null;

        [Required]
        public DateTime MeetingDate { get; set; }

        public int DurationNotes { get; set; } = 60;

        [MaxLength(100)]
        public string? MeetingType { get; set; }

        [MaxLength(300)]
        public string? Location { get; set; }

        public MeetingStatus Status { get; set; } = MeetingStatus.Requested;

        [MaxLength(2000)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; }
    }
}
