using System.Text;
using Conexa.Api.Services.Insights;

namespace Conexa.Api.Controllers;

/// <summary>Painéis agregados, relatório por estande e exportações CSV (sem dados pessoais, exceto leads com consentimento).</summary>
[Route("api")]
public class InsightsController(ApplicationDbContext db, InsightsService insights, MatchingService matching,
    IEventAccessService access) : ApiControllerBase
{
    [HttpGet("organizer/events/{eventId:guid}/insights"), Authorize(Policy = Policies.EventOrganizer)]
    public async Task<ActionResult<EventInsights>> Event(Guid eventId, CancellationToken ct) =>
        await insights.EventAsync(eventId, ct);

    [HttpGet("organizer/events/{eventId:guid}/export/booths.csv"), Authorize(Policy = Policies.EventOrganizer)]
    public async Task<IActionResult> ExportBooths(Guid eventId, CancellationToken ct)
    {
        var rows = (await insights.BoothsAsync(eventId, ct)).Select(b => (IEnumerable<object?>)new object?[]
            { b.Name, b.BoothLocation, b.CheckIns, b.CheckedInVisitors, b.RecommendedTo, b.HighAffinity,
              b.FeedbackUseful, b.FeedbackNotUseful, b.LeadsAvailable });
        return Csv("estandes.csv", ["Estande", "Localização", "Check-ins", "Visitantes com check-in", "Recomendado a (visitantes)",
            "Afinidade alta", "Útil (sim)", "Útil (não)", "Leads disponíveis"], rows);
    }

    [HttpGet("organizer/events/{eventId:guid}/export/themes.csv"), Authorize(Policy = Policies.EventOrganizer)]
    public async Task<IActionResult> ExportThemes(Guid eventId, CancellationToken ct)
    {
        var data = await insights.EventAsync(eventId, ct);
        return Csv("temas.csv", ["Tema", "Macro-área", "Demanda (visitantes)", "Oferta (estandes)", "Lacuna"],
            data.Themes.Select(t => (IEnumerable<object?>)new object?[] { t.Name, t.Area, t.Demand, t.Supply, t.Demand > 0 && t.Supply == 0 }));
    }

    /// <summary>Métricas do estande. Organizador ou representante da empresa.</summary>
    [HttpGet("events/{eventId:guid}/companies/{companyId:guid}/report"), Authorize(Policy = Policies.EventMember)]
    public async Task<ActionResult<BoothMetrics>> BoothReport(Guid eventId, Guid companyId, CancellationToken ct)
    {
        if (!await CanManage(eventId, companyId, ct)) return Forbid();
        var m = await insights.BoothAsync(eventId, companyId, ct);
        return m is null ? NotFound() : m;
    }

    /// <summary>Leads do estande em CSV. Mesmas regras de consentimento da tela de leads.</summary>
    [HttpGet("events/{eventId:guid}/companies/{companyId:guid}/leads.csv"), Authorize(Policy = Policies.EventMember)]
    public async Task<IActionResult> ExportLeads(Guid eventId, Guid companyId, CancellationToken ct)
    {
        if (!await CanManage(eventId, companyId, ct)) return Forbid();
        var leads = await matching.LeadsForAsync(eventId, companyId, MatchingService.MaxResults, ct);
        return Csv("leads.csv", ["Nome", "Empresa", "Cargo", "Objetivo", "Afinidade", "Motivos", "E-mail", "Telefone"],
            leads.Select(l => (IEnumerable<object?>)new object?[]
                { l.DisplayName, l.CompanyName, l.JobTitle, l.Goal, l.Band.ToString(), string.Join(" ", l.Reasons), l.Email, l.Phone }));
    }

    private async Task<bool> CanManage(Guid eventId, Guid companyId, CancellationToken ct) =>
        await db.Companies.AnyAsync(c => c.Id == companyId && c.EventId == eventId, ct)
        && (await access.IsOrganizerAsync(eventId, User, ct)
            || await db.CompanyMemberships.AnyAsync(m => m.CompanyId == companyId && m.UserId == CurrentUserId, ct));

    private FileContentResult Csv(string name, IEnumerable<string> headers, IEnumerable<IEnumerable<object?>> rows) =>
        File(Encoding.UTF8.GetBytes(CsvWriter.Write(headers, rows)), "text/csv; charset=utf-8", name);
}
