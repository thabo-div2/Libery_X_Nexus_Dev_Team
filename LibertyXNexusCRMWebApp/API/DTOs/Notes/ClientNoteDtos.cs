using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Notes
{
    /// <summary>
    /// A private note about a client.
    /// </summary>
    public class ClientNoteDto
    {
        public int ClientNoteId { get; set; }

        public int ClientId { get; set; }

        public string Text { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Sent by the advisor to add a note.
    /// </summary>
    public class CreateClientNoteRequest
    {
        [Required, MaxLength(2000)]
        public string? Text { get; set; }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
