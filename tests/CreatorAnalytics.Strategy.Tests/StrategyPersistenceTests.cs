using CreatorAnalytics.Strategy.Domain;
using CreatorAnalytics.Strategy.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CreatorAnalytics.Strategy.Tests;

public class StrategyPersistenceTests : DatabaseTestBase
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _reviewerId = Guid.NewGuid();

    private StrategyDocument NewSubmittedDocument()
    {
        var document = new StrategyDocument(_tenantId, Guid.NewGuid());
        document.AddRevision("AI draft", RevisionOrigin.Ai, null);
        document.SubmitForApproval();
        return document;
    }

    private static StrategyDocument Load(StrategyDbContext context, Guid id) =>
        context.Documents
            .Include(d => d.Revisions)
            .Include(d => d.Reviews)
            .Single(d => d.Id == id);

    [Fact]
    public void Saves_And_Loads_A_Full_Review_History()
    {
        var document = NewSubmittedDocument();
        document.Reject(_reviewerId, document.CurrentRevision!.Id, "Too slow.");
        document.AddRevision("Faster pacing", RevisionOrigin.Human, Guid.NewGuid());
        document.SubmitForApproval();
        document.Approve(_reviewerId, document.CurrentRevision!.Id);

        using (var write = NewContext(_tenantId))
        {
            write.Documents.Add(document);
            write.SaveChanges();
        }

        using var read = NewContext(_tenantId);
        var loaded = Load(read, document.Id);

        Assert.Equal(StrategyStatus.Approved, loaded.Status);
        Assert.Equal(2, loaded.Revisions.Count);
        Assert.Equal(2, loaded.CurrentRevision!.VersionNumber);
        Assert.Equal(2, loaded.Reviews.Count);
        Assert.Contains(loaded.Reviews, r => r.Decision == ReviewDecision.Rejected && r.Reason == "Too slow.");
        Assert.Contains(loaded.Reviews, r => r.Decision == ReviewDecision.Approved);
    }

    [Fact]
    public void A_Review_Added_To_A_Loaded_Document_Is_Saved()
    {
        var document = NewSubmittedDocument();
        using (var setup = NewContext(_tenantId))
        {
            setup.Documents.Add(document);
            setup.SaveChanges();
        }

        using (var work = NewContext(_tenantId))
        {
            var loaded = Load(work, document.Id);
            loaded.Approve(_reviewerId, loaded.CurrentRevision!.Id);
            work.SaveChanges();
        }

        using var check = NewContext(_tenantId);
        var reloaded = Load(check, document.Id);
        Assert.Equal(StrategyStatus.Approved, reloaded.Status);
        Assert.Single(reloaded.Reviews);
    }

    [Fact]
    public void Two_Admins_Deciding_At_Once_Cannot_Both_Win()
    {
        var document = NewSubmittedDocument();
        using (var setup = NewContext(_tenantId))
        {
            setup.Documents.Add(document);
            setup.SaveChanges();
        }

        using var adminA = NewContext(_tenantId);
        using var adminB = NewContext(_tenantId);
        var docA = Load(adminA, document.Id);
        var docB = Load(adminB, document.Id);

        docA.Approve(Guid.NewGuid(), docA.CurrentRevision!.Id);
        adminA.SaveChanges();

        docB.Reject(Guid.NewGuid(), docB.CurrentRevision!.Id, "Weak hook.");
        Assert.Throws<DbUpdateConcurrencyException>(() => adminB.SaveChanges());

        using var check = NewContext(_tenantId);
        var final = Load(check, document.Id);
        Assert.Equal(StrategyStatus.Approved, final.Status);
        Assert.Single(final.Reviews);
    }
}
