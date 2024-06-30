using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading;
using System.Threading.Tasks;
using TheGame.Domain.DAL;

namespace TheGame.Api;

public class ApiInfraHealthCheck (GameDbContext gameDbContext) : IHealthCheck
{
  public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
  {
    var canConnect = await gameDbContext.Database.CanConnectAsync(cancellationToken);

    return canConnect ? HealthCheckResult.Healthy("DB is good") : HealthCheckResult.Unhealthy("DB is bad");
  }
}
