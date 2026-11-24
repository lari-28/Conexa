namespace Conexa.Api.Controllers;

/// <summary>Perfil do próprio usuário dentro de um evento: objetivo, temas e consentimentos por finalidade.</summary>
[Route("api/events/{eventId:guid}/my-profile"), Authorize(Policy = Policies.EventMember)]
public class VisitorProfileController(ApplicationDbContext db, MatchNotifier notifier, ILogger<VisitorProfileController> log) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<VisitorProfileResponse>> Get(Guid eventId, CancellationToken ct)
    {
        var p = await Load(eventId, CurrentUserId, asNoTracking: true, ct);
        return p is null ? Empty(eventId) : ToResponse(p);
    }

    [HttpPut]
    public async Task<ActionResult<VisitorProfileResponse>> Put(Guid eventId, UpdateVisitorProfileRequest req, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var uid = CurrentUserId;

        var ids = (req.TagIds ?? []).Distinct().ToList();
        var found = await db.InterestTags.Where(t => ids.Contains(t.Id)).ToListAsync(ct);
        var tagError = VisitorProfileRules.ValidateTags(ids, found, eventId);
        if (tagError is not null) return ValidationFailure(nameof(req.TagIds), tagError);

        var p = await Load(eventId, uid, asNoTracking: false, ct);
        var current = p is null
            ? new ConsentState(null, null, null)
            : new ConsentState(p.ConsentMatchingAt, p.ConsentShareWithExhibitorsAt, p.ConsentExternalAiAt);
        var (state, consentError) = VisitorProfileRules.ApplyConsents(
            current, req.ConsentMatching, req.ConsentShareWithExhibitors, req.ConsentExternalAi, now);
        if (consentError is not null) return ValidationFailure(nameof(req.ConsentMatching), consentError);

        if (p is null)
        {
            p = new ParticipantEventProfile { EventId = eventId, UserId = uid };
            db.ParticipantEventProfiles.Add(p);
        }
        p.Goal = VisitorProfileRules.CleanGoal(req.Goal);
        p.Timeframe = req.Timeframe;
        p.ConsentMatchingAt = state!.Matching;
        p.ConsentShareWithExhibitorsAt = state.ShareWithExhibitors;
        p.ConsentExternalAiAt = state.ExternalAi;
        p.UpdatedAt = now;

        // Diferença de temas (a chave composta impede apagar e recriar o mesmo vínculo).
        foreach (var old in p.Tags.Where(x => !ids.Contains(x.TagId)).ToList()) p.Tags.Remove(old);
        foreach (var t in found.Where(t => p.Tags.All(x => x.TagId != t.Id)))
            p.Tags.Add(new ParticipantTag { EventId = eventId, UserId = uid, TagId = t.Id, Tag = t });

        await db.SaveChangesAsync(ct);

        // Avisos não podem derrubar o salvamento do perfil.
        try { await notifier.AfterProfileSavedAsync(eventId, uid, ct); }
        catch (Exception ex) when (ex is not OperationCanceledException)
        { log.LogWarning(ex, "Falha ao gerar avisos de matching para o evento {EventId}", eventId); }
        return ToResponse(p);
    }

    private Task<ParticipantEventProfile?> Load(Guid eventId, Guid userId, bool asNoTracking, CancellationToken ct)
    {
        var q = db.ParticipantEventProfiles.Include(x => x.Tags).ThenInclude(x => x.Tag).AsQueryable();
        if (asNoTracking) q = q.AsNoTracking();
        return q.FirstOrDefaultAsync(x => x.EventId == eventId && x.UserId == userId, ct);
    }

    private static VisitorProfileResponse Empty(Guid eventId) =>
        new(eventId, null, null, [], new ConsentsResponse(false, null, false, null, false, null), false, null);

    private static VisitorProfileResponse ToResponse(ParticipantEventProfile p) =>
        new(p.EventId, p.Goal, p.Timeframe,
            p.Tags.Select(x => TaxonomyRules.ToResponse(x.Tag)).OrderBy(t => t.Name).ToList(),
            new ConsentsResponse(p.ConsentMatchingAt is not null, p.ConsentMatchingAt,
                p.ConsentShareWithExhibitorsAt is not null, p.ConsentShareWithExhibitorsAt,
                p.ConsentExternalAiAt is not null, p.ConsentExternalAiAt),
            VisitorProfileRules.IsReadyForMatching(p.Goal, p.Tags.Count, p.ConsentMatchingAt), p.UpdatedAt);
}
