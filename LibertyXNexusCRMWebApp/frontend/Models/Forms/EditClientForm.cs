using System.ComponentModel.DataAnnotations;

namespace frontend.Models.Forms
{
    /// <summary>
    /// The edit form in the client detail panel on the Clients page.
    /// Holds the fields the API lets an advisor change (UpdateClientRequest).
    /// </summary>
    public sealed class EditClientForm
    {
        [Required(ErrorMessage = "Enter the client's first name.")]
        [MaxLength(100, ErrorMessage = "First name can be at most 100 characters.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter the client's last name.")]
        [MaxLength(100, ErrorMessage = "Last name can be at most 100 characters.")]
        public string LastName { get; set; } = string.Empty;

        // Optional. Empty passes; anything typed must look like a phone number.
        [RegularExpression(@"^\+?[0-9 ()\-]{7,20}$", ErrorMessage = "Enter a valid phone number, like +27 82 000 0000.")]
        [MaxLength(30)]
        public string? Phone { get; set; }

        // Optional. The API also runs the SA ID checksum and explains if it fails.
        [RegularExpression(@"^\d{13}$", ErrorMessage = "A South African ID number must be 13 digits.")]
        public string? IdentificationNumber { get; set; }

        [MaxLength(100)]
        public string? RiskProfile { get; set; }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
