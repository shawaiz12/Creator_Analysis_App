using CreatorAnalytics.Identity.Contracts.Services;
using CreatorAnalytics.Identity.Domain;
using CreatorAnalytics.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CreatorAnalytics.Identity.Services;

public sealed class OnboardingService : IOnboardingService
{
    public const int MaxOrganizationsPerUser = 3;

    private readonly IdentityDbContext _context;

    public OnboardingService(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<OnboardingResult> CreateOrganizationAsync(
        string externalUserId,
        string email,
        string organizationName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(organizationName))
            return OnboardingResult.Failure("Organization name is required.");

        var user = await _context.Users
            .SingleOrDefaultAsync(u => u.ExternalId == externalUserId, cancellationToken);

        if (user is not null)
        {
            var adminCount = await _context.TenantMemberships
                .CountAsync(m => m.UserId == user.Id && m.Role == Role.Admin, cancellationToken);

            if (adminCount >= MaxOrganizationsPerUser)
                return OnboardingResult.Failure(
                    $"You can administer at most {MaxOrganizationsPerUser} organizations.");
        }
        else
        {
            user = new User(externalUserId, email);
            _context.Users.Add(user);
        }

        var organization = new Organization(organizationName.Trim());
        _context.Organizations.Add(organization);
        _context.TenantMemberships.Add(new TenantMembership(organization.Id, user.Id, Role.Admin));

        // One SaveChanges is one transaction: the user, the organization and the
        // Admin membership are all saved together, or none of them are.
        await _context.SaveChangesAsync(cancellationToken);

        return OnboardingResult.Success(user.Id, organization.Id);
    }
}