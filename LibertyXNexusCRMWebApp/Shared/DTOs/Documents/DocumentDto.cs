namespace Shared.DTOs.Documents
{
    public class DocumentDto
    {
        public int DocumentId { get; set; }
        public int ClientId { get; set; }
        public int PolicyId { get; set; }
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
