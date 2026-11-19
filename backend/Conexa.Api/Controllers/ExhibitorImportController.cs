namespace Conexa.Api.Controllers;

[Route("api/organizer/events/{eventId:guid}/exhibitors"), Authorize(Policy = Policies.EventOrganizer)]
public class ExhibitorImportController(ExhibitorImportService import) : ApiControllerBase
{
    /// <summary>Valida (Commit=false) ou importa (Commit=true) o catálogo de expositores em CSV.</summary>
    [HttpPost("import")]
    public async Task<ActionResult<ImportReport>> Import(Guid eventId, ImportExhibitorsRequest req, CancellationToken ct) =>
        await import.RunAsync(eventId, req.Csv, req.Commit, CurrentUserId, ct);
}
