namespace API.Services.Implementations
{
    /// <summary>
    /// Turns the UTC times stored in the database into South African time for notification text,
    /// so a client reads "14:00" and not the UTC "12:00".
    /// </summary>
    public static class DisplayTime
    {
        private static readonly TimeZoneInfo SouthAfrica = FindSouthAfrica();

        private static TimeZoneInfo FindSouthAfrica()
        {
            // Windows and Linux name the zone differently.
            foreach (var id in new[] { "South Africa Standard Time", "Africa/Johannesburg" })
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(id);
                }
                catch (TimeZoneNotFoundException) { }
                catch (InvalidTimeZoneException) { }
            }

            // South Africa has no daylight saving, so a fixed +2 is always right.
            return TimeZoneInfo.CreateCustomTimeZone("SAST", TimeSpan.FromHours(2), "SAST", "SAST");
        }

        /// <summary>
        /// Formats a UTC date as South African time, e.g. "Mon 3 Nov, 14:00".
        /// </summary>
        public static string Format(DateTime utc, string format = "ddd d MMM, HH:mm")
        {
            var asUtc = utc.Kind == DateTimeKind.Local ? utc.ToUniversalTime() : DateTime.SpecifyKind(utc, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTimeFromUtc(asUtc, SouthAfrica).ToString(format);
        }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
