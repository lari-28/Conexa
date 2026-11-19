using System.Text.RegularExpressions;

namespace Conexa.Api.Services.Matching;

public enum MatchBand { None, Low, Medium, High }

/// <summary>
/// Pesos e limiares do matching. ATENÇÃO: valores iniciais vindos de um teste sintético de autor único
/// (pesos 0,2 / 0,7 / 0,1 ajustados em 13 visitantes fantasia). São hipótese, não calibração:
/// devem ser reajustados com catálogo e visitantes reais antes de qualquer promessa de "% de match".
/// </summary>
public record MatchOptions(
    double ThemeWeight = 0.2, double TextWeight = 0.7, double AudienceWeight = 0.1,
    double HighAt = 0.45, double MediumAt = 0.25, double LowAt = 0.12,
    double SiblingThemeCredit = 0.5)
{
    public static readonly MatchOptions Default = new();
}

public record MatchBooth(Guid CompanyId, string Name, string Text, string? AudienceText,
    IReadOnlySet<Guid> TagIds);

public record MatchVisitor(Guid UserId, string? Goal, IReadOnlySet<Guid> TagIds, string? JobTitle, string? Area);

public record MatchResult(Guid UserId, Guid CompanyId, double Score, double? Theme, double? Text, double? Audience,
    MatchBand Band, IReadOnlyList<string> Reasons);

public record TagInfo(string Name, Guid? ParentId);

/// <summary>
/// Motor híbrido, sem rede e sem LLM: tema (chips) + texto livre (TF-IDF de n-gramas de caracteres) + público-alvo.
/// Sinais ausentes saem da soma e os pesos restantes são renormalizados. O texto livre do visitante é o sinal mais forte.
/// </summary>
public class MatchingEngine
{
    private static readonly Regex Word = new(@"[a-z0-9]+", RegexOptions.Compiled);
    private static readonly HashSet<string> Stop = new(
        ("para com uma uns umas dos das nos nas por que como mais mas sua seu suas seus minha meu quero quer preciso " +
         "procuro buscando busco ter fazer sobre entre ainda muito pela pelo isso esta este esse essa").Split(' '));

    private readonly MatchOptions _o;
    private readonly IReadOnlyDictionary<Guid, TagInfo> _tags;
    private readonly IReadOnlyList<MatchBooth> _booths;
    private readonly Dictionary<string, double> _idf = new();
    private readonly double _idfUnseen;
    private readonly Dictionary<Guid, Dictionary<string, double>> _boothVec = new();
    private readonly Dictionary<Guid, Dictionary<string, double>> _audVec = new();
    private readonly Dictionary<Guid, HashSet<string>> _boothWords = new();

    public MatchingEngine(IReadOnlyList<MatchBooth> booths, IReadOnlyDictionary<Guid, TagInfo> tags, MatchOptions? options = null)
    {
        _o = options ?? MatchOptions.Default;
        _tags = tags;
        _booths = booths;

        var docs = booths.Select(b => (b, grams: Grams(b.Text))).ToList();
        var n = Math.Max(1, docs.Count);
        var df = new Dictionary<string, int>();
        foreach (var (_, g) in docs)
            foreach (var k in g.Keys) df[k] = df.GetValueOrDefault(k) + 1;
        foreach (var (k, c) in df) _idf[k] = Math.Log((1.0 + n) / (1.0 + c)) + 1.0;
        _idfUnseen = Math.Log(1.0 + n) + 1.0;

        foreach (var (b, g) in docs)
        {
            _boothVec[b.CompanyId] = Weigh(g);
            _boothWords[b.CompanyId] = Tokens(b.Text).ToHashSet();
            if (!string.IsNullOrWhiteSpace(b.AudienceText)) _audVec[b.CompanyId] = Weigh(Grams(b.AudienceText));
        }
    }

    public MatchResult Score(MatchVisitor v, MatchBooth b)
    {
        double? theme = null, text = null, audience = null;

        if (v.TagIds.Count > 0 && b.TagIds.Count > 0) theme = ThemeScore(v.TagIds, b.TagIds);
        if (!string.IsNullOrWhiteSpace(v.Goal))
            text = Cosine(Weigh(Grams(v.Goal)), _boothVec[b.CompanyId]);
        var aud = string.Join(' ', new[] { v.JobTitle, v.Area }.Where(s => !string.IsNullOrWhiteSpace(s)));
        if (aud.Length > 0 && _audVec.TryGetValue(b.CompanyId, out var av))
            audience = Cosine(Weigh(Grams(aud)), av);

        double num = 0, den = 0;
        void Add(double? s, double w) { if (s is { } x) { num += w * x; den += w; } }
        Add(theme, _o.ThemeWeight); Add(text, _o.TextWeight); Add(audience, _o.AudienceWeight);
        // Público-alvo é sinal de apoio: sozinho (sem objetivo nem temas) não sustenta uma recomendação.
        var score = den == 0 || (theme is null && text is null) ? 0 : num / den;

        var band = score >= _o.HighAt ? MatchBand.High : score >= _o.MediumAt ? MatchBand.Medium
            : score >= _o.LowAt ? MatchBand.Low : MatchBand.None;
        return new MatchResult(v.UserId, b.CompanyId, score, theme, text, audience, band, Reasons(v, b, theme, audience));
    }

    /// <summary>Estandes ordenados para um visitante. Só inclui os acima do limiar de "sem match".</summary>
    public IReadOnlyList<MatchResult> RankBoothsFor(MatchVisitor v, int limit = 10) =>
        _booths.Select(b => Score(v, b)).Where(r => r.Band != MatchBand.None)
            .OrderByDescending(r => r.Score).ThenBy(r => r.CompanyId).Take(limit).ToList();

    /// <summary>Visitantes ordenados para um estande (leads), acima do limiar.</summary>
    public IReadOnlyList<MatchResult> RankVisitorsFor(MatchBooth b, IEnumerable<MatchVisitor> visitors, int limit = 20) =>
        visitors.Select(v => Score(v, b)).Where(r => r.Band != MatchBand.None)
            .OrderByDescending(r => r.Score).ThenBy(r => r.UserId).Take(limit).ToList();

    // ---------- Sinais ----------

    private double ThemeScore(IReadOnlySet<Guid> visitor, IReadOnlySet<Guid> booth)
    {
        double sum = 0;
        foreach (var t in visitor)
        {
            if (booth.Contains(t)) { sum += 1; continue; }
            var parent = _tags.GetValueOrDefault(t)?.ParentId;
            // mesma macro-área: crédito parcial (é a "conexão entre áreas próximas")
            if (parent is not null && booth.Any(bt => _tags.GetValueOrDefault(bt)?.ParentId == parent
                || bt == parent)) sum += _o.SiblingThemeCredit;
        }
        return sum / visitor.Count;
    }

    private IReadOnlyList<string> Reasons(MatchVisitor v, MatchBooth b, double? theme, double? audience)
    {
        var reasons = new List<string>();
        var shared = v.TagIds.Where(b.TagIds.Contains).Select(t => _tags.GetValueOrDefault(t)?.Name ?? "tema").ToList();
        if (shared.Count > 0)
            reasons.Add($"Cobre {shared.Count} de {v.TagIds.Count} tema(s) que você escolheu: {string.Join(", ", shared)}.");
        else if (theme is > 0)
            reasons.Add("Atua em áreas próximas dos temas que você escolheu.");

        if (!string.IsNullOrWhiteSpace(v.Goal))
        {
            var words = Tokens(v.Goal).Where(w => w.Length >= 4 && !Stop.Contains(w))
                .Distinct().Where(_boothWords[b.CompanyId].Contains).Take(4).ToList();
            if (words.Count > 0) reasons.Add($"Termos em comum com o seu objetivo: {string.Join(", ", words)}.");
        }
        if (audience is >= 0.2 && !string.IsNullOrWhiteSpace(b.AudienceText))
            reasons.Add($"O perfil que o estande quer atender se parece com o seu: {b.AudienceText!.Trim()}");
        return reasons;
    }

    // ---------- Texto ----------

    private static IEnumerable<string> Tokens(string? s) =>
        s is null ? [] : Word.Matches(TaxonomyRules.Key(s)).Select(m => m.Value);

    private static Dictionary<string, double> Grams(string? s)
    {
        var tf = new Dictionary<string, double>();
        foreach (var w in Tokens(s))
        {
            if (w.Length < 2) continue;
            var p = $" {w} ";
            for (var n = 3; n <= 5; n++)
                for (var i = 0; i + n <= p.Length; i++)
                {
                    var g = p.Substring(i, n);
                    tf[g] = tf.GetValueOrDefault(g) + 1;
                }
        }
        return tf;
    }

    private Dictionary<string, double> Weigh(Dictionary<string, double> tf)
    {
        var v = new Dictionary<string, double>(tf.Count);
        foreach (var (g, c) in tf) v[g] = (1 + Math.Log(c)) * _idf.GetValueOrDefault(g, _idfUnseen);
        return v;
    }

    private static double Cosine(Dictionary<string, double> a, Dictionary<string, double> b)
    {
        if (a.Count == 0 || b.Count == 0) return 0;
        var (small, large) = a.Count <= b.Count ? (a, b) : (b, a);
        double dot = 0;
        foreach (var (k, x) in small) if (large.TryGetValue(k, out var y)) dot += x * y;
        var na = Math.Sqrt(a.Values.Sum(x => x * x));
        var nb = Math.Sqrt(b.Values.Sum(x => x * x));
        return na == 0 || nb == 0 ? 0 : dot / (na * nb);
    }
}
