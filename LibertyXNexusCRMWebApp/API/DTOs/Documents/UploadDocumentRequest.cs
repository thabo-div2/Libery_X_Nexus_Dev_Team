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

        [Required, MaxLength(100)]
        public string UploadedBy { get; set; } = string.Empty; //temporary until auth exists, TODO: replace with advisors identity from the token instead oif client supplied value

        public IFormFile File { get; set; } = null!;
    }
}
