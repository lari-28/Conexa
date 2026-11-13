namespace Conexa.Api.Domain.Entities;

/// <summary>Conta de acesso (ASP.NET Core Identity). A senha é guardada apenas como hash.</summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>false = conta suspensa globalmente (RN09: registros são preservados).</summary>
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public UserProfile? Profile { get; set; }
    public ContactPrivacy? Privacy { get; set; }
}
