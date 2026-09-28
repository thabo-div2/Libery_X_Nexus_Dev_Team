using API.Data;
using API.Repositories.Implementations;
using API.Repositories.Interfaces;
using API.Services.Implementations;
using API.Services.Interfaces;
using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;

namespace API
{
    public partial class Program
    {
        public static async Task Main(string[] args) 
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var containerName = builder.Configuration["BlobStorage:ContainerName"];

            var dataDirectory = Path.Combine(builder.Environment.ContentRootPath, "Data");

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")?.Replace("[DataPath]", dataDirectory);

            builder.Services.AddDataAccessLayer(connectionString!);

            builder.Services.AddScoped<IClientRepository, ClientRepository>();
            builder.Services.AddScoped<IPolicyRepository, PolicyRepository>();
            builder.Services.AddScoped<IMeetingRepository, MeetingRepository>();
            builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
            builder.Services.AddScoped<IClientService, ClientService>();
            builder.Services.AddScoped<IMeetingService, MeetingService>();
            builder.Services.AddScoped<IPolicyService, PolicyService>();
            builder.Services.AddBlobStorage(
                builder.Configuration.GetConnectionString("BlobStorage")!, 
                builder.Configuration["BlobStorage:ContainerName"]);
            builder.Services.AddScoped<IDocumentService, DocumentService>();


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();

                using (var scope = app.Services.CreateAsyncScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    await db.Database.MigrateAsync();
                    await DbInitializer.SeedAsync(db);
                }

                await RunTest.SmokeTest(app.Services);
            }

            using (var scope = app.Services.CreateScope())
            {
                var blobService = scope.ServiceProvider.GetRequiredService<IBlobStorageService>();
                if (blobService is BlobStorageService concreteBlobService)
                {
                    await concreteBlobService.EnsureContainerExistsAsync();
                }
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
