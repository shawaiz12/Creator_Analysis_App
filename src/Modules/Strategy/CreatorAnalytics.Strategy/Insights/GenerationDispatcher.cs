using CreatorAnalytics.Identity.Contracts.Services;
using CreatorAnalytics.SharedKernel.Tenancy;
using CreatorAnalytics.Strategy.Domain;
using CreatorAnalytics.Strategy.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CreatorAnalytics.Strategy.Insights;

public sealed class GenerationDispatcher
{
    private const int BatchPerTenant = 10;

    private sealed record DueRequest(Guid RequestId, Guid DocumentId);

    private readonly IServiceProvider _rootServices;
    private readonly ILogger<GenerationDispatcher> _logger;

    public GenerationDispatcher(IServiceProvider rootServices, ILogger<GenerationDispatcher> logger)
    {
        _rootServices = rootServices;
        _logger = logger;
    }

    /// <summary>One pass over every organization. Returns how many drafts were produced.</summary>
    public async Task<int> RunOnceAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Guid> tenantIds;

        using (var scope = _rootServices.CreateScope())
        {
            var directory = scope.ServiceProvider.GetRequiredService<IOrganizationDirectory>();
            tenantIds = await directory.ListOrganizationIdsAsync(cancellationToken);
        }

        var completed = 0;

        foreach (var tenantId in tenantIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            completed += await ProcessTenantAsync(tenantId, nowUtc, cancellationToken);
        }

        return completed;
    }

    private async Task<int> ProcessTenantAsync(Guid tenantId, DateTime nowUtc, CancellationToken cancellationToken)
    {
        List<DueRequest> due;

        using (var scope = _rootServices.CreateScope())
        {
            // Work is always done for one explicitly chosen tenant at a time.
            scope.ServiceProvider.GetRequiredService<TenantContext>().Set(tenantId);
            var context = scope.ServiceProvider.GetRequiredService<StrategyDbContext>();

            var rows = await context.GenerationRequests
                .Where(r => r.Status == GenerationStatus.Pending && r.NextAttemptAtUtc <= nowUtc)
                .OrderBy(r => r.NextAttemptAtUtc)
                .Select(r => new { r.Id, r.StrategyDocumentId })
                .Take(BatchPerTenant)
                .ToListAsync(cancellationToken);

            due = rows.Select(r => new DueRequest(r.Id, r.StrategyDocumentId)).ToList();
        }

        var completed = 0;

        foreach (var item in due)
        {
            // A fresh scope (and tenant context) for every request.
            using var scope = _rootServices.CreateScope();
            scope.ServiceProvider.GetRequiredService<TenantContext>().Set(tenantId);

            if (await ProcessOneAsync(scope.ServiceProvider, tenantId, item, nowUtc, cancellationToken))
                completed++;
        }

        return completed;
    }

    private async Task<bool> ProcessOneAsync(
        IServiceProvider services, Guid tenantId, DueRequest item, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var context = services.GetRequiredService<StrategyDbContext>();
        var builder = services.GetRequiredService<InsightInputBuilder>();
        var generator = services.GetRequiredService<IInsightGenerator>();

        try
        {
            var document = await context.Documents
                .Include(d => d.Revisions)
                .Include(d => d.Reviews)
                .Include(d => d.GenerationRequests)
                .SingleAsync(d => d.Id == item.DocumentId, cancellationToken);

            var input = await builder.BuildAsync(document.VideoId, cancellationToken);
            var result = await generator.GenerateAsync(input, cancellationToken);

            document.CompleteGeneration(item.RequestId, result.Content, result.Model, result.PromptVersion, nowUtc);
            await context.SaveChangesAsync(cancellationToken);

            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (InsightGenerationException ex)
        {
            _logger.LogWarning(ex,
                "Draft generation failed for request {RequestId} (transient: {Transient})",
                item.RequestId, ex.IsTransient);

            await RecordFailureAsync(tenantId, item, ex.Message, !ex.IsTransient, nowUtc, cancellationToken);
        }
        catch (TenantViolationException ex)
        {
            _logger.LogCritical(ex, "Tenant violation while generating a draft for request {RequestId}", item.RequestId);
            await RecordFailureAsync(tenantId, item, "Blocked by a tenant safety check.", true, nowUtc, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            await RecordFailureAsync(
                tenantId, item, "The strategy changed while the draft was being written.", true, nowUtc, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            // For example: the strategy moved on to review, or the AI draft limit was reached.
            await RecordFailureAsync(tenantId, item, ex.Message, true, nowUtc, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Generating a draft failed for request {RequestId}", item.RequestId);
            await RecordFailureAsync(tenantId, item, ex.Message, false, nowUtc, cancellationToken);
        }

        return false;
    }

    private async Task RecordFailureAsync(
        Guid tenantId, DueRequest item, string error, bool permanent, DateTime nowUtc, CancellationToken cancellationToken)
    {
        try
        {
            // A fresh scope: the one where the error happened may hold half-finished changes.
            using var scope = _rootServices.CreateScope();
            scope.ServiceProvider.GetRequiredService<TenantContext>().Set(tenantId);
            var context = scope.ServiceProvider.GetRequiredService<StrategyDbContext>();

            var document = await context.Documents
                .Include(d => d.GenerationRequests)
                .SingleAsync(d => d.Id == item.DocumentId, cancellationToken);

            document.FailGeneration(item.RequestId, error, permanent, nowUtc);
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not record the failure of generation request {RequestId}", item.RequestId);
        }
    }
}