namespace Conexa.Api.Domain.Entities;

/// <summary>
/// Tema da taxonomia de um evento, em dois níveis: macro-área (ParentId nulo) e tema.
/// Sinônimos e descrição curta existem para distinguir termos ambíguos (ex.: "apontamento" em ERP e em IoT).
/// </summary>
public class InterestTag
{
    public const int MaxPerCompany = 3;

    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;
    public Guid? ParentId { get; set; }
    public InterestTag? Parent { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Synonyms { get; set; } = new();
    public TagStatus Status { get; set; } = TagStatus.Suggested;
    /// <summary>Chave do tema na biblioteca-base por setor, quando veio dela.</summary>
    public string? LibraryKey { get; set; }
}

/// <summary>Tema escolhido por um estande (até <see cref="InterestTag.MaxPerCompany"/>).</summary>
public class CompanyTag
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public Guid TagId { get; set; }
    public InterestTag Tag { get; set; } = null!;
}

/// <summary>
/// Perfil do visitante dentro de um evento. O objetivo em texto livre é o sinal mais forte do matching;
/// os temas (chips) são opcionais. Consentimentos são por finalidade (LGPD).
/// </summary>
public class ParticipantEventProfile
{
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public string? Goal { get; set; }
    public VisitTimeframe? Timeframe { get; set; }
    public ICollection<ParticipantTag> Tags { get; set; } = new List<ParticipantTag>();

    public DateTimeOffset? ConsentMatchingAt { get; set; }
    public DateTimeOffset? ConsentShareWithExhibitorsAt { get; set; }
    public DateTimeOffset? ConsentExternalAiAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class ParticipantTag
{
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }
    public ParticipantEventProfile Profile { get; set; } = null!;
    public Guid TagId { get; set; }
    public InterestTag Tag { get; set; } = null!;
}
