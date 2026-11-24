namespace Conexa.Api.Domain.Entities;

public enum InteractionType { CheckIn, FeedbackUseful, FeedbackNotUseful }

/// <summary>
/// Ação do visitante registrada para os insights do evento: visita ao estande (check-in por QR)
/// ou "isso foi útil?" sobre uma recomendação. Feedback guarda no máximo um registro por visitante e estande.
/// </summary>
public class Interaction
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public InteractionType Type { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>Aviso dentro do app. <c>DedupeKey</c> impede avisos repetidos do mesmo fato para o mesmo usuário.</summary>
public class Notification
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public Guid EventId { get; set; }
    public string DedupeKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Link { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReadAt { get; set; }
}
