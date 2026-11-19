namespace Conexa.Api.Services.Matching;

/// <summary>
/// Gera avisos no app depois que o visitante salva o perfil. Leads só são anunciados a expositores
/// quando o visitante consentiu o compartilhamento; ao revogar, os avisos com o nome dele são apagados.
/// E-mail e WhatsApp ainda não são enviados (dependem de provedor e de consentimento próprio).
/// </summary>
public class MatchNotifier(ApplicationDbContext db, MatchingService matching)
{
    public static string LeadKey(Guid eventId, Guid companyId, Guid userId) => $"lead:{eventId}:{companyId}:{userId}";

    public async Task AfterProfileSavedAsync(Guid eventId, Guid userId, CancellationToken ct)
    {
        var profile = await db.ParticipantEventProfiles.AsNoTracking()
            .Include(p => p.User).FirstOrDefaultAsync(p => p.EventId == eventId && p.UserId == userId, ct);
        if (profile is null) return;

        var leadPrefix = $"lead:{eventId}:";
        var leadSuffix = $":{userId}";
        if (profile.ConsentShareWithExhibitorsAt is null)
        {
            var stale = await db.Notifications.Where(n => n.DedupeKey.StartsWith(leadPrefix) && n.DedupeKey.EndsWith(leadSuffix)).ToListAsync(ct);
            db.Notifications.RemoveRange(stale);
            await db.SaveChangesAsync(ct);
        }

        var (ready, items) = await matching.BoothsForAsync(eventId, userId, MatchingService.MaxResults, ct);
        if (!ready || items.Count == 0) return;

        var pending = new List<Notification>();
        var recipients = new HashSet<Guid> { userId };
        var leadTargets = new List<(BoothMatch Match, Guid RepId)>();
        if (profile.ConsentShareWithExhibitorsAt is not null)
        {
            var highs = items.Where(i => i.Band == MatchBand.High).Select(i => i.CompanyId).ToList();
            var reps = await db.CompanyMemberships.AsNoTracking()
                .Where(m => highs.Contains(m.CompanyId) && m.User.IsActive).Select(m => new { m.CompanyId, m.UserId }).ToListAsync(ct);
            foreach (var i in items.Where(i => i.Band == MatchBand.High))
                foreach (var r in reps.Where(r => r.CompanyId == i.CompanyId)) { leadTargets.Add((i, r.UserId)); recipients.Add(r.UserId); }
        }

        var existing = (await db.Notifications.AsNoTracking().Where(n => recipients.Contains(n.UserId) && n.EventId == eventId)
            .Select(n => new { n.UserId, n.DedupeKey }).ToListAsync(ct)).Select(n => (n.UserId, n.DedupeKey)).ToHashSet();

        void Add(Guid to, string key, string title, string body, string link)
        {
            if (!existing.Add((to, key))) return;
            pending.Add(new Notification { UserId = to, EventId = eventId, DedupeKey = key, Title = title, Body = body, Link = link });
        }

        Add(userId, $"matches:{eventId}", "Estandes para você",
            $"Encontramos {items.Count} estande(s) com afinidade com o que você procura.", "/evento");
        var goal = profile.Goal is { Length: > 140 } g ? g[..140] + "…" : profile.Goal;
        foreach (var (m, rep) in leadTargets)
            Add(rep, LeadKey(eventId, m.CompanyId, userId), "Novo lead com afinidade alta",
                $"{profile.User.DisplayName} procura: “{goal}”", "/evento");

        if (pending.Count == 0) return;
        db.Notifications.AddRange(pending);
        await db.SaveChangesAsync(ct);
    }
}
