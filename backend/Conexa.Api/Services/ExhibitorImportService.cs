namespace Conexa.Api.Services;

/// <summary>
/// Importa o catálogo de expositores de um CSV. Duplicados (por nome, ignorando acento e caixa) são pulados;
/// temas desconhecidos viram sugestões na fila de aprovação; o responsável (e-mail) é vinculado se já tiver conta.
/// </summary>
public class ExhibitorImportService(ApplicationDbContext db)
{
    public async Task<ImportReport> RunAsync(Guid eventId, string csv, bool commit, Guid actorId, CancellationToken ct)
    {
        var parsed = CatalogCsv.Parse(csv);
        if (parsed.FileErrors.Count > 0)
            return new ImportReport(false, 0, 0, 0, 0, [], parsed.FileErrors, []);

        var existing = (await db.Companies.AsNoTracking().Where(c => c.EventId == eventId).Select(c => c.Name).ToListAsync(ct))
            .Select(TaxonomyRules.Key).ToHashSet();
        var tags = await db.InterestTags.Where(t => t.EventId == eventId && t.Status != TagStatus.Rejected).ToListAsync(ct);
        var byName = new Dictionary<string, InterestTag>();
        foreach (var t in tags.Where(t => t.Status == TagStatus.Approved))
        {
            byName.TryAdd(TaxonomyRules.Key(t.Name), t);
            foreach (var s in t.Synonyms) byName.TryAdd(TaxonomyRules.Key(s), t);
        }
        var suggestedByKey = tags.Where(t => t.Status == TagStatus.Suggested)
            .GroupBy(t => TaxonomyRules.Key(t.Name)).ToDictionary(g => g.Key, g => g.First());

        var rows = new List<ImportRowResult>();
        var newSuggestions = new List<string>();
        int created = 0, skipped = 0, invalid = 0;
        var seen = new HashSet<string>(existing);

        foreach (var r in parsed.Rows)
        {
            var msgs = new List<string>(r.Problems);
            if (CatalogCsv.IsBlocking(r)) { invalid++; rows.Add(new(r.Line, r.Name, "Invalid", msgs)); continue; }

            var key = TaxonomyRules.Key(r.Name);
            if (!seen.Add(key))
            {
                skipped++; msgs.Add("Já existe um estande com este nome.");
                rows.Add(new(r.Line, r.Name, "Skipped", msgs)); continue;
            }

            var company = new Company
            {
                EventId = eventId, Name = r.Name, Segment = r.Segment, Description = r.Description,
                ProductsServices = r.Products, IdealCustomer = r.IdealCustomer, BoothLocation = r.Booth, Website = r.Website,
            };

            var themeIds = new List<Guid>();
            foreach (var theme in r.Themes)
            {
                var tk = TaxonomyRules.Key(theme);
                if (byName.TryGetValue(tk, out var known)) { themeIds.Add(known.Id); continue; }
                if (!suggestedByKey.TryGetValue(tk, out var sug))
                {
                    sug = new InterestTag { EventId = eventId, Name = theme, Status = TagStatus.Suggested };
                    suggestedByKey[tk] = sug;
                    newSuggestions.Add(theme);
                    if (commit) db.InterestTags.Add(sug);
                }
                msgs.Add($"Tema \"{theme}\" ainda não existe: enviado para aprovação do organizador.");
            }

            if (commit)
            {
                db.Companies.Add(company);
                foreach (var id in themeIds.Distinct())
                    db.CompanyTags.Add(new CompanyTag { CompanyId = company.Id, TagId = id });
                if (r.Email is not null) await LinkRepresentative(eventId, company, r.Email, msgs, ct);
            }
            else if (r.Email is not null) msgs.Add("O responsável será vinculado se já tiver conta.");

            created++;
            rows.Add(new(r.Line, r.Name, "Created", msgs));
        }

        if (commit && created > 0)
        {
            db.AuditLogs.Add(new AuditLog
            {
                EventId = eventId, ActorId = actorId, Action = "exhibitors.import", TargetType = nameof(Event),
                TargetId = eventId, Details = $"{created} criado(s), {skipped} pulado(s), {invalid} inválido(s)",
            });
            await db.SaveChangesAsync(ct);
        }
        return new ImportReport(commit, parsed.Rows.Count, created, skipped, invalid, rows, [], newSuggestions);
    }

    /// <summary>Vincula o responsável como expositor do evento e representante, se a conta já existir.</summary>
    private async Task LinkRepresentative(Guid eventId, Company company, string email, List<string> msgs, CancellationToken ct)
    {
        var normalized = email.Trim().ToUpperInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalized && u.IsActive, ct);
        if (user is null) { msgs.Add($"Sem conta para {email}: peça que se cadastre pelo convite e vincule depois."); return; }

        var m = await db.EventMemberships.FirstOrDefaultAsync(x => x.EventId == eventId && x.UserId == user.Id, ct);
        if (m is null)
            db.EventMemberships.Add(new EventMembership { EventId = eventId, UserId = user.Id, RoleInEvent = EventRole.Exhibitor });
        else if (m.Status != MembershipStatus.Active) { msgs.Add($"{email} está desativado no evento: não vinculado."); return; }
        // papel existente (inclusive organizador) é preservado

        db.CompanyMemberships.Add(new CompanyMembership { CompanyId = company.Id, UserId = user.Id });
        msgs.Add($"Responsável {email} vinculado.");
    }
}
