namespace Conexa.Api.Controllers;

[Route("api/events/{eventId:guid}/companies/{companyId:guid}/comments"), Authorize(Policy = Policies.EventMember)]
public class BoothCommentsController(ApplicationDbContext db, IEventAccessService access) : ApiControllerBase
{
    private const int MaxCommentsPerMinute = 6;

    [HttpGet]
    public async Task<ActionResult<BoothCommentsResponse>> List(
        Guid eventId, Guid companyId, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var companyExists = await db.Companies.AsNoTracking().AnyAsync(c => c.Id == companyId && c.EventId == eventId, ct);
        if (!companyExists) return NotFound();
        (page, pageSize) = Mapping.Clamp(page, pageSize, 100);

        if (!await CanViewAsync(eventId, companyId, ct))
            return new BoothCommentsResponse(false, [], page, pageSize, 0);

        var query = db.BoothComments.AsNoTracking()
            .Where(c => c.CompanyId == companyId && c.Status == ContentStatus.Visible);
        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(c => new
            {
                c.Id, c.AuthorId, Name = c.Author.DisplayName, Photo = c.Author.Profile!.PhotoUrl,
                c.Text, c.CreatedAt
            })
            .ToListAsync(ct);
        var isOrganizer = await access.IsOrganizerAsync(eventId, User, ct);
        var items = rows.Select(c => new BoothCommentResponse(c.Id, companyId,
            new AuthorInfo(c.AuthorId, c.Name, c.Photo), c.Text, c.CreatedAt,
            c.AuthorId == CurrentUserId || isOrganizer)).ToList();
        return new BoothCommentsResponse(true, items, page, pageSize, total);
    }

    [HttpPost]
    public async Task<ActionResult<BoothCommentResponse>> Create(
        Guid eventId, Guid companyId, CommentRequest req, CancellationToken ct)
    {
        var company = await db.Companies.FirstOrDefaultAsync(c => c.Id == companyId && c.EventId == eventId, ct);
        if (company is null) return NotFound();
        if (!await CanViewAsync(eventId, companyId, ct))
            return Problem(statusCode: 403, title: "Faça check-in neste estande para participar da conversa.");

        var uid = CurrentUserId;
        var since = DateTimeOffset.UtcNow.AddMinutes(-1);
        if (await db.BoothComments.CountAsync(c => c.AuthorId == uid && c.CreatedAt > since, ct) >= MaxCommentsPerMinute)
            return Problem(statusCode: 429, title: "Você está comentando rápido demais. Aguarde um instante.");

        var comment = new BoothComment { CompanyId = companyId, AuthorId = uid, Text = req.Text.Trim() };
        db.BoothComments.Add(comment);
        db.AuditLogs.Add(Audit(eventId, "booth.comment.create", nameof(BoothComment), comment.Id, company.Name));
        await db.SaveChangesAsync(ct);

        var author = await db.Users.AsNoTracking().Where(u => u.Id == uid)
            .Select(u => new AuthorInfo(u.Id, u.DisplayName, u.Profile!.PhotoUrl)).FirstAsync(ct);
        return StatusCode(StatusCodes.Status201Created,
            new BoothCommentResponse(comment.Id, companyId, author, comment.Text, comment.CreatedAt, true));
    }

    private async Task<bool> CanViewAsync(Guid eventId, Guid companyId, CancellationToken ct)
    {
        var isParticipant = await db.EventMemberships.AsNoTracking().AnyAsync(m => m.EventId == eventId
            && m.UserId == CurrentUserId && m.Status == MembershipStatus.Active
            && m.RoleInEvent == EventRole.Participant, ct);
        if (!isParticipant) return false;

        return await db.Interactions.AsNoTracking().AnyAsync(i => i.EventId == eventId
            && i.CompanyId == companyId && i.UserId == CurrentUserId && i.Type == InteractionType.CheckIn, ct);
    }
}
