using API.Services.Interfaces;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;

namespace API.Services.Implementations
{
    public class BlobStorageOptions
    {
        public string ContainerName { get; set; } = "client-documents";
    }

    public class BlobStorageService : IBlobStorageService
    {
        private readonly BlobContainerClient _containerClient;
        private readonly BlobStorageOptions _options;

        public BlobStorageService(BlobServiceClient blobServiceClient, IOptions<BlobStorageOptions> options)
        {
            _options = options.Value;
            _containerClient = blobServiceClient.GetBlobContainerClient(_options.ContainerName);
        }

        public async Task EnsureContainerExistsAsync()
        {
            await _containerClient.CreateIfNotExistsAsync(Azure.Storage.Blobs.Models.PublicAccessType.None);
        }

        public async Task<string> UploadAsync(Stream content, string fileName, string contentType)
        {
            if (content is null) throw new ArgumentNullException(nameof(content));

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("File name is required.", nameof(fileName));
            }

            var blobName = $"{Guid.NewGuid():N}-{SanitizeFileName(fileName)}";

            var blobClient = _containerClient.GetBlobClient(blobName);

            var httpHeaders = new Azure.Storage.Blobs.Models.BlobHttpHeaders
            {
                ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType
            };

            await blobClient.UploadAsync(content, httpHeaders);

            return $"{_options.ContainerName}/{blobName}";
        }

        public async Task<Uri> GetReadSasUriAsync(string blobReference, TimeSpan? validFor = null) 
        {
            var blobClient = GetBlobClient(blobReference);

            if(!blobClient.CanGenerateSasUri)
            {
                throw new InvalidOperationException("Blob client cannot generate SAS URIs. Ensure the storage " +
                    "was configured with a shared key credential, not just a connection " +
                    "string missing account key permissions.");
            }

            var expiry = DateTimeOffset.UtcNow.Add(validFor ?? TimeSpan.FromMinutes(15));

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = blobClient.BlobContainerName,
                BlobName = blobClient.Name,
                Resource = "b",
                ExpiresOn = expiry
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            return blobClient.GenerateSasUri(sasBuilder);
        }

        public async Task DeleteAsync(string blobReference) 
        {
            var blobClient = GetBlobClient(blobReference);
            await blobClient.DeleteIfExistsAsync();
        }
        
        public async Task<bool> ExistsAsync(string blobReference) 
        {
            var blobClient = GetBlobClient(blobReference);
            var response = await blobClient.ExistsAsync();
            return response.Value;
        }

        private BlobClient GetBlobClient(string blobReference)
        {
            if (string.IsNullOrWhiteSpace(blobReference))
                throw new ArgumentException("Blob reference is required.", nameof(blobReference));

            var separatorIndex = blobReference.IndexOf('/');
            if (separatorIndex < 0)
                throw new ArgumentException($"Malformed blob reference '{blobReference}' - expected 'container/blobName'.");

            var blobName = blobReference[(separatorIndex + 1)..];
            return _containerClient.GetBlobClient(blobName);
        }

        private static string SanitizeFileName(string fileName)
        {
            foreach (var invalidChar in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(invalidChar, '_');
            }

            return fileName;
        }
    }
}
