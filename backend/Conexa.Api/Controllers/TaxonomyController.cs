namespace Conexa.Api.Controllers;

/// <summary>
/// Taxonomia por evento (2 níveis). O organizador aprova e ajusta; visitantes e expositores sugerem.
/// A fila de aprovação trabalha em lote, agrupando sugestões parecidas.
/// </summary>
[Route("api/events/{eventId:guid}/taxonomy"), Authorize(Policy = Policies.EventMember)]
public class TaxonomyController(ApplicationDbContext db, IEventAccessService access) : ApiControllerBase
{
    private const int MaxTagsPerEvent = 500;

    // ---------- Leitura ----------

    /// <summary>Membros veem só os temas aprovados; o organizador pode filtrar por status.</summary>
    [HttpGet]
    public async Task<ActionResult<List<TagResponse>>> List(Guid eventId, TagStatus? status, CancellationToken ct)
    {
        var effective = TagStatus.Approved;
        if (status is { } s && s != TagStatus.Approved)
        {
            if (!await access.IsOrganizerAsync(eventId, User, ct)) return Forbid();
            effective = s;
        }
        var tags = await db.InterestTags.AsNoTracking()
            .Where(t => t.EventId == eventId && t.Status == effective)
            .OrderBy(t => t.Name).ToListAsync(ct);
        return tags.Select(TaxonomyRules.ToResponse).ToList();
    }

    /// <summary>Setores disponíveis na biblioteca-base.</summary>
    [HttpGet("library")]
    public ActionResult<List<LibrarySector>> Library() =>
        TaxonomyLibrary.Sectors.Select(s => new LibrarySector(s.Key, s.Name, s.Areas.Length,
            s.Areas.Sum(a => a.Themes.Length))).ToList();

    // ---------- Organizador: cadastro direto ----------

    [HttpPost, Authorize(Policy = Policies.EventOrganizer)]
    public async Task<ActionResult<TagResponse>> Create(Guid eventId, TagRequest req, CancellationToken ct)
    {
        var name = TaxonomyRules.CleanName(req.Name);
        var (all, error) = await LoadEventTags(eventId, ct);
        if (error is not null) return error;
        var problem = CheckParentAndDuplicate(eventId, all, req.ParentId, name, null);
        if (problem is not null) return problem;

        var tag = new InterestTag
        {
            EventId = eventId, ParentId = req.ParentId, Name = name,
            Description = Mapping.Clean(req.Description),
            Synonyms = TaxonomyRules.NormalizeSynonyms(req.Synonyms, name),
            Status = TagStatus.Approved,
        };
        db.InterestTags.Add(tag);
        db.AuditLogs.Add(Audit(eventId, "taxonomy.create", nameof(InterestTag), tag.Id, name));
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(List), new { eventId }, TaxonomyRules.ToResponse(tag));
    }

    [HttpPut("{tagId:guid}"), Authorize(Policy = Policies.EventOrganizer)]
    public async Task<ActionResult<TagResponse>> Update(Guid eventId, Guid tagId, TagRequest req, CancellationToken ct)
    {
        var tag = await db.InterestTags.FirstOrDefaultAsync(t => t.Id == tagId && t.EventId == eventId, ct);
        if (tag is null) return NotFound();
        var name = TaxonomyRules.CleanName(req.Name);
        var (all, error) = await LoadEventTags(eventId, ct);
        if (error is not null) return error;

        if (req.ParentId != tag.ParentId && all.Any(t => t.ParentId == tag.Id))
            return ValidationFailure(nameof(req.ParentId), "Uma macro-área com temas não pode virar tema.");
        var problem = CheckParentAndDuplicate(eventId, all, req.ParentId, name, tag.Id);
        if (problem is not null) return problem;

        tag.Name = name;
        tag.ParentId = req.ParentId;
        tag.Description = Mapping.Clean(req.Description);
        tag.Synonyms = TaxonomyRules.NormalizeSynonyms(req.Synonyms, name);
        db.AuditLogs.Add(Audit(eventId, "taxonomy.update", nameof(InterestTag), tag.Id, name));
        await db.SaveChangesAsync(ct);
        return TaxonomyRules.ToResponse(tag);
    }

    /// <summary>Arquiva o tema (status Rejeitado). Vínculos existentes são preservados.</summary>
    [HttpDelete("{tagId:guid}"), Authorize(Policy = Policies.EventOrganizer)]
    public async Task<IActionResult> Archive(Guid eventId, Guid tagId, CancellationToken ct)
    {
        var tag = await db.InterestTags.FirstOrDefaultAsync(t => t.Id == tagId && t.EventId == eventId, ct);
        if (tag is null) return NotFound();
        if (await db.InterestTags.AnyAsync(t => t.ParentId == tag.Id && t.Status != TagStatus.Rejected, ct))
            return Problem(statusCode: 409, title: "Arquive ou mova os temas desta macro-área primeiro.");
        tag.Status = TagStatus.Rejected;
        db.AuditLogs.Add(Audit(eventId, "taxonomy.archive", nameof(InterestTag), tag.Id, tag.Name));
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ---------- Sugestões e fila de aprovação ----------

    /// <summary>Qualquer membro sugere um tema. Se já existir, devolve o existente em vez de duplicar.</summary>
    [HttpPost("suggestions")]
    public async Task<ActionResult<TagResponse>> Suggest(Guid eventId, SuggestTagRequest req, CancellationToken ct)
    {
        var name = TaxonomyRules.CleanName(req.Name);
        var key = TaxonomyRules.Key(name);
        var (all, error) = await LoadEventTags(eventId, ct);
        if (error is not null) return error;

        var existing = all.FirstOrDefault(t => t.Status != TagStatus.Rejected
            && (TaxonomyRules.Key(t.Name) == key || t.Synonyms.Any(s => TaxonomyRules.Key(s) == key)));
        if (existing is not null) return Ok(TaxonomyRules.ToResponse(existing));

        if (req.ParentId is { } pid)
        {
            var parent = all.FirstOrDefault(t => t.Id == pid);
            var err = TaxonomyRules.ValidateParent(parent, eventId, null);
            if (err is not null) return ValidationFailure(nameof(req.ParentId), err);
        }
        var tag = new InterestTag
        {
            EventId = eventId, ParentId = req.ParentId, Name = name,
            Description = Mapping.Clean(req.Description), Status = TagStatus.Suggested,
        };
        db.InterestTags.Add(tag);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(List), new { eventId }, TaxonomyRules.ToResponse(tag));
    }

    /// <summary>Fila do organizador: sugestões pendentes, agrupadas por nome parecido.</summary>
    [HttpGet("suggestions"), Authorize(Policy = Policies.EventOrganizer)]
    public async Task<ActionResult<List<SuggestionGroup>>> Suggestions(Guid eventId, CancellationToken ct)
    {
        var tags = await db.InterestTags.AsNoTracking().Where(t => t.EventId == eventId
            && (t.Status == TagStatus.Suggested || t.Status == TagStatus.Approved)).ToListAsync(ct);
        return TaxonomyRules.GroupSuggestions(
            tags.Where(t => t.Status == TagStatus.Suggested),
            tags.Where(t => t.Status == TagStatus.Approved).ToList());
    }

    /// <summary>Aprova, rejeita ou funde várias sugestões de uma vez.</summary>
    [HttpPost("review"), Authorize(Policy = Policies.EventOrganizer)]
    public async Task<ActionResult<ReviewResult>> Review(Guid eventId, ReviewTagsRequest req, CancellationToken ct)
    {
        var ids = req.TagIds.Distinct().ToList();
        var tags = await db.InterestTags.Where(t => t.EventId == eventId && ids.Contains(t.Id)).ToListAsync(ct);
        if (tags.Count != ids.Count)
            return ValidationFailure(nameof(req.TagIds), "Algum tema não existe neste evento.");

        int approved = 0, rejected = 0, merged = 0;
        switch (req.Action)
        {
            case ReviewAction.Approve:
            {
                var parentIds = tags.Where(t => t.ParentId is not null).Select(t => t.ParentId!.Value).Distinct().ToList();
                var parents = await db.InterestTags.AsNoTracking()
                    .Where(p => p.EventId == eventId && parentIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);
                foreach (var t in tags)
                {
                    var parentOk = t.ParentId is null || parents.TryGetValue(t.ParentId.Value, out var p)
                        && (p.Status == TagStatus.Approved || ids.Contains(p.Id));
                    if (!parentOk)
                        return ValidationFailure(nameof(req.TagIds), $"Aprove antes a macro-área de \"{t.Name}\".");
                }
                foreach (var t in tags) { t.Status = TagStatus.Approved; approved++; }
                break;
            }
            case ReviewAction.Reject:
                foreach (var t in tags) { t.Status = TagStatus.Rejected; rejected++; }
                break;
            case ReviewAction.Merge:
            {
                if (req.MergeIntoId is not { } targetId)
                    return ValidationFailure(nameof(req.MergeIntoId), "Informe o tema de destino da fusão.");
                var target = await db.InterestTags.FirstOrDefaultAsync(t => t.Id == targetId && t.EventId == eventId, ct);
                if (target is null || target.Status != TagStatus.Approved || ids.Contains(targetId))
                    return ValidationFailure(nameof(req.MergeIntoId), "O destino precisa ser um tema aprovado fora da seleção.");
                if (await db.InterestTags.AnyAsync(t => t.ParentId != null && ids.Contains(t.ParentId.Value), ct))
                    return ValidationFailure(nameof(req.TagIds), "Não é possível fundir uma macro-área que tem temas.");
                foreach (var t in tags) { await MergeInto(eventId, t, target, ct); merged++; }
                break;
            }
        }

        db.AuditLogs.Add(Audit(eventId, $"taxonomy.review.{req.Action.ToString().ToLowerInvariant()}",
            nameof(InterestTag), ids[0], $"{ids.Count} tema(s)"));
        await db.SaveChangesAsync(ct);
        return new ReviewResult(approved, rejected, merged);
    }

    // ---------- Biblioteca-base ----------

    /// <summary>Importa os temas de um setor como aprovados, sem duplicar o que já existe.</summary>
    [HttpPost("library/import"), Authorize(Policy = Policies.EventOrganizer)]
    public async Task<ActionResult<List<TagResponse>>> ImportLibrary(Guid eventId, ImportLibraryRequest req, CancellationToken ct)
    {
        var sector = TaxonomyLibrary.Find(req.Sector);
        if (sector is null) return ValidationFailure(nameof(req.Sector), "Setor não encontrado na biblioteca.");

        var (all, error) = await LoadEventTags(eventId, ct);
        if (error is not null) return error;
        var created = new List<InterestTag>();

        foreach (var area in sector.Areas)
        {
            var areaTag = all.Concat(created).FirstOrDefault(t => t.ParentId is null && t.Status != TagStatus.Rejected
                && (t.LibraryKey == area.Key || TaxonomyRules.Key(t.Name) == TaxonomyRules.Key(area.Name)));
            if (areaTag is null)
            {
                areaTag = new InterestTag { EventId = eventId, Name = area.Name, LibraryKey = area.Key, Status = TagStatus.Approved };
                created.Add(areaTag);
                db.InterestTags.Add(areaTag);
            }
            foreach (var th in area.Themes)
            {
                var exists = all.Concat(created).Any(t => t.Status != TagStatus.Rejected
                    && (t.LibraryKey == th.Key || TaxonomyRules.Key(t.Name) == TaxonomyRules.Key(th.Name)));
                if (exists) continue;
                var tag = new InterestTag
                {
                    EventId = eventId, ParentId = areaTag.Id, Name = th.Name, Description = th.Description,
                    Synonyms = TaxonomyRules.NormalizeSynonyms(th.Synonyms, th.Name),
                    LibraryKey = th.Key, Status = TagStatus.Approved,
                };
                created.Add(tag);
                db.InterestTags.Add(tag);
            }
        }
        if (all.Count + created.Count > MaxTagsPerEvent)
            return Problem(statusCode: 409, title: "Limite de temas por evento excedido.");

        db.AuditLogs.Add(Audit(eventId, "taxonomy.import", nameof(Event), eventId, $"{sector.Key}: {created.Count} tema(s)"));
        await db.SaveChangesAsync(ct);
        return created.Select(TaxonomyRules.ToResponse).ToList();
    }

    // ---------- Temas do estande ----------

    [HttpGet("/api/events/{eventId:guid}/companies/{companyId:guid}/tags")]
    public async Task<ActionResult<List<TagResponse>>> CompanyTags(Guid eventId, Guid companyId, CancellationToken ct)
    {
        if (!await db.Companies.AnyAsync(c => c.Id == companyId && c.EventId == eventId, ct)) return NotFound();
        var tags = await db.CompanyTags.AsNoTracking().Where(ct2 => ct2.CompanyId == companyId)
            .Select(ct2 => ct2.Tag).OrderBy(t => t.Name).ToListAsync(ct);
        return tags.Select(TaxonomyRules.ToResponse).ToList();
    }

    /// <summary>Define os temas do estande (até 3, aprovados). Só o organizador ou um representante da empresa.</summary>
    [HttpPut("/api/events/{eventId:guid}/companies/{companyId:guid}/tags")]
    public async Task<ActionResult<List<TagResponse>>> SetCompanyTags(
        Guid eventId, Guid companyId, SetCompanyTagsRequest req, CancellationToken ct)
    {
        var company = await db.Companies.Include(c => c.Tags)
            .FirstOrDefaultAsync(c => c.Id == companyId && c.EventId == eventId, ct);
        if (company is null) return NotFound();

        var allowed = await access.IsOrganizerAsync(eventId, User, ct)
            || await db.CompanyMemberships.AnyAsync(m => m.CompanyId == companyId && m.UserId == CurrentUserId, ct);
        if (!allowed) return Forbid();

        var ids = req.TagIds.Distinct().ToList();
        var found = await db.InterestTags.Where(t => ids.Contains(t.Id)).ToListAsync(ct);
        var err = TaxonomyRules.ValidateCompanyTags(ids, found, eventId);
        if (err is not null) return ValidationFailure(nameof(req.TagIds), err);

        company.Tags.Clear();
        foreach (var t in found) company.Tags.Add(new CompanyTag { CompanyId = companyId, TagId = t.Id });
        db.AuditLogs.Add(Audit(eventId, "company.tags", nameof(Company), companyId, $"{found.Count} tema(s)"));
        await db.SaveChangesAsync(ct);
        return found.OrderBy(t => t.Name).Select(TaxonomyRules.ToResponse).ToList();
    }

    // ---------- Auxiliares ----------

    private async Task<(List<InterestTag> All, ActionResult? Error)> LoadEventTags(Guid eventId, CancellationToken ct)
    {
        var all = await db.InterestTags.AsNoTracking().Where(t => t.EventId == eventId).ToListAsync(ct);
        return (all, all.Count >= MaxTagsPerEvent
            ? Problem(statusCode: 409, title: "Limite de temas por evento excedido.") : null);
    }

    private ActionResult? CheckParentAndDuplicate(
        Guid eventId, List<InterestTag> all, Guid? parentId, string name, Guid? selfId)
    {
        ActionResult? result = null;
        if (parentId is { } pid)
        {
            var err = TaxonomyRules.ValidateParent(all.FirstOrDefault(t => t.Id == pid), eventId, selfId);
            if (err is not null) result = ValidationFailure(nameof(TagRequest.ParentId), err);
        }
        if (result is null)
        {
            var key = TaxonomyRules.Key(name);
            var dup = all.Any(t => t.Id != selfId && t.Status != TagStatus.Rejected && t.ParentId == parentId
                && TaxonomyRules.Key(t.Name) == key);
            if (dup) result = ValidationFailure(nameof(TagRequest.Name), "Já existe um tema com este nome neste nível.");
        }
        return result;
    }

    /// <summary>Passa os vínculos do tema sugerido para o destino e guarda o nome como sinônimo.</summary>
    private async Task MergeInto(Guid eventId, InterestTag source, InterestTag target, CancellationToken ct)
    {
        var synonyms = new List<string>(target.Synonyms) { source.Name };
        synonyms.AddRange(source.Synonyms);
        target.Synonyms = TaxonomyRules.NormalizeSynonyms(synonyms, target.Name);

        var companyLinks = await db.CompanyTags.Where(x => x.TagId == source.Id).ToListAsync(ct);
        var companiesWithTarget = (await db.CompanyTags.Where(x => x.TagId == target.Id)
            .Select(x => x.CompanyId).ToListAsync(ct)).ToHashSet();
        foreach (var l in companyLinks)
        {
            db.CompanyTags.Remove(l);
            if (companiesWithTarget.Add(l.CompanyId))
                db.CompanyTags.Add(new CompanyTag { CompanyId = l.CompanyId, TagId = target.Id });
        }

        var visitorLinks = await db.ParticipantTags.Where(x => x.EventId == eventId && x.TagId == source.Id).ToListAsync(ct);
        var visitorsWithTarget = (await db.ParticipantTags.Where(x => x.EventId == eventId && x.TagId == target.Id)
            .Select(x => x.UserId).ToListAsync(ct)).ToHashSet();
        foreach (var l in visitorLinks)
        {
            db.ParticipantTags.Remove(l);
            if (visitorsWithTarget.Add(l.UserId))
                db.ParticipantTags.Add(new ParticipantTag { EventId = eventId, UserId = l.UserId, TagId = target.Id });
        }
        db.InterestTags.Remove(source);
    }
}
