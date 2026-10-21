using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CreatorAnalytics.Api.Health;

public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly string? _connectionString;

    public DatabaseHealthCheck(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default");
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            return HealthCheckResult.Unhealthy("The database connection is not configured.");

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            await command.ExecuteScalarAsync(cancellationToken);

            return HealthCheckResult.Healthy("The database answers.");
        }
        catch (Exception)
        {
            // Never put the exception text in the answer: it could contain connection details.
            return HealthCheckResult.Unhealthy("The database cannot be reached.");
        }
    }
}