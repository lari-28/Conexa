namespace Conexa.Api.Controllers;

/// <summary>Painel global da plataforma (RF21). Restrito ao papel PlatformAdmin.</summary>
[Route("api/admin"), Authorize(Roles = Roles.PlatformAdmin)]
public class AdminController(ApplicationDbContext db, UserManager<ApplicationUser> users) : ApiControllerBase
{
    [HttpGet("events")]
    public async Task<ActionResult<List<EventResponse>>> Events(CancellationToken ct)
    {
        var events = await db.Events.AsNoTracking().OrderByDescending(e => e.StartsAt).ToListAsync(ct);
        return events.Select(Mapping.ToEvent).ToList();
    }

    [HttpPost("events")]
    public async Task<ActionResult<EventResponse>> CreateEvent(EventRequest req, CancellationToken ct)
    {
        if (req.EndsAt < req.StartsAt)
            return ValidationFailure(nameof(req.EndsAt), "O término deve ser posterior ao início.");
        var ev = new Event
        {
            Name = req.Name.Trim(), Description = Mapping.Clean(req.Description),
            StartsAt = req.StartsAt.ToUniversalTime(), EndsAt = req.EndsAt.ToUniversalTime(),
            Location = Mapping.Clean(req.Location), Status = req.Status ?? EventStatus.Draft
        };
        db.Events.Add(ev);
        db.AuditLogs.Add(Audit(ev.Id, "event.create", nameof(Event), ev.Id));
        await db.SaveChangesAsync(ct);
        return StatusCode(StatusCodes.Status201Created, Mapping.ToEvent(ev));
    }

    [HttpGet("users")]
    public async Task<ActionResult<PagedResponse<AdminUserResponse>>> Users(
        string? q, int page = 1, int pageSize = 30, CancellationToken ct = default)
    {
        (page, pageSize) = Mapping.Clamp(page, pageSize, 100);
        var query = db.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var like = $"%{Mapping.EscapeLike(q.Trim())}%";
            query = query.Where(u => EF.Functions.ILike(u.DisplayName, like) || EF.Functions.ILike(u.Email!, like));
        }
        var total = await query.CountAsync(ct);
        var rows = await query.OrderBy(u => u.DisplayName).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(u => new { u.Id, u.DisplayName, Email = u.Email ?? "", u.IsActive, u.CreatedAt })
            .ToListAsync(ct);

        var ids = rows.Select(r => r.Id).ToList();
        var roleRows = await (from ur in db.UserRoles
                              join r in db.Roles on ur.RoleId equals r.Id
                              where ids.Contains(ur.UserId)
                              select new { ur.UserId, Role = r.Name }).ToListAsync(ct);
        var roles = roleRows.GroupBy(x => x.UserId).ToDictionary(g => g.Key, g => g.Select(x => x.Role ?? "").ToList());

        var items = rows.Select(r => new AdminUserResponse(r.Id, r.DisplayName, r.Email, r.IsActive,
            roles.GetValueOrDefault(r.Id) ?? new List<string>(), r.CreatedAt)).ToList();
        return new PagedResponse<AdminUserResponse>(items, page, pageSize, total);
    }

    /// <summary>Suspende/reativa a conta globalmente. Os registros são preservados (RN09).</summary>
    [HttpPatch("users/{id:guid}")]
    public async Task<IActionResult> SetActive(Guid id, SetUserActiveRequest req, CancellationToken ct)
    {
        if (id == CurrentUserId) return Problem(statusCode: 400, title: "Você não pode suspender a própria conta.");
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null) return NotFound();
        user.IsActive = req.IsActive;
        db.AuditLogs.Add(Audit(null, req.IsActive ? "user.enable" : "user.disable", nameof(ApplicationUser), id));
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("events/{eventId:guid}/organizers")]
    public async Task<IActionResult> AddOrganizer(Guid eventId, AddOrganizerRequest req, CancellationToken ct)
    {
        if (!await db.Events.AnyAsync(e => e.Id == eventId, ct)) return NotFound();
        var user = await users.FindByEmailAsync(req.Email.Trim());
        if (user is null) return Problem(statusCode: 404, title: "Usuário não encontrado.");

        var m = await db.EventMemberships.FindAsync(new object[] { eventId, user.Id }, ct);
        if (m is null)
            db.EventMemberships.Add(new EventMembership { EventId = eventId, UserId = user.Id, RoleInEvent = EventRole.Organizer });
        else { m.RoleInEvent = EventRole.Organizer; m.Status = MembershipStatus.Active; }

        if (!await users.IsInRoleAsync(user, Roles.Organizer)) await users.AddToRoleAsync(user, Roles.Organizer);
        db.AuditLogs.Add(Audit(eventId, "organizer.add", nameof(ApplicationUser), user.Id));
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
