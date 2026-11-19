using Conexa.Api.Services.Matching;

namespace Conexa.Api.Controllers;

public record BoothMatchesResponse(bool Ready, string? Message, IReadOnlyList<BoothMatch> Items);
public record LeadMatchesResponse(IReadOnlyList<LeadMatch> Items, int ConsentingVisitors);

/// <summary>Recomendações por afinidade. Explicadas por regras, sem LLM; consentimento obrigatório.</summary>
[Route("api/events/{eventId:guid}"), Authorize(Policy = Policies.EventMember)]
public class MatchingController(ApplicationDbContext db, MatchingService matching, IEventAccessService access) : ApiControllerBase
{
    /// <summary>Estandes com mais afinidade com o perfil do usuário atual.</summary>
    [HttpGet("matches/booths")]
    public async Task<ActionResult<BoothMatchesResponse>> Booths(Guid eventId, int limit = 10, CancellationToken ct = default)
    {
        var (ready, items) = await matching.BoothsForAsync(eventId, CurrentUserId, limit, ct);
        if (!ready)
            return new BoothMatchesResponse(false,
                "Conte o que você procura e autorize o uso do perfil para receber recomendações.", []);
        return new BoothMatchesResponse(true,
            items.Count == 0 ? "Nenhum estande com afinidade suficiente por enquanto. Tente descrever melhor o seu objetivo." : null,
            items);
    }

    /// <summary>Visitantes com mais afinidade com o estande. Organizador ou representante da empresa.</summary>
    [HttpGet("companies/{companyId:guid}/leads")]
    public async Task<ActionResult<LeadMatchesResponse>> Leads(Guid eventId, Guid companyId, int limit = 20, CancellationToken ct = default)
    {
        if (!await db.Companies.AnyAsync(c => c.Id == companyId && c.EventId == eventId, ct)) return NotFound();
        var allowed = await access.IsOrganizerAsync(eventId, User, ct)
            || await db.CompanyMemberships.AnyAsync(m => m.CompanyId == companyId && m.UserId == CurrentUserId, ct);
        if (!allowed) return Forbid();

        var items = await matching.LeadsForAsync(eventId, companyId, limit, ct);
        var consenting = await db.ParticipantEventProfiles.CountAsync(p => p.EventId == eventId
            && p.ConsentMatchingAt != null && p.ConsentShareWithExhibitorsAt != null, ct);
        return new LeadMatchesResponse(items, consenting);
    }
}
