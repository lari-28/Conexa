namespace Conexa.Api.Controllers;

[Route("api/events/{eventId:guid}/companies"), Authorize(Policy = Policies.EventMember)]
public class CompaniesController(ApplicationDbContext db, IEventAccessService access) : ApiControllerBase
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
            .Include(x => x.Tags).ThenInclude(t => t.Tag)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == companyId && x.EventId == eventId, ct);
        if (c is null) return NotFound();

        var reps = c.Members.Where(m => m.User.IsActive).Select(m =>
        {
            var (email, phone) = Mapping.AuthorizedContacts(m.User);
            return new Representative(m.UserId, m.User.DisplayName, m.User.Profile?.PhotoUrl, m.Title, email, phone);
        }).ToList();

        return new CompanyDetail(c.Id, c.Name, c.Description, c.Segment, c.ProductsServices,
            c.BoothLocation, c.Website, reps, c.IdealCustomer,
            c.Tags.Select(t => TaxonomyRules.ToResponse(t.Tag)).OrderBy(t => t.Name).ToList());
    }

    /// <summary>Estandes dos quais o usuário atual é representante neste evento.</summary>
    [HttpGet("mine")]
    public async Task<ActionResult<List<CompanySummary>>> Mine(Guid eventId, CancellationToken ct) =>
        await db.CompanyMemberships.AsNoTracking()
            .Where(m => m.UserId == CurrentUserId && m.Company.EventId == eventId)
            .Select(m => new CompanySummary(m.CompanyId, m.Company.Name, m.Company.Segment, m.Company.Description,
                m.Company.BoothLocation, m.Company.Members.Count))
            .ToListAsync(ct);

    /// <summary>Cadastra o estande. Expositor do evento (vira representante) ou organizador.</summary>
    [HttpPost]
    public async Task<ActionResult<CompanyDetail>> Create(Guid eventId, CompanyRequest req, CancellationToken ct)
    {
        var uid = CurrentUserId;
        var organizer = await access.IsOrganizerAsync(eventId, User, ct);
        var exhibitor = await db.EventMemberships.AnyAsync(m => m.EventId == eventId && m.UserId == uid
            && m.Status == MembershipStatus.Active && m.RoleInEvent == EventRole.Exhibitor, ct);
        if (!organizer && !exhibitor)
            return Problem(statusCode: 403, title: "Só expositores e organizadores cadastram estandes.");

        // Um expositor tem um estande por evento (evita duplicatas por duplo clique ou reenvio).
        if (!organizer && await db.CompanyMemberships.AnyAsync(m => m.UserId == uid && m.Company.EventId == eventId, ct))
            return Problem(statusCode: 409, title: "Você já tem um estande neste evento. Edite o existente.");

        var c = new Company { EventId = eventId };
        Apply(c, req);
        db.Companies.Add(c);
        if (exhibitor) db.CompanyMemberships.Add(new CompanyMembership { CompanyId = c.Id, UserId = uid });
        db.AuditLogs.Add(Audit(eventId, "company.create", nameof(Company), c.Id, c.Name));
        await db.SaveChangesAsync(ct);
        var created = await Get(eventId, c.Id, ct);
        return CreatedAtAction(nameof(Get), new { eventId, companyId = c.Id }, created.Value);
    }

    /// <summary>Edita o estande: organizador ou representante da empresa.</summary>
    [HttpPut("{companyId:guid}")]
    public async Task<ActionResult<CompanyDetail>> Update(Guid eventId, Guid companyId, CompanyRequest req, CancellationToken ct)
    {
        var c = await db.Companies.FirstOrDefaultAsync(x => x.Id == companyId && x.EventId == eventId, ct);
        if (c is null) return NotFound();
        var allowed = await access.IsOrganizerAsync(eventId, User, ct)
            || await db.CompanyMemberships.AnyAsync(m => m.CompanyId == companyId && m.UserId == CurrentUserId, ct);
        if (!allowed) return Forbid();

        Apply(c, req);
        db.AuditLogs.Add(Audit(eventId, "company.update", nameof(Company), c.Id, c.Name));
        await db.SaveChangesAsync(ct);
        return await Get(eventId, companyId, ct);
    }

    private static void Apply(Company c, CompanyRequest req)
    {
        c.Name = TaxonomyRules.CleanName(req.Name);
        c.Description = Mapping.Clean(req.Description);
        c.Segment = Mapping.Clean(req.Segment);
        c.ProductsServices = Mapping.Clean(req.ProductsServices);
        c.BoothLocation = Mapping.Clean(req.BoothLocation);
        c.Website = Mapping.Clean(req.Website);
        c.IdealCustomer = Mapping.Clean(req.IdealCustomer);
    }
}
