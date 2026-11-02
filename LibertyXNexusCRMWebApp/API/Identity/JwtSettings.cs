namespace API.Identity
{
    public class JwtSettings
    {
        public const string SectionName = "Jwt";

        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;

        // Signing Key
        public string Key { get; set; } = string.Empty;

        public int ExpiryMinutes { get; set; } = 60;

        // How long a "Keep me signed in" login lasts.
        public int RememberMeExpiryDays { get; set; } = 7;
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
