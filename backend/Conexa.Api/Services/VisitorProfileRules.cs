namespace Conexa.Api.Services;

/// <summary>Resultado de aplicar os consentimentos pedidos sobre o estado atual.</summary>
public record ConsentState(DateTimeOffset? Matching, DateTimeOffset? ShareWithExhibitors, DateTimeOffset? ExternalAi);

/// <summary>Regras puras do perfil do visitante por evento (sem banco).</summary>
public static class VisitorProfileRules
{
    public const int MaxTags = 8;
    public const int MinGoalLength = 10;

    public static string? CleanGoal(string? goal) => Mapping.Clean(goal) is { } g ? TaxonomyRules.CleanName(g) : null;

    /// <summary>Temas do visitante: opcionais, até <see cref="MaxTags"/>, aprovados e do mesmo evento.</summary>
    public static string? ValidateTags(IReadOnlyCollection<Guid> ids, IReadOnlyCollection<InterestTag> found, Guid eventId)
    {
        var distinct = ids.Distinct().ToList();
        if (distinct.Count > MaxTags) return $"Escolha até {MaxTags} temas.";
        if (found.Count != distinct.Count || found.Any(t => t.EventId != eventId))
            return "Algum tema não existe neste evento.";
        if (found.Any(t => t.Status != TagStatus.Approved))
            return "Só é possível escolher temas aprovados.";
        return null;
    }

    /// <summary>
    /// Consentimentos por finalidade. Conceder preserva a data original; revogar apaga.
    /// Compartilhar com expositores e usar IA externa dependem do consentimento de matching:
    /// pedi-los sem ele é erro, e revogar o matching revoga os dois.
    /// </summary>
    public static (ConsentState? State, string? Error) ApplyConsents(
        ConsentState current, bool matching, bool share, bool externalAi, DateTimeOffset now)
    {
        if (!matching && (share || externalAi))
            return (null, "Compartilhar com expositores e usar IA externa exigem o consentimento de matching.");
        return (new ConsentState(
            matching ? current.Matching ?? now : null,
            share ? current.ShareWithExhibitors ?? now : null,
            externalAi ? current.ExternalAi ?? now : null), null);
    }

    /// <summary>Pronto para o matching: consentiu e informou ao menos o objetivo (ou temas).</summary>
    public static bool IsReadyForMatching(string? goal, int tagCount, DateTimeOffset? consentMatching) =>
        consentMatching is not null && ((goal?.Length ?? 0) >= MinGoalLength || tagCount > 0);
}
