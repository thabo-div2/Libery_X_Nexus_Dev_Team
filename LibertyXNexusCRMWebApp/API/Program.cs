using API.Data;
using API.HealthChecks;
using API.Identity;
using API.Repositories.Implementations;
using API.Repositories.Interfaces;
using API.Services.Implementations;
using API.Services.Interfaces;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;
using System.Text;


namespace API
{
    /// <summary>
    /// Starts the API.
    /// </summary>
    public partial class Program
    {
        /// <summary>
        /// Sets up the services and runs the API.
        /// </summary>
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("Frontend", policy =>
                {
                    policy
                        .WithOrigins(
                           builder.Configuration["Frontend:BaseUrl"]!
                        )
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });

            var dataDirectory = Path.Combine(builder.Environment.ContentRootPath, "Data");

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")?.Replace("[DataPath]", dataDirectory);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("ConnectionStrings:DefaultConnection is missing.");
            }

            builder.Services.AddDataAccessLayer(connectionString!);

            // Identity (Accounts, password hashing, lockout, and roles)
            builder.Services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            // JWT Bearer Authentication
            builder.Services.AddOptions<JwtSettings>()
                .Bind(builder.Configuration.GetSection(JwtSettings.SectionName))
                .Validate(s => !string.IsNullOrWhiteSpace(s.Key) && Encoding.UTF8.GetByteCount(s.Key) >= 32,
                "Jwt: Key is missing or shorter than 32 bytes. Set with local or App service settings")
                .ValidateOnStart();
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
            builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<IOptions<JwtSettings>>((options, jwtOptions) =>
                {
                    var jwt = jwtOptions.Value;
                    options.MapInboundClaims = false;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwt.Issuer,
                        ValidateAudience = true,
                        ValidAudience = jwt.Audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                        ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(1),
                        RoleClaimType = "role"
                    };
                });

            builder.Services.AddAuthorizationBuilder().SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

            builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IAdvisorService, AdvisorService>();
            builder.Services.AddScoped<IAdvisorRepository, AdvisorRepository>();
            builder.Services.AddScoped<IInvitationRepository, InvitationRepository>();
            builder.Services.AddScoped<IClientRepository, ClientRepository>();
            builder.Services.AddScoped<IPolicyRepository, PolicyRepository>();
            builder.Services.AddScoped<IMeetingRepository, MeetingRepository>();
            builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
            builder.Services.AddScoped<IClientService, ClientService>();
            builder.Services.AddScoped<IMeetingService, MeetingService>();
            builder.Services.AddScoped<IPolicyService, PolicyService>();
            builder.Services.AddScoped<IInvitationService, InvitationService>();
            builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddOptions<SmtpEmailOptions>()
                .Bind(builder.Configuration.GetSection(SmtpEmailOptions.SectionName))
                .ValidateOnStart();
            builder.Services.AddScoped<IEmailService, SmtpEmailService>();
            builder.Services.AddScoped<IMessageService, MessageService>();
            builder.Services.AddBlobStorage(
                builder.Configuration.GetConnectionString("BlobStorage"),
                builder.Configuration["BlobStorage:AccountUrl"],
                builder.Configuration["BlobStorage:ContainerName"]);
            builder.Services.AddScoped<IDocumentService, DocumentService>();
            builder.Services.AddScoped<ICaseService, CaseService>();
            builder.Services.Configure<AlphaVantageOptions>(builder.Configuration.GetSection("AlphaVantage"));
            builder.Services.AddHttpClient<IMarketInformationService, MarketInformationService>();
            builder.Services.AddScoped<IFaqService, FaqService>();
            builder.Services.AddScoped<IMathService, MathService>();

            builder.Services.AddMemoryCache();

            builder.Services.AddHealthChecks()
                .AddCheck<DatabaseHealthCheck>("database")
                .AddCheck<BlobStorageHealthCheck>("blob-storage");

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
                    await IdentitySeeder.SeedAsync(scope.ServiceProvider);
                }

            }

            using (var scope = app.Services.CreateScope())
            {
                var blobService = scope.ServiceProvider.GetRequiredService<IBlobStorageService>();
                if (blobService is BlobStorageService concreteBlobService)
                {
                    await concreteBlobService.EnsureContainerExistsAsync();
                }


            }

            if (app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("Seed:SampleDocuments"))
            {
                using var scope = app.Services.CreateScope();
                await DocumentSeeder.SeedAsync(scope.ServiceProvider);
            }

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseCors();

            app.MapControllers();
            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = async (context, report) =>
                {
                    context.Response.ContentType = "application/json";

                    var result = JsonSerializer.Serialize(new
                    {
                        status = report.Status.ToString(),
                        checks = report.Entries.Select(e => new
                        {
                            name = e.Key,
                            status = e.Value.Status.ToString(),
                            description = e.Value.Description,
                            error = e.Value.Exception?.Message
                        })
                    });

                    await context.Response.WriteAsync(result);
                }
            }).AllowAnonymous();

            app.Run();
        }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
