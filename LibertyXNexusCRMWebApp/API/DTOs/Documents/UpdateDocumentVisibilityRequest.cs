using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Documents
{
    public class UpdateDocumentVisibilityRequest
    {
        [Required]
        public bool VisibleToClient { get; set; }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
