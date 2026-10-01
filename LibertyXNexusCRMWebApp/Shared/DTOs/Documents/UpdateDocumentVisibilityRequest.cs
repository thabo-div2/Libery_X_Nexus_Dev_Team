using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.Documents
{
    public class UpdateDocumentVisibilityRequest
    {
        [Required]
        public bool VisibleToClient { get; set; }
    }
}
