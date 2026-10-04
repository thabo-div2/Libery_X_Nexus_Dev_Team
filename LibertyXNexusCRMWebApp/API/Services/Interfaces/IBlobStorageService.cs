namespace API.Services.Interfaces
{
    public interface IBlobStorageService
    {
        Task<string> UploadAsync(Stream content, string fileName, string contentType);
        Task<Uri> GetReadSasUriAsync(string blobReference, TimeSpan? validFor = null);
        Task DeleteAsync(string blobReference);
        Task<bool> ExistsAsync(string blobReference);
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
