namespace Conexa.Api.Services.Insights;

public record InsightTotals(int VisitorsWithProfile, int ConsentedMatching, int ConsentedSharing, int Booths, int BoothsWithThemes,
    int CheckIns, int CheckedInVisitors, int FeedbackUseful, int FeedbackNotUseful, double? UsefulRate);
public record ThemeInsight(Guid TagId, string Name, string? Area, int Demand, int Supply);
public record ExposureInsight(int BoothsRecommended, int BoothsNeverRecommended, double Gini, double Top20Share,
    string? MostRecommended, int MostRecommendedCount);
public record ProfileGroup(string Area, int Visitors, List<string> TopThemes);
public record TermCount(string Term, int Count);
public record EventInsights(InsightTotals Totals, List<ThemeInsight> Themes, List<ThemeInsight> Gaps,
    ExposureInsight Exposure, List<ProfileGroup> ProfileGroups, List<TermCount> TopTerms, List<string> Notes);

public record BoothMetrics(Guid CompanyId, string Name, string? BoothLocation, int CheckIns, int CheckedInVisitors,
    int RecommendedTo, int HighAffinity, int FeedbackUseful, int FeedbackNotUseful, int LeadsAvailable);

/// <summary>
/// Painéis do evento. Só agrega: nenhuma lista de pessoas sai daqui (leads individuais seguem as regras de
/// consentimento em <see cref="MatchingService"/>). Considera apenas visitantes que consentiram o matching.
/// </summary>
public class InsightsService(ApplicationDbContext db)
{
    public const int MinVisitorsForConfidence = 10;

    public async Task<EventInsights> EventAsync(Guid eventId, CancellationToken ct)
    {
        var profiles = await db.ParticipantEventProfiles.AsNoTracking().Include(p => p.Tags)
            .Include(p => p.User).ThenInclude(u => u.Profile)
            .Where(p => p.EventId == eventId && p.User.IsActive).AsSplitQuery().ToListAsync(ct);
        var consented = profiles.Where(p => p.ConsentMatchingAt != null).ToList();
        var tags = await db.InterestTags.AsNoTracking().Where(t => t.EventId == eventId && t.Status == TagStatus.Approved).ToListAsync(ct);
        var companies = await db.Companies.AsNoTracking().Include(c => c.Tags).Where(c => c.EventId == eventId).ToListAsync(ct);
        var inter = await db.Interactions.AsNoTracking().Where(i => i.EventId == eventId).ToListAsync(ct);
        var scores = await db.MatchScores.AsNoTracking().Where(m => m.EventId == eventId && m.Band != "None")
            .Select(m => new { m.CompanyId, m.UserId }).ToListAsync(ct);

        var checkIns = inter.Where(i => i.Type == InteractionType.CheckIn).ToList();
        var useful = inter.Count(i => i.Type == InteractionType.FeedbackUseful);
        var notUseful = inter.Count(i => i.Type == InteractionType.FeedbackNotUseful);
        var totals = new InsightTotals(profiles.Count, consented.Count,
            consented.Count(p => p.ConsentShareWithExhibitorsAt != null), companies.Count,
            companies.Count(c => c.Tags.Count > 0), checkIns.Count, checkIns.Select(i => i.UserId).Distinct().Count(),
            useful, notUseful, useful + notUseful == 0 ? null : Math.Round(useful / (double)(useful + notUseful), 4));

        // Demanda (visitantes que escolheram ou mencionaram o tema) x oferta (estandes com o tema)
        var area = tags.Where(t => t.ParentId == null).ToDictionary(t => t.Id, t => t.Name);
        var themes = tags.Where(t => t.ParentId != null || !tags.Any(x => x.ParentId == t.Id)).Select(t =>
        {
            var demand = consented.Count(p => p.Tags.Any(x => x.TagId == t.Id) || InsightMath.Mentions(p.Goal, t.Name, t.Synonyms));
            var supply = companies.Count(c => c.Tags.Any(x => x.TagId == t.Id));
            return new ThemeInsight(t.Id, t.Name, t.ParentId is { } pid ? area.GetValueOrDefault(pid) : null, demand, supply);
        }).OrderByDescending(t => t.Demand).ThenBy(t => t.Name).ToList();
        var gaps = themes.Where(t => t.Demand > 0 && t.Supply == 0).ToList();

        // Concentração de exposição entre estandes
        var counts = companies.Select(c => scores.Count(s => s.CompanyId == c.Id)).ToList();
        var top = companies.Select((c, i) => (c.Name, n: counts[i])).OrderByDescending(x => x.n).FirstOrDefault();
        var exposure = new ExposureInsight(counts.Count(n => n > 0), counts.Count(n => n == 0),
            InsightMath.Gini(counts), InsightMath.TopShare(counts), top.n > 0 ? top.Name : null, top.n);

        // Perfis em comum: visitantes agrupados pela área de atuação, com os temas mais pedidos em cada grupo
        var tagName = tags.ToDictionary(t => t.Id, t => t.Name);
        var groups = consented.GroupBy(p => Mapping.Clean(p.User.Profile?.Area) ?? "Não informado")
            .Select(g => new ProfileGroup(g.Key, g.Count(),
                g.SelectMany(p => p.Tags.Select(x => x.TagId)).GroupBy(x => x).OrderByDescending(x => x.Count())
                    .Take(3).Select(x => tagName.GetValueOrDefault(x.Key, "tema")).ToList()))
            .OrderByDescending(g => g.Visitors).ThenBy(g => g.Area).ToList();

        var terms = InsightMath.TopTerms(consented.Select(p => p.Goal)).Select(t => new TermCount(t.Term, t.Count)).ToList();

        var notes = new List<string>();
        if (consented.Count < MinVisitorsForConfidence)
            notes.Add($"Poucos visitantes com perfil ({consented.Count}): os números abaixo são indicativos, não conclusivos.");
        if (scores.Count == 0)
            notes.Add("Ainda não há recomendações calculadas: a concentração de exposição aparece depois que os visitantes salvarem o perfil.");
        if (checkIns.Count == 0)
            notes.Add("Nenhum check-in por QR registrado ainda.");

        return new EventInsights(totals, themes, gaps, exposure, groups, terms, notes);
    }

    public async Task<List<BoothMetrics>> BoothsAsync(Guid eventId, CancellationToken ct)
    {
        var companies = await db.Companies.AsNoTracking().Where(c => c.EventId == eventId).OrderBy(c => c.Name).ToListAsync(ct);
        var inter = await db.Interactions.AsNoTracking().Where(i => i.EventId == eventId)
            .Select(i => new { i.CompanyId, i.UserId, i.Type }).ToListAsync(ct);
        var scores = await db.MatchScores.AsNoTracking().Where(m => m.EventId == eventId && m.Band != "None")
            .Select(m => new { m.CompanyId, m.UserId, m.Band }).ToListAsync(ct);
        var sharing = (await db.ParticipantEventProfiles.AsNoTracking().Where(p => p.EventId == eventId
            && p.ConsentMatchingAt != null && p.ConsentShareWithExhibitorsAt != null && p.User.IsActive)
            .Select(p => p.UserId).ToListAsync(ct)).ToHashSet();

        return companies.Select(c =>
        {
            var ci = inter.Where(i => i.CompanyId == c.Id && i.Type == InteractionType.CheckIn).ToList();
            var sc = scores.Where(s => s.CompanyId == c.Id).ToList();
            return new BoothMetrics(c.Id, c.Name, c.BoothLocation, ci.Count, ci.Select(i => i.UserId).Distinct().Count(),
                sc.Count, sc.Count(s => s.Band == "High"),
                inter.Count(i => i.CompanyId == c.Id && i.Type == InteractionType.FeedbackUseful),
                inter.Count(i => i.CompanyId == c.Id && i.Type == InteractionType.FeedbackNotUseful),
                sc.Count(s => sharing.Contains(s.UserId)));
        }).ToList();
    }

    public async Task<BoothMetrics?> BoothAsync(Guid eventId, Guid companyId, CancellationToken ct) =>
        (await BoothsAsync(eventId, ct)).FirstOrDefault(b => b.CompanyId == companyId);
}
