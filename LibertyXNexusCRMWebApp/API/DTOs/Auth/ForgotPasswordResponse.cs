namespace API.DTOs.Auth
{
    /// <summary>
    /// What the API sends back after a forgot password request. It's always the same message so nobody can tell which emails have accounts.
    /// </summary>
    public class ForgotPasswordResponse
    {
        public string Message { get; set; } = string.Empty;
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
