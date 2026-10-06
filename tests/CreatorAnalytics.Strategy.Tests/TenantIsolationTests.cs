using CreatorAnalytics.Strategy.Domain;
using Microsoft.EntityFrameworkCore;

namespace CreatorAnalytics.Strategy.Tests;

public class TenantIsolationTests : DatabaseTestBase
{
    private readonly Guid _tenantA = Guid.NewGuid();
    private readonly Guid _tenantB = Guid.NewGuid();

    private Guid SeedApprovedDocumentFor(Guid tenantId)
    {
        var document = new StrategyDocument(tenantId, Guid.NewGuid());
        document.AddRevision("AI draft", RevisionOrigin.Ai, null);
        document.SubmitForApproval();
        document.Approve(Guid.NewGuid(), document.CurrentRevision!.Id);

        using var context = NewContext(tenantId);
        context.Documents.Add(document);
        context.SaveChanges();
        return document.Id;
    }

    [Fact]
    public void A_Tenant_Sees_Its_Own_Data()
    {
        var id = SeedApprovedDocumentFor(_tenantA);

        using var context = NewContext(_tenantA);

        Assert.Single(context.Documents.Where(d => d.Id == id));
    }

    [Fact]
    public void Another_Tenant_Sees_Nothing()
    {
        SeedApprovedDocumentFor(_tenantA);

        using var context = NewContext(_tenantB);

        Assert.Empty(context.Documents.ToList());
        Assert.Empty(context.Revisions.ToList());
        Assert.Empty(context.Reviews.ToList());
    }

    [Fact]
    public void Another_Tenant_Cannot_Load_A_Document_By_Id()
    {
        var id = SeedApprovedDocumentFor(_tenantA);

        using var context = NewContext(_tenantB);

        Assert.Null(context.Documents.SingleOrDefault(d => d.Id == id));
    }

    [Fact]
    public void Writing_Another_Tenants_Data_Is_Blocked()
    {
        var foreign = new StrategyDocument(_tenantA, Guid.NewGuid());

        using (var context = NewContext(_tenantB))
        {
            context.Documents.Add(foreign);

            var exception = Assert.Throws<InvalidOperationException>(() => context.SaveChanges());
            Assert.Contains("cross-tenant", exception.Message);
        }

        using var check = NewContext(_tenantA);
        Assert.Empty(check.Documents.ToList());
    }

    [Fact]
    public void No_Tenant_Means_No_Data()
    {
        SeedApprovedDocumentFor(_tenantA);

        using var context = NewContext();

        Assert.Empty(context.Documents.ToList());
    }
}