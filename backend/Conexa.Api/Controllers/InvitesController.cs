namespace Conexa.Api.Controllers;

[Route("api")]
public class InvitesController(ApplicationDbContext db) : ApiControllerBase
{
    private static InviteResponse ToResponse(EventInvite i, DateTimeOffset now) =>
        new(i.Id, i.Code, i.Role, i.MaxUses, i.Uses, i.ExpiresAt, i.RevokedAt, i.InvalidReason(now) is null, i.CreatedAt);

    /// <summary>Pré-visualização pública do convite (usada pela página do link).</summary>
    [HttpGet("invites/{code}"), AllowAnonymous]
    public async Task<ActionResult<InvitePreview>> Preview(string code, CancellationToken ct)
    {
        var inv = await db.EventInvites.AsNoTracking().Include(i => i.Event)
            .FirstOrDefaultAsync(i => i.Code == code, ct);
        if (inv is null) return new InvitePreview(false, "Convite não encontrado.", null, null);
        var reason = inv.InvalidReason(DateTimeOffset.UtcNow);
        return reason is null
            ? new InvitePreview(true, null, inv.Event.Name, inv.Role)
            : new InvitePreview(false, reason, null, null);
    }

    [HttpGet("organizer/events/{eventId:guid}/invites"), Authorize(Policy = Policies.EventOrganizer)]
    public async Task<ActionResult<List<InviteResponse>>> List(Guid eventId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var items = await db.EventInvites.AsNoTracking().Where(i => i.EventId == eventId)
            .OrderByDescending(i => i.CreatedAt).ToListAsync(ct);
        return items.Select(i => ToResponse(i, now)).ToList();
    }

    [HttpPost("organizer/events/{eventId:guid}/invites"), Authorize(Policy = Policies.EventOrganizer)]
    public async Task<ActionResult<InviteResponse>> Create(Guid eventId, CreateInviteRequest req, CancellationToken ct)
    {
        if (req.Role == EventRole.Organizer)
            return ValidationFailure(nameof(req.Role), "Convites só podem ser de visitante ou expositor.");
        var now = DateTimeOffset.UtcNow;
        var inv = new EventInvite
        {
            EventId = eventId, Role = req.Role, MaxUses = req.MaxUses, CreatedById = CurrentUserId,
            ExpiresAt = req.ExpiresInDays is { } d ? now.AddDays(d) : null,
        };
        db.EventInvites.Add(inv);
        db.AuditLogs.Add(Audit(eventId, "invite.create", nameof(EventInvite), inv.Id, req.Role.ToString()));
        await db.SaveChangesAsync(ct);
        return Ok(ToResponse(inv, now));
    }

    [HttpDelete("organizer/events/{eventId:guid}/invites/{inviteId:guid}"), Authorize(Policy = Policies.EventOrganizer)]
    public async Task<IActionResult> Revoke(Guid eventId, Guid inviteId, CancellationToken ct)
    {
        var inv = await db.EventInvites.FirstOrDefaultAsync(i => i.Id == inviteId && i.EventId == eventId, ct);
        if (inv is null) return NotFound();
        inv.RevokedAt ??= DateTimeOffset.UtcNow;
        db.AuditLogs.Add(Audit(eventId, "invite.revoke", nameof(EventInvite), inv.Id));
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
