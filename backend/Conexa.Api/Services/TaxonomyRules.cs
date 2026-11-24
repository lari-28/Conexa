using System.Globalization;
using System.Text;

namespace Conexa.Api.Services;

/// <summary>Regras puras da taxonomia (sem banco), para serem testadas isoladamente.</summary>
public static class TaxonomyRules
{
    public const int MaxSynonyms = 15;
    public const int MaxSynonymLength = 60;

    /// <summary>Remove espaços repetidos nas pontas e no meio.</summary>
    public static string CleanName(string s) =>
        string.Join(' ', s.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    /// <summary>Chave de comparação: sem acento, minúscula, sem espaços repetidos.</summary>
    public static string Key(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;
        var d = CleanName(s).Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(d.Length);
        foreach (var c in d)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        return sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }

    /// <summary>Sinônimos limpos, sem duplicatas (por chave) e sem repetir o próprio nome.</summary>
    public static List<string> NormalizeSynonyms(IEnumerable<string>? items, string name)
    {
        var seen = new HashSet<string> { Key(name) };
        var result = new List<string>();
        foreach (var raw in items ?? Array.Empty<string>())
        {
            var s = CleanName(raw ?? string.Empty);
            if (s.Length == 0 || s.Length > MaxSynonymLength) continue;
            if (!seen.Add(Key(s))) continue;
            result.Add(s);
            if (result.Count == MaxSynonyms) break;
        }
        return result;
    }

    /// <summary>Taxonomia tem 2 níveis: o pai precisa ser macro-área (sem pai) do mesmo evento.</summary>
    public static string? ValidateParent(InterestTag? parent, Guid eventId, Guid? selfId) =>
        parent is null ? "A macro-área informada não existe."
        : parent.EventId != eventId ? "A macro-área pertence a outro evento."
        : parent.Id == selfId ? "Um tema não pode ser pai de si mesmo."
        : parent.ParentId is not null ? "Só há dois níveis: o pai precisa ser uma macro-área."
        : parent.Status == TagStatus.Rejected ? "A macro-área informada foi rejeitada."
        : null;

    /// <summary>Um estande escolhe até 3 temas distintos, aprovados e do mesmo evento.</summary>
    public static string? ValidateCompanyTags(IReadOnlyCollection<Guid> ids, IReadOnlyCollection<InterestTag> found, Guid eventId)
    {
        var distinct = ids.Distinct().ToList();
        if (distinct.Count > InterestTag.MaxPerCompany)
            return $"Escolha até {InterestTag.MaxPerCompany} temas.";
        if (found.Count != distinct.Count || found.Any(t => t.EventId != eventId))
            return "Algum tema não existe neste evento.";
        if (found.Any(t => t.Status != TagStatus.Approved))
            return "Só é possível escolher temas aprovados.";
        return null;
    }

    /// <summary>
    /// Agrupa sugestões de mesmo nome (chave) e marca as que coincidem com nome ou sinônimo de um tema aprovado,
    /// indicando a fusão em vez de um tema novo.
    /// </summary>
    public static List<SuggestionGroup> GroupSuggestions(IEnumerable<InterestTag> suggested, IReadOnlyCollection<InterestTag> approved)
    {
        var lookup = new Dictionary<string, InterestTag>();
        foreach (var t in approved)
        {
            lookup.TryAdd(Key(t.Name), t);
            foreach (var s in t.Synonyms) lookup.TryAdd(Key(s), t);
        }
        return suggested
            .GroupBy(t => Key(t.Name))
            .Select(g =>
            {
                lookup.TryGetValue(g.Key, out var match);
                var first = g.OrderBy(t => t.Name, StringComparer.Ordinal).First();
                return new SuggestionGroup(g.Key, first.Name,
                    g.Select(ToResponse).ToList(), match?.Id, match?.Name);
            })
            .OrderBy(g => g.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public static TagResponse ToResponse(InterestTag t) =>
        new(t.Id, t.ParentId, t.Name, t.Description, t.Synonyms, t.Status, t.LibraryKey);
}
