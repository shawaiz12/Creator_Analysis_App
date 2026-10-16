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

            builder.Property(x => x.Id).ValueGeneratedNever();

            builder.Property(x => x.RowVersion).IsRowVersion();

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

            builder.HasMany(x => x.GenerationRequests)
    .WithOne()
    .HasForeignKey(r => r.StrategyDocumentId)
    .OnDelete(DeleteBehavior.Restrict);

            builder.Navigation(x => x.GenerationRequests).UsePropertyAccessMode(PropertyAccessMode.Field);
        }
    }

    public class StrategyRevisionConfiguration : IEntityTypeConfiguration<StrategyRevision>
    {
        public void Configure(EntityTypeBuilder<StrategyRevision> builder)
        {
            builder.ToTable("StrategyRevisions");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).ValueGeneratedNever();

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

            builder.Property(x => x.Id).ValueGeneratedNever();

            builder.Property(x => x.Decision)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.HasIndex(x => x.TenantId);
        }
    }

    public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
    {
        public void Configure(EntityTypeBuilder<OutboxMessage> builder)
        {
            builder.ToTable("OutboxMessages");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Type)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Content)
                .IsRequired(); // Maps to NVARCHAR(MAX) for JSON payloads
        }
    }

    public class GenerationRequestConfiguration : IEntityTypeConfiguration<GenerationRequest>
    {
        public void Configure(EntityTypeBuilder<GenerationRequest> builder)
        {
            builder.ToTable("GenerationRequests");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();

            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(x => x.Error).HasMaxLength(1000);
            builder.Property(x => x.Model).HasMaxLength(100);
            builder.Property(x => x.PromptVersion).HasMaxLength(50);

            builder.HasIndex(x => x.TenantId);
            builder.HasIndex(x => new { x.Status, x.NextAttemptAtUtc });

            // The database itself refuses a second open request for the same strategy,
            // even if two requests arrive at the same moment.
            builder.HasIndex(x => x.StrategyDocumentId)
                .IsUnique()
                .HasFilter("[Status] = 'Pending'");
        }
    }
}
