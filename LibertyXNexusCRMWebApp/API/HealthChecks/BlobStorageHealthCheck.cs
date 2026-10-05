using API.Services.Implementations;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace API.HealthChecks
{
    /// <summary>
    /// Confirms the API can reach Azure Blob Storage
    /// </summary>
    public class BlobStorageHealthCheck : IHealthCheck
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName;

        public BlobStorageHealthCheck(BlobServiceClient blobServiceClient, IOptions<BlobStorageOptions> options)
        {
            _blobServiceClient = blobServiceClient;
            _containerName = options.Value.ContainerName;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                await containerClient.GetPropertiesAsync(cancellationToken: cancellationToken);

                return HealthCheckResult.Healthy("Blob Storage connection succeeded.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Failed to check Blob Storage connection.", ex);
            }
        }
    }
}