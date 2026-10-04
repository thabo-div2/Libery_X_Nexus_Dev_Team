using API.Services.Interfaces;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;

namespace API.Services.Implementations
{
    /// <summary>
    /// Blob storage settings.
    /// </summary>
    public class BlobStorageOptions
    {
        public string ContainerName { get; set; } = "client-documents";
    }

    /// <summary>
    /// Saves client documents in Azure Blob Storage.
    /// </summary>
    public class BlobStorageService : IBlobStorageService
    {
        private readonly BlobContainerClient _containerClient;
        private readonly BlobStorageOptions _options;

        /// <summary>
        /// Sets up the service.
        /// </summary>
        public BlobStorageService(BlobServiceClient blobServiceClient, IOptions<BlobStorageOptions> options)
        {
            _options = options.Value;
            _containerClient = blobServiceClient.GetBlobContainerClient(_options.ContainerName);
        }

        /// <summary>
        /// Creates the container if it's missing.
        /// </summary>
        public async Task EnsureContainerExistsAsync()
        {
            await _containerClient.CreateIfNotExistsAsync(PublicAccessType.None);
        }

        /// <summary>
        /// Uploads a file.
        /// </summary>
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

        /// <summary>
        /// Makes a temporary link to view a file.
        /// </summary>
        public async Task<Uri> GetReadSasUriAsync(string blobReference, TimeSpan? validFor = null)
        {
            var blobClient = GetBlobClient(blobReference);

            var expiry = DateTimeOffset.UtcNow.Add(validFor ?? TimeSpan.FromMinutes(15));

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = blobClient.BlobContainerName,
                BlobName = blobClient.Name,
                Resource = "b",
                ExpiresOn = expiry
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            if (blobClient.CanGenerateSasUri)
            {
                return blobClient.GenerateSasUri(sasBuilder);
            }

            // Azure App Service / managed identity path.
            // Requires Storage Blob Data Contributor (or equivalent) on the storage account.
            var now = DateTimeOffset.UtcNow;

            var userDelegationKeyOptions = new BlobGetUserDelegationKeyOptions(expiry)
            {
                StartsOn = now.AddMinutes(-5)
            };

            var userDelegationKey = await _containerClient
                .GetParentBlobServiceClient()
                .GetUserDelegationKeyAsync(userDelegationKeyOptions);

            return new UriBuilder(blobClient.Uri)
            {
                Query = sasBuilder.ToSasQueryParameters(userDelegationKey.Value, _containerClient.GetParentBlobServiceClient().AccountName).ToString()
            }.Uri;
        }

        /// <summary>
        /// Deletes a file.
        /// </summary>
        public async Task DeleteAsync(string blobReference)
        {
            var blobClient = GetBlobClient(blobReference);
            await blobClient.DeleteIfExistsAsync();
        }

        /// <summary>
        /// Checks if a file exists.
        /// </summary>
        public async Task<bool> ExistsAsync(string blobReference)
        {
            var blobClient = GetBlobClient(blobReference);
            var response = await blobClient.ExistsAsync();
            return response.Value;
        }

        /// <summary>
        /// Gets the blob from its saved name.
        /// </summary>
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

        /// <summary>
        /// Removes bad characters from a file name.
        /// </summary>
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

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
