using System.ComponentModel.DataAnnotations;

namespace Conexa.Api.Contracts.Requests;

/// <summary>Papel do convite: só visitante ou expositor (organizador é designado pelo administrador).</summary>
public record CreateInviteRequest(EventRole Role, [Range(1, 100000)] int? MaxUses, [Range(1, 365)] int? ExpiresInDays);

/// <summary>Catálogo em CSV. <c>Commit=false</c> só valida e mostra o que seria feito.</summary>
public record ImportExhibitorsRequest([Required, StringLength(1_000_000)] string Csv, bool Commit);
