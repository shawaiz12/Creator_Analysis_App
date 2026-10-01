using CreatorAnalytics.Strategy.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreatorAnalytics.Strategy.Infrastructure
{
    public class StrategyEntityConfigurations : IEntityTypeConfiguration<StrategyDocument>
    {
        public void Configure(EntityTypeBuilder<StrategyDocument> builder)
        {
            builder.ToTable("StrategyDocuments");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            builder.HasIndex(x => x.TenantId);

            builder.HasMany(x => x.Revisions)
           .WithOne()
           .HasForeignKey(r => r.StrategyDocumentId)
           .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Reviews)
                .WithOne()
                .HasForeignKey(r => r.StrategyDocumentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Navigation(x => x.Revisions).UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.Navigation(x => x.Reviews).UsePropertyAccessMode(PropertyAccessMode.Field);
        }
    }

    public class StrategyRevisionConfiguration : IEntityTypeConfiguration<StrategyRevision>
    {
        public void Configure(EntityTypeBuilder<StrategyRevision> builder)
        {
            builder.ToTable("StrategyRevisions");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Content).IsRequired();
            builder.Property(x => x.Origin)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.HasIndex(x => x.TenantId);
            builder.HasIndex(x => new { x.StrategyDocumentId, x.VersionNumber }).IsUnique();
        }
    }

    public class StrategyReviewConfiguration : IEntityTypeConfiguration<StrategyReview>
    {
        public void Configure(EntityTypeBuilder<StrategyReview> builder)
        {
            builder.ToTable("StrategyReviews");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Decision)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.HasIndex(x => x.TenantId);
        }
    }
}
