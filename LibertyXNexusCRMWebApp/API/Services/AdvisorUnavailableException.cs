namespace API.Services
{
    /// <summary>
    /// Thrown when a client tries to message their advisor, or book or move a meeting, outside the
    /// advisor's working hours. The message is written for the client and is safe to show as it is.
    /// It is an InvalidOperationException so the controllers return it as a 409 with the message.
    /// </summary>
    public class AdvisorUnavailableException : InvalidOperationException
    {
        public AdvisorUnavailableException(string message) : base(message) { }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
