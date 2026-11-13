namespace Conexa.Api.Services;

public interface IEventAccessService
{
    Task<bool> IsMemberAsync(Guid eventId, ClaimsPrincipal user, CancellationToken ct = default);
    Task<bool> IsOrganizerAsync(Guid eventId, ClaimsPrincipal user, CancellationToken ct = default);
}

/// <summary>Regras de acesso por evento. O administrador da plataforma tem abrangência global (RN07).</summary>
public class EventAccessService(ApplicationDbContext db) : IEventAccessService
{
    public async Task<bool> IsMemberAsync(Guid eventId, ClaimsPrincipal user, CancellationToken ct = default)
    {
        if (user.IsInRole(Roles.PlatformAdmin)) return true;
        var uid = user.GetUserId();
        return await db.EventMemberships.AsNoTracking().AnyAsync(
            m => m.EventId == eventId && m.UserId == uid && m.Status == MembershipStatus.Active, ct);
    }

    public async Task<bool> IsOrganizerAsync(Guid eventId, ClaimsPrincipal user, CancellationToken ct = default)
    {
        if (user.IsInRole(Roles.PlatformAdmin)) return true;
        var uid = user.GetUserId();
        return await db.EventMemberships.AsNoTracking().AnyAsync(
            m => m.EventId == eventId && m.UserId == uid && m.Status == MembershipStatus.Active
                 && m.RoleInEvent == EventRole.Organizer, ct);
    }
}
