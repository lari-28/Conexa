namespace Conexa.Api.Controllers;

[Route("api/events/{eventId:guid}/companies"), Authorize(Policy = Policies.EventMember)]
public class CompaniesController(ApplicationDbContext db) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<CompanySummary>>> List(
        Guid eventId, string? q, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        (page, pageSize) = Mapping.Clamp(page, pageSize);
        var query = db.Companies.AsNoTracking().Where(c => c.EventId == eventId);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var like = $"%{Mapping.EscapeLike(q.Trim())}%";
            query = query.Where(c => EF.Functions.ILike(c.Name, like) || EF.Functions.ILike(c.Segment!, like));
        }
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(c => c.Name).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(c => new CompanySummary(c.Id, c.Name, c.Segment, c.Description, c.BoothLocation, c.Members.Count))
            .ToListAsync(ct);
        return new PagedResponse<CompanySummary>(items, page, pageSize, total);
    }

    [HttpGet("{companyId:guid}")]
    public async Task<ActionResult<CompanyDetail>> Get(Guid eventId, Guid companyId, CancellationToken ct)
    {
        var c = await db.Companies.AsNoTracking()
            .Include(x => x.Members).ThenInclude(m => m.User).ThenInclude(u => u.Profile)
            .Include(x => x.Members).ThenInclude(m => m.User).ThenInclude(u => u.Privacy)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == companyId && x.EventId == eventId, ct);
        if (c is null) return NotFound();

        var reps = c.Members.Where(m => m.User.IsActive).Select(m =>
        {
            var (email, phone) = Mapping.AuthorizedContacts(m.User);
            return new Representative(m.UserId, m.User.DisplayName, m.User.Profile?.PhotoUrl, m.Title, email, phone);
        }).ToList();

        return new CompanyDetail(c.Id, c.Name, c.Description, c.Segment, c.ProductsServices,
            c.BoothLocation, c.Website, reps);
    }
}
