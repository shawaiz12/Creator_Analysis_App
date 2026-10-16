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
        document.SubmitForApproval(Guid.NewGuid());
        document.ClearDomainEvents();
        return document;
    }

    private static StrategyDocument Load(StrategyDbContext context, Guid id) =>
        context.Documents
            .Include(d => d.Revisions)
            .Include(d => d.Reviews)
            .Include(d => d.GenerationRequests)
            .Single(d => d.Id == id);


    [Fact]
    public void Saves_And_Loads_A_Full_Review_History()
    {
        var document = NewSubmittedDocument();
        document.Reject(_reviewerId, document.CurrentRevision!.Id, "Too slow.");
        document.AddRevision("Faster pacing", RevisionOrigin.Human, Guid.NewGuid());
        document.SubmitForApproval(Guid.NewGuid());
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

    [Fact]
    public void Approving_Writes_An_Outbox_Message_In_The_Same_Save()
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
        var message = Assert.Single(check.OutboxMessages);
        Assert.Equal("StrategyApprovedEvent", message.Type);
        Assert.Null(message.ProcessedOnUtc);
    }

    [Fact]
    public void A_Refused_Save_Leaves_No_Outbox_Message_Behind()
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
        var message = Assert.Single(check.OutboxMessages);
        Assert.Equal("StrategyApprovedEvent", message.Type);
    }

    [Fact]
    public void The_Creation_Time_Survives_A_Round_Trip()
    {
        var document = NewSubmittedDocument();
        using (var write = NewContext(_tenantId))
        {
            write.Documents.Add(document);
            write.SaveChanges();
        }

        using var read = NewContext(_tenantId);
        var loaded = Load(read, document.Id);

        Assert.Equal(document.CreatedAtUtc, loaded.CreatedAtUtc);
    }
    [Fact]
    public void A_Generation_Request_Round_Trips_Together_With_Its_Ai_Draft()
    {
        var document = new StrategyDocument(_tenantId, Guid.NewGuid());
        var request = document.RequestGeneration(_reviewerId, DateTime.UtcNow);
        document.CompleteGeneration(request.Id, "AI draft text", "template", "v1", DateTime.UtcNow);

        using (var write = NewContext(_tenantId))
        {
            write.Documents.Add(document);
            write.SaveChanges();
        }

        using var read = NewContext(_tenantId);
        var loaded = Load(read, document.Id);

        var saved = Assert.Single(loaded.GenerationRequests);
        Assert.Equal(GenerationStatus.Succeeded, saved.Status);
        Assert.Equal("template", saved.Model);

        var revision = Assert.Single(loaded.Revisions);
        Assert.Equal(RevisionOrigin.Ai, revision.Origin);
        Assert.Null(revision.AuthorUserId);
    }

    [Fact]
    public void Another_Tenant_Sees_No_Generation_Requests()
    {
        var document = new StrategyDocument(_tenantId, Guid.NewGuid());
        document.RequestGeneration(_reviewerId, DateTime.UtcNow);

        using (var write = NewContext(_tenantId))
        {
            write.Documents.Add(document);
            write.SaveChanges();
        }

        using var other = NewContext(Guid.NewGuid());

        Assert.Empty(other.GenerationRequests.ToList());
    }

    [Fact]
    public void Two_Simultaneous_Requests_For_One_Strategy_Cannot_Both_Be_Saved()
    {
        var document = new StrategyDocument(_tenantId, Guid.NewGuid());
        using (var setup = NewContext(_tenantId))
        {
            setup.Documents.Add(document);
            setup.SaveChanges();
        }

        using var first = NewContext(_tenantId);
        using var second = NewContext(_tenantId);
        var docFirst = Load(first, document.Id);
        var docSecond = Load(second, document.Id);

        docFirst.RequestGeneration(_reviewerId, DateTime.UtcNow);
        docSecond.RequestGeneration(_reviewerId, DateTime.UtcNow);

        first.SaveChanges();

        Assert.Throws<DbUpdateException>(() => second.SaveChanges());
    }
}