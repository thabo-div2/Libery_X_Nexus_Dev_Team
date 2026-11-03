using API.Repositories.Implementations;
using API.Repositories.Interfaces;
using API.Services.Implementations;
using API.Services.Interfaces;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Register Repositories and Add db context
        /// </summary>
        /// <param name="services"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static IServiceCollection AddDataAccessLayer(this IServiceCollection services, string connectionString)
        {
            services.AddDbContextFactory<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(connectionString, sql =>
                {
                    sql.EnableRetryOnFailure(
                        maxRetryCount: 8,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);

                    sql.CommandTimeout(60); // Set command timeout to 60 seconds
                });
            });

            services.AddScoped<ApplicationDbContext>(sp => sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());

            // Scoped: one instance per HTTP request, matching DbContext lifetime.
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IClientRepository, ClientRepository>();
            services.AddScoped<IAdvisorRepository, AdvisorRepository>();
            services.AddScoped<IPolicyRepository, PolicyRepository>();
            services.AddScoped<IMeetingRepository, MeetingRepository>();
            services.AddScoped<IDocumentRepository, DocumentRepository>();
            services.AddScoped<ICaseRepository, CaseRepository>();
            services.AddScoped<IQueryRepository, QueryRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IInvitationRepository, InvitationRepository>();
            services.AddScoped<IAuditLogRepository, AuditLogRepository>();

            return services;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Registers Blob Storage support.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="connectionString"></param>
        /// <param name="containerName"></param>
        /// <returns></returns>
        public static IServiceCollection AddBlobStorage(
            this IServiceCollection services,
            string? connectionString,
            string? accountUrl,
            string? containerName = null)
        {
            services.Configure<BlobStorageOptions>(options =>
            {
                if (!string.IsNullOrWhiteSpace(containerName))
                    options.ContainerName = containerName;
            });

            services.AddSingleton<BlobServiceClient>(sp =>
            {
                var environment = sp.GetRequiredService<IHostEnvironment>();

                // Azure/Production: use managed identity authentication.
                if (!environment.IsDevelopment())
                {
                    if (string.IsNullOrWhiteSpace(accountUrl))
                        throw new InvalidOperationException(
                            "BlobStorage:AccountUrl is required in production.");

                    return new BlobServiceClient(
                        new Uri(accountUrl),
                        new DefaultAzureCredential());
                }

                // Local development: optionally use a storage connection string.
                if (!string.IsNullOrWhiteSpace(connectionString))
                {
                    return new BlobServiceClient(connectionString);
                }

                // Local development: authenticate using Visual Studio or Azure CLI.
                if (!string.IsNullOrWhiteSpace(accountUrl))
                {
                    return new BlobServiceClient(
                        new Uri(accountUrl),
                        new DefaultAzureCredential());
                }

                throw new InvalidOperationException(
                    "Blob Storage is not configured.");
            });

            services.AddSingleton<IBlobStorageService, BlobStorageService>();

            return services;
        }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
