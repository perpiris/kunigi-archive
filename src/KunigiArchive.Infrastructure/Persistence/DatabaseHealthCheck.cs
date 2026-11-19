using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace KunigiArchive.Infrastructure.Persistence;

// /healthz (#39): the site is healthy when it can reach its database. The deploy's post-check calls it, so a
// deploy that starts the site but can't reach SQL Server fails instead of passing on the home page alone.
internal sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly KunigiArchiveDbContext _dbContext;

    public DatabaseHealthCheck(KunigiArchiveDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("The database can't be reached.");
    }
}
