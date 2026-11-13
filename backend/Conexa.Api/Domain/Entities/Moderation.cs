namespace Conexa.Api.Domain.Entities;

public class Report
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    /// <summary>Evento ao qual o alvo pertence; define qual organizador pode analisar (RN07).</summary>
    public Guid EventId { get; set; }
    public Guid ReporterId { get; set; }
    public ApplicationUser Reporter { get; set; } = null!;
    public ReportTargetType TargetType { get; set; }
    public Guid TargetId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public ReportStatus Status { get; set; } = ReportStatus.Open;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? ReviewedById { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
}

/// <summary>Registro de ações administrativas e de moderação.</summary>
public class AuditLog
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid? EventId { get; set; }
    public Guid ActorId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public Guid TargetId { get; set; }
    public string? Details { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
