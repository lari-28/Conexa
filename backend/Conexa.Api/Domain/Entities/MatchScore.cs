namespace Conexa.Api.Domain.Entities;

/// <summary>
/// Afinidade entre um visitante e um estande em um evento, com a decomposição do score e a explicação.
/// Recalculada sob demanda; guardada para análise (ex.: concentração de exposição) e auditoria.
/// </summary>
public class MatchScore
{
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public double Score { get; set; }
    public double? ThemeScore { get; set; }
    public double? TextScore { get; set; }
    public double? AudienceScore { get; set; }
    public string Band { get; set; } = "None";
    public List<string> Reasons { get; set; } = new();
    public DateTimeOffset ComputedAt { get; set; } = DateTimeOffset.UtcNow;
}
