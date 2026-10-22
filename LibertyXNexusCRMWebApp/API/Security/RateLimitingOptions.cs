namespace API.Security
{
    /// <summary>
    /// Settings for API rate limiting
    /// </summary>
    public class RateLimitingOptions
    {
        public const string SectionName = "RateLimiting";
        public bool Enabled { get; set; } = true;
        public int WindowSeconds { get; set; } = 60;
        // Requests allowed per window for each signed-in user, across the whole API.
        public int UserRequestsPerWindow { get; set; } = 120;
        public int AnonymousRequestsPerWindow { get; set; } = 60;
        public int AuthRequestsPerWindow { get; set; } = 20;
        public int PasswordResetRequestsPerWindow { get; set; } = 5;
        public int UploadRequestsPerWindow { get; set; } = 15;
        public int PasswordResetWindowMinutes { get; set; } = 15;
        public int UploadWindowMinutes { get; set; } = 10;

        ///<summary>
        /// Checks that every limit and window is a positive number, which means a typo in the configuratin stops the API at startup
        /// </summary>
        public void Validate()
        {
            var values = new Dictionary<string, int>
            {
                [nameof(WindowSeconds)] = WindowSeconds,
                [nameof(UserRequestsPerWindow)] = UserRequestsPerWindow,
                [nameof(AnonymousRequestsPerWindow)] = AnonymousRequestsPerWindow,
                [nameof(AuthRequestsPerWindow)] = AuthRequestsPerWindow,
                [nameof(PasswordResetRequestsPerWindow)] = PasswordResetRequestsPerWindow,
                [nameof(PasswordResetWindowMinutes)] = PasswordResetWindowMinutes,
                [nameof(UploadRequestsPerWindow)] = UploadRequestsPerWindow,
                [nameof(UploadWindowMinutes)] = UploadWindowMinutes
            };

            foreach (var (name, value) in values)
            {
                if (value <= 0)
                {
                    throw new InvalidOperationException($"{SectionName}:{name} must be greater than zero");
                }
            }
        }
    }
}
