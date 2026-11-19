using System.ComponentModel.DataAnnotations;

namespace Conexa.Api.Contracts.Requests;

/// <summary>
/// Perfil do visitante no evento. O objetivo em texto livre é o sinal mais forte; os temas são opcionais.
/// Os consentimentos são separados por finalidade (LGPD) e nada é presumido.
/// </summary>
public record UpdateVisitorProfileRequest(
    [StringLength(1000)] string? Goal,
    VisitTimeframe? Timeframe,
    [MaxLength(20)] List<Guid>? TagIds,
    bool ConsentMatching,
    bool ConsentShareWithExhibitors,
    bool ConsentExternalAi);
