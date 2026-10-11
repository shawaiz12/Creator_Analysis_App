using CreatorAnalytics.Strategy.Domain;

namespace CreatorAnalytics.Strategy.Tests;

public class StrategyDocumentCreatedAtTests
{
    [Fact]
    public void A_New_Strategy_Records_When_It_Was_Created()
    {
        var before = DateTime.UtcNow;

        var document = new StrategyDocument(Guid.NewGuid(), Guid.NewGuid());

        var after = DateTime.UtcNow;
        Assert.InRange(document.CreatedAtUtc, before, after);
    }
}