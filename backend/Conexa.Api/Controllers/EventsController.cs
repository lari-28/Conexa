namespace Conexa.Api.Controllers;

[Route("api/events")]
public class EventsController(ApplicationDbContext db) : ApiControllerBase
{
    [HttpGet("{eventId:guid}/maps"), Authorize(Policy = Policies.EventMember)]
    public async Task<ActionResult<List<EventMapResponse>>> Maps(Guid eventId, CancellationToken ct)
    {
        var maps = await db.EventMaps.AsNoTracking().Where(m => m.EventId == eventId)
            .OrderBy(m => m.CreatedAt).ToListAsync(ct);
        return maps.Select(m => new EventMapResponse(m.Id, m.Description, m.FileName,
            $"data:image/png;base64,{Convert.ToBase64String(m.Image)}", m.CreatedAt)).ToList();
    }

    /// <summary>Eventos do usuário (administrador vê todos os não arquivados).</summary>
    [HttpGet("mine")]
    public async Task<ActionResult<List<MyEventResponse>>> Mine(CancellationToken ct)
    {
        if (IsPlatformAdmin)
        {
            var all = await db.Events.AsNoTracking().Where(e => e.Status != EventStatus.Archived)
                .OrderBy(e => e.StartsAt).ToListAsync(ct);
            return all.Select(e => new MyEventResponse(Mapping.ToEvent(e), EventRole.Organizer)).ToList();
        }

        var uid = CurrentUserId;
        var rows = await db.EventMemberships.AsNoTracking()
            .Where(m => m.UserId == uid && m.Status == MembershipStatus.Active && m.Event.Status != EventStatus.Archived)
            .OrderBy(m => m.Event.StartsAt)
            .Select(m => new { m.Event, m.RoleInEvent })
            .ToListAsync(ct);
        return rows.Select(r => new MyEventResponse(Mapping.ToEvent(r.Event), r.RoleInEvent)).ToList();
    }

    [HttpGet("{eventId:guid}"), Authorize(Policy = Policies.EventMember)]
    public async Task<ActionResult<EventResponse>> Get(Guid eventId, CancellationToken ct)
    {
        var ev = await db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == eventId, ct);
        if (ev is null) return NotFound();
        return Mapping.ToEvent(ev);
    }
}
