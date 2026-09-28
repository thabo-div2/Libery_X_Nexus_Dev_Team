using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Documents
{
    public class UpdateDocumentVisibilityRequest
    {
        [Required]
        public bool VisibleToClient { get; set; }
    }
}
