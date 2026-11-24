using System.Security.Cryptography;

namespace Conexa.Api.Domain.Entities;

/// <summary>Convite por link: quem se cadastra com o código entra no evento com o papel definido.</summary>
public class EventInvite
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;
    public string Code { get; set; } = NewCode();
    public EventRole Role { get; set; } = EventRole.Participant;
    public int? MaxUses { get; set; }
    public int Uses { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public Guid CreatedById { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>128 bits aleatórios em base64url (22 caracteres): impossível de adivinhar.</summary>
    public static string NewCode() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(16))
        .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    /// <summary>Motivo pelo qual o convite não pode ser usado agora, ou nulo se for válido.</summary>
    public string? InvalidReason(DateTimeOffset now) =>
        RevokedAt is not null ? "Este convite foi cancelado."
        : ExpiresAt is { } e && e <= now ? "Este convite expirou."
        : MaxUses is { } m && Uses >= m ? "Este convite atingiu o limite de usos."
        : null;
}
