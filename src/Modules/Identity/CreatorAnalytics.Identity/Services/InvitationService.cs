using CreatorAnalytics.Identity.Contracts.Services;
using CreatorAnalytics.Identity.Domain;
using CreatorAnalytics.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CreatorAnalytics.Identity.Services;

public sealed class InvitationService : IInvitationService
{
    private readonly IdentityDbContext _context;

    public InvitationService(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<InvitationResult> CreateAsync(
        Guid tenantId, Guid invitedByUserId, string email, string role, DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<Role>(role, ignoreCase: true, out var parsedRole) || !Enum.IsDefined(parsedRole))
            return InvitationResult.Invalid("Unknown role. Use Admin, Strategist or Editor.");

        string normalizedEmail;
        try
        {
            normalizedEmail = Invitation.NormalizeEmail(email);
        }
        catch (ArgumentException ex)
        {
            return InvitationResult.Invalid(ex.Message);
        }

        var alreadyMember = await _context.TenantMemberships
            .Join(_context.Users, m => m.UserId, u => u.Id, (m, u) => new { m.TenantId, u.Email })
            .AnyAsync(x => x.TenantId == tenantId && x.Email.ToLower() == normalizedEmail, cancellationToken);

        if (alreadyMember)
            return InvitationResult.Conflict("This person is already a member of the organization.");

        var open = await _context.Invitations.SingleOrDefaultAsync(
            i => i.TenantId == tenantId && i.Email == normalizedEmail && i.Status == InvitationStatus.Pending,
            cancellationToken);

        if (open is not null)
        {
            open.MarkExpiredIfDue(nowUtc);

            if (open.Status == InvitationStatus.Pending)
                return InvitationResult.Conflict("An invitation for this email is already open.");

            // Save the expiry first so the old invitation stops blocking the new one.
            await _context.SaveChangesAsync(cancellationToken);
        }

        var invitation = new Invitation(tenantId, normalizedEmail, parsedRole, invitedByUserId, nowUtc);
        _context.Invitations.Add(invitation);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Two requests raced; the database's unique rule let only one through.
            return InvitationResult.Conflict("An invitation for this email is already open.");
        }

        return InvitationResult.Success(invitation.Id, tenantId);
    }

    public async Task<IReadOnlyList<InvitationInfo>> ListAsync(
        Guid tenantId, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var rows = await _context.Invitations
            .AsNoTracking()
            .Where(i => i.TenantId == tenantId)
            .OrderByDescending(i => i.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return rows
            .Select(i => new InvitationInfo(
                i.Id,
                i.Email,
                i.Role.ToString(),
                i.Status == InvitationStatus.Pending && i.IsExpired(nowUtc) ? "Expired" : i.Status.ToString(),
                i.CreatedAtUtc,
                i.ExpiresAtUtc))
            .ToList();
    }

    public async Task<InvitationResult> RevokeAsync(
        Guid tenantId, Guid invitationId, CancellationToken cancellationToken = default)
    {
        var invitation = await _context.Invitations.SingleOrDefaultAsync(
            i => i.Id == invitationId && i.TenantId == tenantId, cancellationToken);

        if (invitation is null)
            return InvitationResult.NotFound();

        try
        {
            invitation.Revoke();
        }
        catch (InvalidOperationException ex)
        {
            return InvitationResult.Conflict(ex.Message);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return InvitationResult.Success(invitation.Id, tenantId);
    }

    public async Task<InvitationResult> AcceptAsync(
        Guid invitationId, string externalUserId, string email, DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        var invitation = await _context.Invitations
            .SingleOrDefaultAsync(i => i.Id == invitationId, cancellationToken);

        // Identity first: a wrong email looks exactly like an unknown invitation.
        if (invitation is null || !invitation.EmailMatches(email))
            return InvitationResult.NotFound();

        var user = await _context.Users
            .SingleOrDefaultAsync(u => u.ExternalId == externalUserId, cancellationToken);

        if (user is not null)
        {
            var alreadyMember = await _context.TenantMemberships
                .AnyAsync(m => m.TenantId == invitation.TenantId && m.UserId == user.Id, cancellationToken);

            if (alreadyMember)
                return InvitationResult.Conflict("You are already a member of this organization.");
        }

        try
        {
            invitation.Accept(email, nowUtc);
        }
        catch (InvitationEmailMismatchException)
        {
            return InvitationResult.NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return InvitationResult.Conflict(ex.Message);
        }

        if (user is null)
        {
            user = new User(externalUserId, email);
            _context.Users.Add(user);
        }

        _context.TenantMemberships.Add(new TenantMembership(invitation.TenantId, user.Id, invitation.Role));

        try
        {
            // One SaveChanges is one transaction: invitation, user and membership together.
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return InvitationResult.Conflict("This invitation was just accepted. Please reload.");
        }

        return InvitationResult.Success(invitation.Id, invitation.TenantId);
    }
}