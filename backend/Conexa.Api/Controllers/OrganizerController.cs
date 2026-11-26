namespace Conexa.Api.Controllers;

[Route("api/organizer")]
public class OrganizerController(ApplicationDbContext db, IEventAccessService access,
    UserManager<ApplicationUser> users) : ApiControllerBase
{
    private static readonly EventStatus[] OrganizerStatuses = [EventStatus.Draft, EventStatus.Active, EventStatus.Finished];

    [HttpPost("events/{eventId:guid}/activities"), Authorize(Policy = Policies.EventOrganizer)]
    public async Task<ActionResult<EventActivityResponse>> CreateActivity(Guid eventId, EventActivityRequest req, CancellationToken ct)
    {
        if (req.EndsAt <= req.StartsAt) return ValidationFailure(nameof(req.EndsAt), "O término deve ser posterior ao início.");
        if (!Enum.IsDefined(req.Kind)) return ValidationFailure(nameof(req.Kind), "Escolha palestra ou atividade.");
        var ev = await db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == eventId, ct);
        if (ev is null) return NotFound();
        if (req.StartsAt < ev.StartsAt || req.EndsAt > ev.EndsAt)
            return ValidationFailure(nameof(req.StartsAt), "O horário deve ficar dentro do período do evento.");
        var personIds = (req.PersonIds ?? []).Distinct().ToArray();
        var validPeople = await db.EventMemberships.Where(m => m.EventId == eventId && m.Status == MembershipStatus.Active
                && personIds.Contains(m.UserId))
            .Select(m => m.UserId).ToListAsync(ct);
        if (validPeople.Count != personIds.Length)
            return ValidationFailure(nameof(req.PersonIds), "Vincule apenas pessoas ativas neste evento.");

        var activity = new EventActivity
        {
            EventId = eventId, Kind = req.Kind, Title = req.Title.Trim(), Description = Mapping.Clean(req.Description),
            Location = req.Location.Trim(), StartsAt = req.StartsAt.ToUniversalTime(), EndsAt = req.EndsAt.ToUniversalTime(),
            People = personIds.Select(id => new EventActivityPerson { UserId = id }).ToList(),
        };
        db.EventActivities.Add(activity);
        db.AuditLogs.Add(Audit(eventId, "event.activity.create", nameof(EventActivity), activity.Id, activity.Title));
        await db.SaveChangesAsync(ct);
        return Created($"/api/events/{eventId}/activities", new EventActivityResponse(activity.Id, activity.Kind,
            activity.Title, activity.Description, activity.Location, activity.StartsAt, activity.EndsAt, []));
    }

    [HttpPut("events/{eventId:guid}/activities/{activityId:guid}"), Authorize(Policy = Policies.EventOrganizer)]
    public async Task<ActionResult<EventActivityResponse>> UpdateActivity(Guid eventId, Guid activityId,
        EventActivityRequest req, CancellationToken ct)
    {
        if (req.EndsAt <= req.StartsAt) return ValidationFailure(nameof(req.EndsAt), "O término deve ser posterior ao início.");
        if (!Enum.IsDefined(req.Kind)) return ValidationFailure(nameof(req.Kind), "Escolha palestra ou atividade.");
        var activity = await db.EventActivities.Include(a => a.People)
            .FirstOrDefaultAsync(a => a.Id == activityId && a.EventId == eventId, ct);
        if (activity is null) return NotFound();
        var ev = await db.Events.AsNoTracking().Where(e => e.Id == eventId)
            .Select(e => new { e.StartsAt, e.EndsAt }).FirstOrDefaultAsync(ct);
        if (ev is null) return NotFound();
        if (req.StartsAt < ev.StartsAt || req.EndsAt > ev.EndsAt)
            return ValidationFailure(nameof(req.StartsAt), "O horário deve ficar dentro do período do evento.");
        var personIds = (req.PersonIds ?? []).Distinct().ToArray();
        var validPeople = await db.EventMemberships.Where(m => m.EventId == eventId && m.Status == MembershipStatus.Active
                && personIds.Contains(m.UserId))
            .Select(m => m.UserId).ToListAsync(ct);
        if (validPeople.Count != personIds.Length)
            return ValidationFailure(nameof(req.PersonIds), "Vincule apenas pessoas ativas neste evento.");

        activity.Kind = req.Kind;
        activity.Title = req.Title.Trim();
        activity.Description = Mapping.Clean(req.Description);
        activity.Location = req.Location.Trim();
        activity.StartsAt = req.StartsAt.ToUniversalTime();
        activity.EndsAt = req.EndsAt.ToUniversalTime();
        var existingPeople = activity.People.ToDictionary(person => person.UserId);
        foreach (var person in activity.People.Where(person => !personIds.Contains(person.UserId)).ToList())
        {
            db.EventActivityPeople.Remove(person);
            activity.People.Remove(person);
        }
        foreach (var userId in personIds.Where(id => !existingPeople.ContainsKey(id)))
            activity.People.Add(new EventActivityPerson { ActivityId = activityId, UserId = userId });
        db.AuditLogs.Add(Audit(eventId, "event.activity.update", nameof(EventActivity), activity.Id, activity.Title));
        await db.SaveChangesAsync(ct);
        return new EventActivityResponse(activity.Id, activity.Kind, activity.Title, activity.Description,
            activity.Location, activity.StartsAt, activity.EndsAt, []);
    }

    [HttpDelete("events/{eventId:guid}/activities/{activityId:guid}"), Authorize(Policy = Policies.EventOrganizer)]
    public async Task<IActionResult> DeleteActivity(Guid eventId, Guid activityId, CancellationToken ct)
    {
        var activity = await db.EventActivities.FirstOrDefaultAsync(a => a.Id == activityId && a.EventId == eventId, ct);
        if (activity is null) return NotFound();
        db.EventActivities.Remove(activity);
        db.AuditLogs.Add(Audit(eventId, "event.activity.delete", nameof(EventActivity), activity.Id, activity.Title));
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("events/{eventId:guid}/maps"), Authorize(Policy = Policies.EventOrganizer), Consumes("multipart/form-data")]
    [RequestSizeLimit(15_000_000)]
    public async Task<ActionResult<EventMapResponse>> AddMap(Guid eventId, CancellationToken ct)
    {
        var form = await Request.ReadFormAsync(ct);
        var file = form.Files.GetFile("file");
        var description = form["description"].ToString().Trim();
        if (file is null || file.Length == 0) return ValidationFailure("file", "Selecione uma imagem PNG.");
        if (file.Length > 14_000_000) return ValidationFailure("file", "A imagem deve ter até 14 MB.");
        if (string.IsNullOrWhiteSpace(description) || description.Length > 120)
            return ValidationFailure("description", "Informe uma descrição de até 120 caracteres.");
        if (!string.Equals(Path.GetExtension(file.FileName), ".png", StringComparison.OrdinalIgnoreCase))
            return ValidationFailure("file", "Envie um arquivo PNG.");

        await using var input = file.OpenReadStream();
        using var buffer = new MemoryStream();
        await input.CopyToAsync(buffer, ct);
        var image = buffer.ToArray();
        if (image.Length < 8 || !image.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
            return ValidationFailure("file", "O arquivo selecionado não é um PNG válido.");

        var map = new EventMap { EventId = eventId, Description = description,
            FileName = Path.GetFileName(file.FileName), Image = image };
        db.EventMaps.Add(map);
        db.AuditLogs.Add(Audit(eventId, "event.map.add", nameof(EventMap), map.Id, description));
        await db.SaveChangesAsync(ct);
        return Created($"/api/events/{eventId}/maps", new EventMapResponse(map.Id, map.Description,
            map.FileName, $"data:image/png;base64,{Convert.ToBase64String(map.Image)}", map.CreatedAt));
    }

    [HttpDelete("events/{eventId:guid}/maps/{mapId:guid}"), Authorize(Policy = Policies.EventOrganizer)]
    public async Task<IActionResult> DeleteMap(Guid eventId, Guid mapId, CancellationToken ct)
    {
        var map = await db.EventMaps.FirstOrDefaultAsync(m => m.Id == mapId && m.EventId == eventId, ct);
        if (map is null) return NotFound();
        db.EventMaps.Remove(map);
        db.AuditLogs.Add(Audit(eventId, "event.map.delete", nameof(EventMap), map.Id, map.Description));
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ---------- Evento (RF17) ----------

    [HttpPut("events/{eventId:guid}"), Authorize(Policy = Policies.EventOrganizer)]
    public async Task<ActionResult<EventResponse>> UpdateEvent(Guid eventId, EventRequest req, CancellationToken ct)
    {
        if (req.EndsAt < req.StartsAt)
            return ValidationFailure(nameof(req.EndsAt), "O término deve ser posterior ao início.");
        var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == eventId, ct);
        if (ev is null) return NotFound();

        ev.Name = req.Name.Trim();
        ev.Description = Mapping.Clean(req.Description);
        ev.StartsAt = req.StartsAt.ToUniversalTime();
        ev.EndsAt = req.EndsAt.ToUniversalTime();
        ev.Location = Mapping.Clean(req.Location);
        if (req.Status is { } st && (IsPlatformAdmin || OrganizerStatuses.Contains(st))) ev.Status = st;

        db.AuditLogs.Add(Audit(eventId, "event.update", nameof(Event), eventId));
        await db.SaveChangesAsync(ct);
        return Mapping.ToEvent(ev);
    }

    // ---------- Membros (RF18) ----------

    [HttpGet("events/{eventId:guid}/members"), Authorize(Policy = Policies.EventOrganizer)]
    public async Task<ActionResult<PagedResponse<MemberResponse>>> Members(
        Guid eventId, string? q, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        (page, pageSize) = Mapping.Clamp(page, pageSize, 100);
        var query = db.EventMemberships.AsNoTracking().Where(m => m.EventId == eventId);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var like = $"%{Mapping.EscapeLike(q.Trim())}%";
            query = query.Where(m => EF.Functions.ILike(m.User.DisplayName, like) || EF.Functions.ILike(m.User.Email!, like));
        }
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(m => m.User.DisplayName).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(m => new MemberResponse(m.UserId, m.User.DisplayName, m.User.Email ?? "", m.RoleInEvent, m.Status, m.JoinedAt))
            .ToListAsync(ct);
        return new PagedResponse<MemberResponse>(items, page, pageSize, total);
    }

    /// <summary>Vincula um usuário já cadastrado ao evento (pelo e-mail).</summary>
    [HttpPost("events/{eventId:guid}/members"), Authorize(Policy = Policies.EventOrganizer)]
    public async Task<IActionResult> AddMember(Guid eventId, AddMemberRequest req, CancellationToken ct)
    {
        if (req.RoleInEvent == EventRole.Organizer && !IsPlatformAdmin)
            return Problem(statusCode: 403, title: "Somente o administrador designa organizadores.");
        var user = await users.FindByEmailAsync(req.Email.Trim());
        if (user is null) return Problem(statusCode: 404, title: "Usuário não encontrado. Peça que ele se cadastre primeiro.");

        var m = await db.EventMemberships.FindAsync(new object[] { eventId, user.Id }, ct);
        if (m is null)
            db.EventMemberships.Add(new EventMembership { EventId = eventId, UserId = user.Id, RoleInEvent = req.RoleInEvent });
        else
        {
            if (m.RoleInEvent == EventRole.Organizer && !IsPlatformAdmin)
                return Problem(statusCode: 403, title: "Somente o administrador altera organizadores.");
            m.Status = MembershipStatus.Active;
            m.RoleInEvent = req.RoleInEvent;
        }
        db.AuditLogs.Add(Audit(eventId, "member.add", nameof(EventMembership), user.Id, req.RoleInEvent.ToString()));
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>Habilita, desabilita ou remove o acesso de um membro ao evento.</summary>
    [HttpPatch("events/{eventId:guid}/members/{userId:guid}"), Authorize(Policy = Policies.EventOrganizer)]
    public async Task<IActionResult> UpdateMember(Guid eventId, Guid userId, UpdateMemberRequest req, CancellationToken ct)
    {
        if (userId == CurrentUserId) return Problem(statusCode: 400, title: "Você não pode alterar o próprio vínculo.");
        var m = await db.EventMemberships.FindAsync(new object[] { eventId, userId }, ct);
        if (m is null) return NotFound();
        if ((m.RoleInEvent == EventRole.Organizer || req.RoleInEvent == EventRole.Organizer) && !IsPlatformAdmin)
            return Problem(statusCode: 403, title: "Somente o administrador altera organizadores.");

        if (req.Status is { } status) m.Status = status;
        if (req.RoleInEvent is { } role) m.RoleInEvent = role;
        db.AuditLogs.Add(Audit(eventId, "member.update", nameof(EventMembership), userId, $"{m.Status}/{m.RoleInEvent}"));
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ---------- Denúncias / moderação (RF20) ----------

    [HttpGet("events/{eventId:guid}/reports"), Authorize(Policy = Policies.EventOrganizer)]
    public async Task<ActionResult<PagedResponse<ReportResponse>>> Reports(
        Guid eventId, ReportStatus? status, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        (page, pageSize) = Mapping.Clamp(page, pageSize);
        var query = db.Reports.AsNoTracking().Where(r => r.EventId == eventId && (status == null || r.Status == status));
        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(r => r.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => new { r.Id, r.TargetType, r.TargetId, r.Reason, r.Status, r.CreatedAt, Reporter = r.Reporter.DisplayName })
            .ToListAsync(ct);

        var postIds = rows.Where(r => r.TargetType == ReportTargetType.Post).Select(r => r.TargetId).ToList();
        var commentIds = rows.Where(r => r.TargetType == ReportTargetType.Comment).Select(r => r.TargetId).ToList();
        var profileIds = rows.Where(r => r.TargetType == ReportTargetType.Profile).Select(r => r.TargetId).ToList();
        var posts = await db.Posts.Where(p => postIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Text, ct);
        var comments = await db.Comments.Where(c => commentIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Text, ct);
        var names = await db.Users.Where(u => profileIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.DisplayName, ct);

        string? Preview(ReportTargetType t, Guid id)
        {
            var text = t switch
            {
                ReportTargetType.Post => posts.GetValueOrDefault(id),
                ReportTargetType.Comment => comments.GetValueOrDefault(id),
                _ => names.GetValueOrDefault(id)
            };
            return text is { Length: > 160 } ? text[..160] + "…" : text;
        }

        var items = rows.Select(r => new ReportResponse(r.Id, r.TargetType, r.TargetId, Preview(r.TargetType, r.TargetId),
            r.Reason, r.Status, r.CreatedAt, r.Reporter)).ToList();
        return new PagedResponse<ReportResponse>(items, page, pageSize, total);
    }

    /// <summary>Analisa denúncia: descarta ou toma providência (ocultar conteúdo e/ou suspender acesso ao evento).</summary>
    [HttpPatch("reports/{reportId:guid}")]
    public async Task<IActionResult> Review(Guid reportId, ReviewReportRequest req, CancellationToken ct)
    {
        var report = await db.Reports.FindAsync(new object[] { reportId }, ct);
        if (report is null || !await access.IsOrganizerAsync(report.EventId, User, ct)) return NotFound();
        if (req.Status == ReportStatus.Open)
            return ValidationFailure(nameof(req.Status), "Informe Dismissed ou ActionTaken.");

        Post? post = null; Comment? comment = null; Guid? authorId = null;
        switch (report.TargetType)
        {
            case ReportTargetType.Post:
                post = await db.Posts.FindAsync(new object[] { report.TargetId }, ct);
                authorId = post?.AuthorId;
                break;
            case ReportTargetType.Comment:
                comment = await db.Comments.FindAsync(new object[] { report.TargetId }, ct);
                authorId = comment?.AuthorId;
                break;
            default:
                authorId = report.TargetId;
                break;
        }

        if (req.HideContent)
        {
            if (post is not null) post.Status = ContentStatus.Hidden;
            if (comment is not null) comment.Status = ContentStatus.Hidden;
        }
        if (req.DisableAuthorAccess && authorId is { } aid && aid != CurrentUserId)
        {
            var m = await db.EventMemberships.FindAsync(new object[] { report.EventId, aid }, ct);
            if (m is not null && (m.RoleInEvent != EventRole.Organizer || IsPlatformAdmin))
                m.Status = MembershipStatus.Disabled;
        }

        report.Status = req.Status;
        report.ReviewedById = CurrentUserId;
        report.ReviewedAt = DateTimeOffset.UtcNow;
        db.AuditLogs.Add(Audit(report.EventId, "report.review", nameof(Report), report.Id,
            $"{req.Status}; hide={req.HideContent}; disable={req.DisableAuthorAccess}"));
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
