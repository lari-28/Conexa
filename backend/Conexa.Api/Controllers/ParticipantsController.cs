namespace Conexa.Api.Controllers;

[Route("api/events/{eventId:guid}/participants"), Authorize(Policy = Policies.EventMember)]
public class ParticipantsController(ApplicationDbContext db) : ApiControllerBase
{
    private IQueryable<EventMembership> ActiveMembers(Guid eventId) =>
        db.EventMemberships.AsNoTracking()
            .Where(m => m.EventId == eventId && m.Status == MembershipStatus.Active && m.User.IsActive);

    /// <summary>Lista paginada. Nunca inclui contatos (RF07); contatos só no detalhe, se autorizados.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<ParticipantSummary>>> List(
        Guid eventId, string? q, string? area, string? interest, int page = 1, int pageSize = 20,
        CancellationToken ct = default)
    {
        (page, pageSize) = Mapping.Clamp(page, pageSize);
        var query = ActiveMembers(eventId);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var like = $"%{Mapping.EscapeLike(q.Trim())}%";
            query = query.Where(m => EF.Functions.ILike(m.User.DisplayName, like)
                || EF.Functions.ILike(m.User.Profile!.CompanyName!, like)
                || EF.Functions.ILike(m.User.Profile!.Area!, like));
        }
        if (!string.IsNullOrWhiteSpace(area))
        {
            var a = Mapping.EscapeLike(area.Trim());
            query = query.Where(m => EF.Functions.ILike(m.User.Profile!.Area!, a));
        }
        if (!string.IsNullOrWhiteSpace(interest))
        {
            var i = interest.Trim().ToLowerInvariant();
            query = query.Where(m => m.User.Profile!.Interests.Contains(i));
        }

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(m => m.User.DisplayName)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(m => new ParticipantSummary(m.UserId, m.User.DisplayName, m.User.Profile!.PhotoUrl,
                m.User.Profile.CompanyName, m.User.Profile.JobTitle, m.User.Profile.Area,
                m.User.Profile.Interests, m.User.Profile.LookingFor, m.RoleInEvent))
            .ToListAsync(ct);
        return new PagedResponse<ParticipantSummary>(items, page, pageSize, total);
    }

    /// <summary>Valores disponíveis para os filtros de busca.</summary>
    [HttpGet("filters")]
    public async Task<ActionResult<ParticipantFilters>> Filters(Guid eventId, CancellationToken ct)
    {
        var rows = await ActiveMembers(eventId)
            .Select(m => new { Area = m.User.Profile!.Area, Interests = m.User.Profile.Interests })
            .ToListAsync(ct);
        var areas = rows.Select(r => r.Area).Where(a => !string.IsNullOrWhiteSpace(a))
            .Select(a => a!).Distinct(StringComparer.OrdinalIgnoreCase).Order().ToList();
        var interests = rows.SelectMany(r => r.Interests).Distinct().Order().ToList();
        return new ParticipantFilters(areas, interests);
    }

    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<ParticipantDetailResponse>> Get(Guid eventId, Guid userId, CancellationToken ct)
    {
        var membership = await ActiveMembers(eventId).FirstOrDefaultAsync(m => m.UserId == userId, ct);
        if (membership is null) return NotFound();

        var user = await db.Users.AsNoTracking().Include(u => u.Profile).Include(u => u.Privacy)
            .FirstAsync(u => u.Id == userId, ct);
        var companies = await db.CompanyMemberships.AsNoTracking()
            .Where(cm => cm.UserId == userId && cm.Company.EventId == eventId)
            .Select(cm => new CompanyRef(cm.CompanyId, cm.Company.Name)).ToListAsync(ct);
        return new ParticipantDetailResponse(Mapping.ToPublic(user), membership.RoleInEvent, companies);
    }
}
