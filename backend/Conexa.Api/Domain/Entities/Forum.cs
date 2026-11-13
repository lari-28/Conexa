namespace Conexa.Api.Domain.Entities;

public class Post
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;
    public Guid AuthorId { get; set; }
    public ApplicationUser Author { get; set; } = null!;
    public string Text { get; set; } = string.Empty;
    public string? Category { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public ContentStatus Status { get; set; } = ContentStatus.Visible;

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();
}

public class Comment
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid PostId { get; set; }
    public Post Post { get; set; } = null!;
    public Guid AuthorId { get; set; }
    public ApplicationUser Author { get; set; } = null!;
    public string Text { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ContentStatus Status { get; set; } = ContentStatus.Visible;
}

/// <summary>Chave composta (PostId, UserId): impede reação duplicada do mesmo usuário.</summary>
public class Reaction
{
    public Guid PostId { get; set; }
    public Post Post { get; set; } = null!;
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public ReactionType Type { get; set; } = ReactionType.Like;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
