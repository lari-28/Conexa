namespace Conexa.Api.Domain.Entities;

public class UserProfile
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public string? PhotoUrl { get; set; }
    public string? CompanyName { get; set; }
    public string? JobTitle { get; set; }
    public string? Area { get; set; }
    public string? Bio { get; set; }
    /// <summary>Áreas/interesses, normalizados em minúsculas (coluna text[] no PostgreSQL).</summary>
    public List<string> Interests { get; set; } = new();
    public string? LookingFor { get; set; }
    public string? Offers { get; set; }
}

/// <summary>Preferências de privacidade de contato (RF05, RN04, RN05).</summary>
public class ContactPrivacy
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public bool EmailVisible { get; set; }
    public bool PhoneVisible { get; set; }
    public string? Phone { get; set; }
    /// <summary>Preenchido quando o titular consente a exibição de algum contato.</summary>
    public DateTimeOffset? ConsentGivenAt { get; set; }
}
