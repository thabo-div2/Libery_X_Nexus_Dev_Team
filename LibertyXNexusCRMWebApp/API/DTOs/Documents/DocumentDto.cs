namespace API.DTOs.Documents
{
    public class DocumentDto
    {
        public int DocumentId { get; set; }
        public int ClientId { get; set; }

        /// <summary>
        /// The client's full name. Only filled in where the client was loaded with the document (the advisor's list).
        /// </summary>
        public string? ClientName { get; set; }
        public int? PolicyId { get; set; }

        /// <summary>
        /// The policy's name. Only filled in where the policy was loaded with the document (the advisor's list).
        /// </summary>
        public string? PolicyName { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public string SignatureStatus { get; set; } = string.Empty;
        public bool VisibleToClient { get; set; }
        public string? UploadedBy { get; set; }
        public DateTime? UpdateAt { get; set; }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
