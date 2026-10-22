using System.ComponentModel.DataAnnotations;

namespace frontend.Models.Forms
{
    /// <summary>
    /// The "invite a client" box on the advisor dashboard.
    /// </summary>
    public sealed class InviteClientForm
    {
        [Required(ErrorMessage = "Enter the client's email address.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address, like name@example.com.")]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
