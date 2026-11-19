using System.ComponentModel.DataAnnotations;

namespace Conexa.Api.Controllers;

public record CheckInRequest([Required, StringLength(40)] string Code);
public record CheckInResponse(Guid CompanyId, string CompanyName, bool AlreadyRecorded);
public record FeedbackRequest(Guid CompanyId, bool Useful);
public record CheckInCodeResponse(string Code);

/// <summary>Check-in por QR no estande e feedback sobre recomendações. Alimentam os insights do evento.</summary>
[Route("api/events/{eventId:guid}"), Authorize(Policy = Policies.EventMember)]
public class EngagementController(ApplicationDbContext db, IEventAccessService access) : ApiControllerBase
{
    public static readonly TimeSpan CheckInCooldown = TimeSpan.FromMinutes(30);

    /// <summary>Código do QR do estande (gerado na primeira consulta). Organizador ou representante.</summary>
    [HttpGet("companies/{companyId:guid}/checkin-code")]
    public async Task<ActionResult<CheckInCodeResponse>> Code(Guid eventId, Guid companyId, CancellationToken ct)
    {
        var c = await db.Companies.FirstOrDefaultAsync(x => x.Id == companyId && x.EventId == eventId, ct);
        if (c is null) return NotFound();
        var allowed = await access.IsOrganizerAsync(eventId, User, ct)
            || await db.CompanyMemberships.AnyAsync(m => m.CompanyId == companyId && m.UserId == CurrentUserId, ct);
        if (!allowed) return Forbid();
        if (c.CheckInCode is null)
        {
            c.CheckInCode = EventInvite.NewCode();
            await db.SaveChangesAsync(ct);
        }
        return new CheckInCodeResponse(c.CheckInCode);
    }

    /// <summary>Registra a visita ao estande lido pelo QR. Repetir em até 30 minutos não duplica.</summary>
    [HttpPost("checkins")]
    public async Task<ActionResult<CheckInResponse>> CheckIn(Guid eventId, CheckInRequest req, CancellationToken ct)
    {
        var code = req.Code.Trim();
        var c = await db.Companies.AsNoTracking().FirstOrDefaultAsync(x => x.EventId == eventId && x.CheckInCode == code, ct);
        if (c is null) return Problem(statusCode: 404, title: "QR não reconhecido neste evento.");

        var since = DateTimeOffset.UtcNow - CheckInCooldown;
        var recent = await db.Interactions.AnyAsync(i => i.EventId == eventId && i.UserId == CurrentUserId
            && i.CompanyId == c.Id && i.Type == InteractionType.CheckIn && i.CreatedAt >= since, ct);
        if (!recent)
        {
            db.Interactions.Add(new Interaction { EventId = eventId, UserId = CurrentUserId, CompanyId = c.Id, Type = InteractionType.CheckIn });
            await db.SaveChangesAsync(ct);
        }
        return new CheckInResponse(c.Id, c.Name, recent);
    }

    /// <summary>"Isso foi útil?" sobre uma recomendação. Só vale para estandes que foram recomendados ao usuário.</summary>
    [HttpPost("matches/feedback")]
    public async Task<IActionResult> Feedback(Guid eventId, FeedbackRequest req, CancellationToken ct)
    {
        var shown = await db.MatchScores.AnyAsync(m => m.EventId == eventId && m.UserId == CurrentUserId
            && m.CompanyId == req.CompanyId && m.Band != "None", ct);
        if (!shown) return Problem(statusCode: 404, title: "Este estande não foi recomendado a você.");

        var type = req.Useful ? InteractionType.FeedbackUseful : InteractionType.FeedbackNotUseful;
        var previous = await db.Interactions.Where(i => i.EventId == eventId && i.UserId == CurrentUserId
            && i.CompanyId == req.CompanyId && (i.Type == InteractionType.FeedbackUseful || i.Type == InteractionType.FeedbackNotUseful))
            .ToListAsync(ct);
        db.Interactions.RemoveRange(previous.Where(p => p.Type != type));
        if (previous.All(p => p.Type != type))
            db.Interactions.Add(new Interaction { EventId = eventId, UserId = CurrentUserId, CompanyId = req.CompanyId, Type = type });
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}

public record NotificationResponse(Guid Id, Guid EventId, string Title, string Body, string? Link, DateTimeOffset CreatedAt, bool Read);

[Route("api/notifications")]
public class NotificationsController(ApplicationDbContext db) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<NotificationResponse>>> List(bool unreadOnly = false, int limit = 30, CancellationToken ct = default)
    {
        var q = db.Notifications.AsNoTracking().Where(n => n.UserId == CurrentUserId);
        if (unreadOnly) q = q.Where(n => n.ReadAt == null);
        return await q.OrderByDescending(n => n.CreatedAt).Take(Math.Clamp(limit, 1, 100))
            .Select(n => new NotificationResponse(n.Id, n.EventId, n.Title, n.Body, n.Link, n.CreatedAt, n.ReadAt != null))
            .ToListAsync(ct);
    }

    [HttpGet("count")]
    public async Task<ActionResult<int>> Count(CancellationToken ct) =>
        await db.Notifications.CountAsync(n => n.UserId == CurrentUserId && n.ReadAt == null, ct);

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> Read(Guid id, CancellationToken ct)
    {
        var n = await db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == CurrentUserId, ct);
        if (n is null) return NotFound();
        n.ReadAt ??= DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> ReadAll(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var items = await db.Notifications.Where(n => n.UserId == CurrentUserId && n.ReadAt == null).ToListAsync(ct);
        foreach (var n in items) n.ReadAt = now;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
