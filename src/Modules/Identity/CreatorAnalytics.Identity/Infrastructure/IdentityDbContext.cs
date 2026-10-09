using CreatorAnalytics.Identity.Domain;
using Microsoft.EntityFrameworkCore;


namespace CreatorAnalytics.Identity.Infrastructure;

public class IdentityDbContext : DbContext
{
    public const string Schema = "identity";

    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : base(options)
    {
    }

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User> Users => Set<User>();
    public DbSet<TenantMembership> TenantMemberships => Set<TenantMembership>();
    public DbSet<Invitation> Invitations => Set<Invitation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<Organization>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<User>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.ExternalId).IsRequired().HasMaxLength(100);
            b.Property(x => x.Email).IsRequired().HasMaxLength(255);

            // ExternalId must be unique so we don't duplicate Microsoft Entra ID users
            b.HasIndex(x => x.ExternalId).IsUnique();
        });

        modelBuilder.Entity<TenantMembership>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Role).HasConversion<string>().HasMaxLength(20).IsRequired();

            // A user can only have one role per organization
            b.HasIndex(x => new { x.TenantId, x.UserId }).IsUnique();

            b.HasOne<Organization>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Invitation>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Email).IsRequired().HasMaxLength(255);
            b.Property(x => x.Role).HasConversion<string>().HasMaxLength(20).IsRequired();
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            b.HasIndex(x => x.TenantId);
            b.HasOne<Organization>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}