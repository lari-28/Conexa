namespace Conexa.Api.Services.Matching;

public record BoothMatch(Guid CompanyId, string Name, string? Segment, string? BoothLocation, double Score,
    MatchBand Band, IReadOnlyList<string> Reasons);

public record LeadMatch(Guid UserId, string DisplayName, string? CompanyName, string? JobTitle, string? Goal,
    double Score, MatchBand Band, IReadOnlyList<string> Reasons, string? Email, string? Phone);

/// <summary>Carrega os dados do evento, aplica o motor e guarda os scores. Só usa quem consentiu.</summary>
public class MatchingService(ApplicationDbContext db)
{
    public const int MaxResults = 20;

    private record Loaded(MatchingEngine Engine, List<MatchBooth> Booths, Dictionary<Guid, Company> Companies);

    /// <summary>Estandes recomendados ao próprio visitante. Exige consentimento de matching.</summary>
    public async Task<(bool Ready, List<BoothMatch> Items)> BoothsForAsync(Guid eventId, Guid userId, int limit, CancellationToken ct)
    {
        var p = await db.ParticipantEventProfiles.AsNoTracking().Include(x => x.Tags)
            .FirstOrDefaultAsync(x => x.EventId == eventId && x.UserId == userId, ct);
        if (p is null || !VisitorProfileRules.IsReadyForMatching(p.Goal, p.Tags.Count, p.ConsentMatchingAt))
            return (false, []);

        var visitor = await ToVisitor(p, ct);
        var loaded = await Load(eventId, ct);
        var results = loaded.Engine.RankBoothsFor(visitor, Math.Clamp(limit, 1, MaxResults));
        await Save(eventId, results, ct);

        return (true, results.Select(r =>
        {
            var c = loaded.Companies[r.CompanyId];
            return new BoothMatch(c.Id, c.Name, c.Segment, c.BoothLocation, r.Score, r.Band, r.Reasons);
        }).ToList());
    }

    /// <summary>
    /// Leads de um estande: só visitantes que consentiram matching E compartilhamento com expositores.
    /// Contato só aparece se o visitante também o liberou na privacidade do perfil.
    /// </summary>
    public async Task<List<LeadMatch>> LeadsForAsync(Guid eventId, Guid companyId, int limit, CancellationToken ct)
    {
        var loaded = await Load(eventId, ct);
        var booth = loaded.Booths.FirstOrDefault(b => b.CompanyId == companyId);
        if (booth is null) return [];

        var profiles = await db.ParticipantEventProfiles.AsNoTracking().Include(x => x.Tags)
            .Include(x => x.User).ThenInclude(u => u.Profile).Include(x => x.User).ThenInclude(u => u.Privacy)
            .Where(x => x.EventId == eventId && x.ConsentMatchingAt != null && x.ConsentShareWithExhibitorsAt != null
                        && x.User.IsActive
                        && db.EventMemberships.Any(m => m.EventId == eventId && m.UserId == x.UserId
                            && m.Status == MembershipStatus.Active))
            .AsSplitQuery().ToListAsync(ct);

        var byUser = profiles.Where(p => VisitorProfileRules.IsReadyForMatching(p.Goal, p.Tags.Count, p.ConsentMatchingAt))
            .ToDictionary(p => p.UserId);
        var visitors = byUser.Values.Select(p => new MatchVisitor(p.UserId, p.Goal,
            p.Tags.Select(t => t.TagId).ToHashSet(), p.User.Profile?.JobTitle, p.User.Profile?.Area));

        var results = loaded.Engine.RankVisitorsFor(booth, visitors, Math.Clamp(limit, 1, MaxResults));
        await Save(eventId, results, ct);

        return results.Select(r =>
        {
            var u = byUser[r.UserId].User;
            var (email, phone) = Mapping.AuthorizedContacts(u);
            return new LeadMatch(u.Id, u.DisplayName, u.Profile?.CompanyName, u.Profile?.JobTitle,
                byUser[r.UserId].Goal, r.Score, r.Band, r.Reasons, email, phone);
        }).ToList();
    }

    private async Task<MatchVisitor> ToVisitor(ParticipantEventProfile p, CancellationToken ct)
    {
        var prof = await db.UserProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == p.UserId, ct);
        return new MatchVisitor(p.UserId, p.Goal, p.Tags.Select(t => t.TagId).ToHashSet(), prof?.JobTitle, prof?.Area);
    }

    private async Task<Loaded> Load(Guid eventId, CancellationToken ct)
    {
        var tags = await db.InterestTags.AsNoTracking()
            .Where(t => t.EventId == eventId && t.Status == TagStatus.Approved).ToListAsync(ct);
        var info = tags.ToDictionary(t => t.Id, t => new TagInfo(t.Name, t.ParentId));
        var companies = await db.Companies.AsNoTracking().Include(c => c.Tags)
            .Where(c => c.EventId == eventId).ToListAsync(ct);

        var booths = companies.Select(c =>
        {
            var own = c.Tags.Where(t => info.ContainsKey(t.TagId)).Select(t => t.TagId).ToHashSet();
            // o texto do estande inclui nomes e sinônimos dos temas, para o objetivo livre casar com eles
            var themeText = tags.Where(t => own.Contains(t.Id)).SelectMany(t => t.Synonyms.Prepend(t.Name));
            var text = string.Join(". ", new[] { c.Name, c.Segment, c.Description, c.ProductsServices }
                .Concat(themeText).Where(s => !string.IsNullOrWhiteSpace(s)));
            return new MatchBooth(c.Id, c.Name, text, c.IdealCustomer, own);
        }).ToList();

        return new Loaded(new MatchingEngine(booths, info), booths, companies.ToDictionary(c => c.Id));
    }

    private async Task Save(Guid eventId, IReadOnlyList<MatchResult> results, CancellationToken ct)
    {
        if (results.Count == 0) return;
        var now = DateTimeOffset.UtcNow;
        var uids = results.Select(r => r.UserId).Distinct().ToList();
        var cids = results.Select(r => r.CompanyId).Distinct().ToList();
        var existing = await db.MatchScores.Where(m => m.EventId == eventId && uids.Contains(m.UserId) && cids.Contains(m.CompanyId))
            .ToDictionaryAsync(m => (m.UserId, m.CompanyId), ct);
        foreach (var r in results)
        {
            if (!existing.TryGetValue((r.UserId, r.CompanyId), out var row))
            {
                row = new MatchScore { EventId = eventId, UserId = r.UserId, CompanyId = r.CompanyId };
                db.MatchScores.Add(row);
            }
            row.Score = r.Score; row.ThemeScore = r.Theme; row.TextScore = r.Text; row.AudienceScore = r.Audience;
            row.Band = r.Band.ToString(); row.Reasons = r.Reasons.ToList(); row.ComputedAt = now;
        }
        await db.SaveChangesAsync(ct);
    }
}
