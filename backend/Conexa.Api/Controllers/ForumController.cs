namespace Conexa.Api.Controllers;

public class ForumController(ApplicationDbContext db, IEventAccessService access) : ApiControllerBase
{
    private const int MaxPostsPerMinute = 3;      // proteção básica contra spam
    private const int MaxCommentsPerMinute = 6;

    // ---------- Posts ----------

    [HttpGet("api/events/{eventId:guid}/posts"), Authorize(Policy = Policies.EventMember)]
    public async Task<ActionResult<PagedResponse<PostResponse>>> List(
        Guid eventId, string? category, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        (page, pageSize) = Mapping.Clamp(page, pageSize);
        var uid = CurrentUserId;
        var isMod = await access.IsOrganizerAsync(eventId, User, ct);

        var query = db.Posts.AsNoTracking().Where(p => p.EventId == eventId && p.Status == ContentStatus.Visible);
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(p => p.Category == category);

        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new
            {
                p.Id, p.AuthorId, AuthorName = p.Author.DisplayName, Photo = p.Author.Profile!.PhotoUrl,
                p.Text, p.Category, p.CreatedAt, p.UpdatedAt,
                Comments = p.Comments.Count(c => c.Status == ContentStatus.Visible),
                Reactions = p.Reactions.Count,
                Mine = p.Reactions.Where(r => r.UserId == uid).Select(r => (ReactionType?)r.Type).FirstOrDefault()
            })
            .ToListAsync(ct);

        var items = rows.Select(r => new PostResponse(r.Id, eventId, new AuthorInfo(r.AuthorId, r.AuthorName, r.Photo),
            r.Text, r.Category, r.CreatedAt, r.UpdatedAt, r.Comments, r.Reactions, r.Mine,
            CanEdit: r.AuthorId == uid, CanDelete: r.AuthorId == uid || isMod)).ToList();
        return new PagedResponse<PostResponse>(items, page, pageSize, total);
    }

    [HttpPost("api/events/{eventId:guid}/posts"), Authorize(Policy = Policies.EventMember)]
    public async Task<ActionResult<PostResponse>> Create(Guid eventId, PostRequest req, CancellationToken ct)
    {
        if (!await db.Events.AnyAsync(e => e.Id == eventId, ct)) return NotFound();
        var uid = CurrentUserId;

        var since = DateTimeOffset.UtcNow.AddMinutes(-1);
        if (await db.Posts.CountAsync(p => p.AuthorId == uid && p.CreatedAt > since, ct) >= MaxPostsPerMinute)
            return Problem(statusCode: 429, title: "Você está publicando rápido demais. Aguarde um instante.");

        var post = new Post { EventId = eventId, AuthorId = uid, Text = req.Text.Trim(), Category = Mapping.Clean(req.Category) };
        db.Posts.Add(post);
        await db.SaveChangesAsync(ct);

        var author = await db.Users.AsNoTracking().Where(u => u.Id == uid)
            .Select(u => new AuthorInfo(u.Id, u.DisplayName, u.Profile!.PhotoUrl)).FirstAsync(ct);
        return StatusCode(StatusCodes.Status201Created, new PostResponse(post.Id, eventId, author, post.Text,
            post.Category, post.CreatedAt, null, 0, 0, null, true, true));
    }

    [HttpPut("api/posts/{postId:guid}")]
    public async Task<IActionResult> Update(Guid postId, PostRequest req, CancellationToken ct)
    {
        var post = await LoadVisiblePostAsync(postId, ct);
        if (post is null) return NotFound();
        if (post.AuthorId != CurrentUserId) return Problem(statusCode: 403, title: "Apenas o autor pode editar.");

        post.Text = req.Text.Trim();
        post.Category = Mapping.Clean(req.Category);
        post.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>Autor exclui o próprio conteúdo; organizador/admin oculta conteúdo de terceiros (RF16, RN08).</summary>
    [HttpDelete("api/posts/{postId:guid}")]
    public async Task<IActionResult> Delete(Guid postId, CancellationToken ct)
    {
        var post = await LoadVisiblePostAsync(postId, ct);
        if (post is null) return NotFound();

        if (post.AuthorId == CurrentUserId) post.Status = ContentStatus.Deleted;
        else if (await access.IsOrganizerAsync(post.EventId, User, ct))
        {
            post.Status = ContentStatus.Hidden;
            db.AuditLogs.Add(Audit(post.EventId, "post.hide", nameof(Post), post.Id));
        }
        else return Problem(statusCode: 403, title: "Você não pode remover conteúdo de terceiros. Use a denúncia.");

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ---------- Comentários ----------

    [HttpGet("api/posts/{postId:guid}/comments")]
    public async Task<ActionResult<PagedResponse<CommentResponse>>> Comments(
        Guid postId, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var post = await LoadVisiblePostAsync(postId, ct, track: false);
        if (post is null) return NotFound();
        (page, pageSize) = Mapping.Clamp(page, pageSize, 100);
        var uid = CurrentUserId;
        var isMod = await access.IsOrganizerAsync(post.EventId, User, ct);

        var query = db.Comments.AsNoTracking().Where(c => c.PostId == postId && c.Status == ContentStatus.Visible);
        var total = await query.CountAsync(ct);
        var rows = await query.OrderBy(c => c.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(c => new { c.Id, c.AuthorId, Name = c.Author.DisplayName, Photo = c.Author.Profile!.PhotoUrl, c.Text, c.CreatedAt })
            .ToListAsync(ct);
        var items = rows.Select(c => new CommentResponse(c.Id, postId, new AuthorInfo(c.AuthorId, c.Name, c.Photo),
            c.Text, c.CreatedAt, c.AuthorId == uid || isMod)).ToList();
        return new PagedResponse<CommentResponse>(items, page, pageSize, total);
    }

    [HttpPost("api/posts/{postId:guid}/comments")]
    public async Task<ActionResult<CommentResponse>> AddComment(Guid postId, CommentRequest req, CancellationToken ct)
    {
        var post = await LoadVisiblePostAsync(postId, ct, track: false);
        if (post is null) return NotFound();
        var uid = CurrentUserId;

        var since = DateTimeOffset.UtcNow.AddMinutes(-1);
        if (await db.Comments.CountAsync(c => c.AuthorId == uid && c.CreatedAt > since, ct) >= MaxCommentsPerMinute)
            return Problem(statusCode: 429, title: "Você está comentando rápido demais. Aguarde um instante.");

        var comment = new Comment { PostId = postId, AuthorId = uid, Text = req.Text.Trim() };
        db.Comments.Add(comment);
        await db.SaveChangesAsync(ct);

        var author = await db.Users.AsNoTracking().Where(u => u.Id == uid)
            .Select(u => new AuthorInfo(u.Id, u.DisplayName, u.Profile!.PhotoUrl)).FirstAsync(ct);
        return StatusCode(StatusCodes.Status201Created,
            new CommentResponse(comment.Id, postId, author, comment.Text, comment.CreatedAt, true));
    }

    [HttpDelete("api/comments/{commentId:guid}")]
    public async Task<IActionResult> DeleteComment(Guid commentId, CancellationToken ct)
    {
        var comment = await db.Comments.Include(c => c.Post)
            .FirstOrDefaultAsync(c => c.Id == commentId && c.Status == ContentStatus.Visible, ct);
        if (comment is not null)
        {
            if (!await access.IsMemberAsync(comment.Post.EventId, User, ct)) return NotFound();
            if (comment.AuthorId == CurrentUserId) comment.Status = ContentStatus.Deleted;
            else if (await access.IsOrganizerAsync(comment.Post.EventId, User, ct))
            {
                comment.Status = ContentStatus.Hidden;
                db.AuditLogs.Add(Audit(comment.Post.EventId, "comment.hide", nameof(Comment), comment.Id));
            }
            else return Problem(statusCode: 403, title: "Você não pode remover conteúdo de terceiros. Use a denúncia.");
        }
        else
        {
            var boothComment = await db.BoothComments.Include(c => c.Company)
                .FirstOrDefaultAsync(c => c.Id == commentId && c.Status == ContentStatus.Visible, ct);
            if (boothComment is null || !await access.IsMemberAsync(boothComment.Company.EventId, User, ct)) return NotFound();
            if (boothComment.AuthorId == CurrentUserId) boothComment.Status = ContentStatus.Deleted;
            else if (await access.IsOrganizerAsync(boothComment.Company.EventId, User, ct))
            {
                boothComment.Status = ContentStatus.Hidden;
                db.AuditLogs.Add(Audit(boothComment.Company.EventId, "booth.comment.hide", nameof(BoothComment), boothComment.Id));
            }
            else return Problem(statusCode: 403, title: "Você não pode remover conteúdo de terceiros. Use a denúncia.");
        }

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ---------- Reações ----------

    /// <summary>Uma reação por usuário/publicação: reagir de novo apenas troca o tipo.</summary>
    [HttpPost("api/posts/{postId:guid}/reactions")]
    public async Task<ActionResult<ReactionResponse>> React(Guid postId, ReactionRequest req, CancellationToken ct)
    {
        var post = await LoadVisiblePostAsync(postId, ct, track: false);
        if (post is null) return NotFound();

        var uid = CurrentUserId;
        var reaction = await db.Reactions.FindAsync(new object[] { postId, uid }, ct);
        if (reaction is null) db.Reactions.Add(new Reaction { PostId = postId, UserId = uid, Type = req.Type });
        else reaction.Type = req.Type;
        await db.SaveChangesAsync(ct);
        return new ReactionResponse(await db.Reactions.CountAsync(r => r.PostId == postId, ct), req.Type);
    }

    [HttpDelete("api/posts/{postId:guid}/reactions")]
    public async Task<ActionResult<ReactionResponse>> Unreact(Guid postId, CancellationToken ct)
    {
        var post = await LoadVisiblePostAsync(postId, ct, track: false);
        if (post is null) return NotFound();

        var reaction = await db.Reactions.FindAsync(new object[] { postId, CurrentUserId }, ct);
        if (reaction is not null)
        {
            db.Reactions.Remove(reaction);
            await db.SaveChangesAsync(ct);
        }
        return new ReactionResponse(await db.Reactions.CountAsync(r => r.PostId == postId, ct), null);
    }

    /// <summary>Carrega a publicação visível e garante que o usuário é membro do evento (senão, 404).</summary>
    private async Task<Post?> LoadVisiblePostAsync(Guid postId, CancellationToken ct, bool track = true)
    {
        var query = track ? db.Posts : db.Posts.AsNoTracking();
        var post = await query.FirstOrDefaultAsync(p => p.Id == postId && p.Status == ContentStatus.Visible, ct);
        if (post is null || !await access.IsMemberAsync(post.EventId, User, ct)) return null;
        return post;
    }
}
