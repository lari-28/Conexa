using System.ComponentModel.DataAnnotations;

namespace Conexa.Api.Contracts.Requests;

public record TagRequest(
    [Required, StringLength(100, MinimumLength = 2)] string Name,
    [StringLength(300)] string? Description,
    Guid? ParentId,
    List<string>? Synonyms);

public record SuggestTagRequest(
    [Required, StringLength(100, MinimumLength = 2)] string Name,
    [StringLength(300)] string? Description,
    Guid? ParentId);

public enum ReviewAction { Approve, Reject, Merge }

/// <summary>Revisão em lote da fila de sugestões. <c>MergeIntoId</c> é obrigatório em <c>Merge</c>.</summary>
public record ReviewTagsRequest(
    ReviewAction Action,
    [Required, MinLength(1), MaxLength(200)] List<Guid> TagIds,
    Guid? MergeIntoId);

public record ImportLibraryRequest([Required, StringLength(50)] string Sector);

public record SetCompanyTagsRequest([Required, MaxLength(10)] List<Guid> TagIds);
