using CreatorAnalytics.Integration.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreatorAnalytics.Integration.Infrastructure;

public class ChannelConnectionConfiguration : IEntityTypeConfiguration<ChannelConnection>
{
    public void Configure(EntityTypeBuilder<ChannelConnection> builder)
    {
        builder.ToTable("ChannelConnections");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.YouTubeChannelId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.YouTubeChannelId }).IsUnique();
    }
}

public class VideoConfiguration : IEntityTypeConfiguration<Video>
{
    public void Configure(EntityTypeBuilder<Video> builder)
    {
        builder.ToTable("Videos");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.YouTubeVideoId).IsRequired().HasMaxLength(32);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(300);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.YouTubeVideoId }).IsUnique();

        builder.HasOne<ChannelConnection>()
            .WithMany()
            .HasForeignKey(x => x.ChannelConnectionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class RetentionSnapshotConfiguration : IEntityTypeConfiguration<RetentionSnapshot>
{
    public void Configure(EntityTypeBuilder<RetentionSnapshot> builder)
    {
        builder.ToTable("RetentionSnapshots");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.PointsJson).IsRequired();

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.VideoId, x.FetchedAtUtc });

        builder.HasOne<Video>()
            .WithMany()
            .HasForeignKey(x => x.VideoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}