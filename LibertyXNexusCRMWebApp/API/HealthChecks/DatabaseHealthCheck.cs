using API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace API.HealthChecks
{
    /// <summary>
    /// Confirms the API can connect to the database and that the database is responsive.
    /// </summary>
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

        public DatabaseHealthCheck(IDbContextFactory<ApplicationDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var canConnect = await db.Database.CanConnectAsync(cancellationToken);

                return canConnect
                    ? HealthCheckResult.Healthy("Database connection is healthy.")
                    : HealthCheckResult.Unhealthy("Database connection is unhealthy.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Failed to check database connection.", ex);
            }
        }
        
    }
}
