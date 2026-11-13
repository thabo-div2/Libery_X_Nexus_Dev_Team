namespace API.DTOs.Activity
{
    /// <summary>
    /// One line of the activity log: something the signed-in advisor did.
    /// </summary>
    public class ActivityItemDto
    {
        public DateTime Timestamp { get; set; }

        /// <summary>The kind of action, e.g. "Create" or "DocumentDownload".</summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>What it was done to, e.g. "Client" or "Document".</summary>
        public string Entity { get; set; } = string.Empty;

        public int? EntityId { get; set; }

        public string? Details { get; set; }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
