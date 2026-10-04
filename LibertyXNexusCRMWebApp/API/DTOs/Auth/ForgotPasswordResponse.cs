namespace API.DTOs.Auth
{
    // Exists is true only when an active account was found for the email.
    // ResetToken is only populated when Exists is true - this project has no
    // email-sending infrastructure, so the token is handed straight back to
    // the frontend to complete the reset in the same flow, rather than being
    // emailed to the user as it would be in a production system.
    public class ForgotPasswordResponse
    {
        public string Message { get; set; } = string.Empty;
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
