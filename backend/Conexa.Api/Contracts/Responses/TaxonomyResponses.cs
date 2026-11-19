namespace Conexa.Api.Contracts.Responses;

public record TagResponse(Guid Id, Guid? ParentId, string Name, string? Description,
    List<string> Synonyms, TagStatus Status, string? LibraryKey);

/// <summary>Sugestões parecidas agrupadas para aprovação em lote. Se bater com um tema já aprovado, indica a fusão.</summary>
public record SuggestionGroup(string Key, string Name, List<TagResponse> Items, Guid? MatchesTagId, string? MatchesTagName);

public record LibrarySector(string Key, string Name, int MacroAreas, int Themes);

public record ReviewResult(int Approved, int Rejected, int Merged);
