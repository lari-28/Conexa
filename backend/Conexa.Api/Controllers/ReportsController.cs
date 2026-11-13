namespace Conexa.Api.Controllers;

[Route("api/reports")]
public class ReportsController(ApplicationDbContext db, IEventAccessService access) : ApiControllerBase
{
    /// <summary>Denúncia de publicação, comentário ou perfil; encaminhada à moderação do evento (RF20, RN08).</summary>
    [HttpPost]
    public async Task<IActionResult> Create(CreateReportRequest req, CancellationToken ct)
    {
        if (!await access.IsMemberAsync(req.EventId, User, ct)) return NotFound();

        var exists = req.TargetType switch
        {
            ReportTargetType.Post => await db.Posts.AnyAsync(p => p.Id == req.TargetId && p.EventId == req.EventId, ct),
            ReportTargetType.Comment => await db.Comments.AnyAsync(c => c.Id == req.TargetId && c.Post.EventId == req.EventId, ct),
            ReportTargetType.Profile => await db.EventMemberships.AnyAsync(m => m.UserId == req.TargetId && m.EventId == req.EventId, ct),
            _ => false
        };
        if (!exists) return NotFound();

        var uid = CurrentUserId;
        if (await db.Reports.AnyAsync(r => r.ReporterId == uid && r.TargetType == req.TargetType
                && r.TargetId == req.TargetId && r.Status == ReportStatus.Open, ct))
            return Problem(statusCode: 409, title: "Você já denunciou este item.");

        var report = new Report
        {
            EventId = req.EventId, ReporterId = uid, TargetType = req.TargetType,
            TargetId = req.TargetId, Reason = req.Reason.Trim()
        };
        db.Reports.Add(report);
        await db.SaveChangesAsync(ct);
        return StatusCode(StatusCodes.Status201Created, new { report.Id });
    }
}
