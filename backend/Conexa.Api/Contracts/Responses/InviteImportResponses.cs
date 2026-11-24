namespace Conexa.Api.Contracts.Responses;

public record InviteResponse(Guid Id, string Code, EventRole Role, int? MaxUses, int Uses,
    DateTimeOffset? ExpiresAt, DateTimeOffset? RevokedAt, bool Valid, DateTimeOffset CreatedAt);

/// <summary>Resposta pública: não revela nada além do necessário para o cadastro.</summary>
public record InvitePreview(bool Valid, string? Reason, string? EventName, EventRole? Role);

public record ImportRowResult(int Line, string Name, string Status, List<string> Messages);

public record ImportReport(bool Committed, int Total, int Created, int Skipped, int Invalid,
    List<ImportRowResult> Rows, List<string> FileErrors, List<string> NewThemeSuggestions);
