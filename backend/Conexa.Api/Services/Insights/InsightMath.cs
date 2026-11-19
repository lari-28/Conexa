using System.Text;

namespace Conexa.Api.Services.Insights;

/// <summary>Contas puras dos painéis (sem banco) e escrita de CSV segura.</summary>
public static class InsightMath
{
    /// <summary>
    /// Coeficiente de Gini da distribuição de exposição entre estandes (0 = todos igualmente recomendados,
    /// perto de 1 = poucos estandes concentram quase tudo). Estandes nunca recomendados entram como zero.
    /// </summary>
    public static double Gini(IReadOnlyList<int> values)
    {
        var n = values.Count;
        var total = values.Sum();
        if (n == 0 || total == 0) return 0;
        var sorted = values.Order().ToList();
        double weighted = 0;
        for (var i = 0; i < n; i++) weighted += (i + 1.0) * sorted[i];
        return Math.Round(2 * weighted / (n * (double)total) - (n + 1.0) / n, 4);
    }

    /// <summary>Fatia da exposição total que fica com os 20% de estandes mais recomendados (ao menos um).</summary>
    public static double TopShare(IReadOnlyList<int> values, double fraction = 0.2)
    {
        var total = values.Sum();
        if (values.Count == 0 || total == 0) return 0;
        var take = Math.Max(1, (int)Math.Ceiling(values.Count * fraction));
        return Math.Round(values.OrderByDescending(v => v).Take(take).Sum() / (double)total, 4);
    }

    /// <summary>O visitante "menciona" o tema se o nome ou um sinônimo aparece no objetivo (sem acento, palavra inteira).</summary>
    public static bool Mentions(string? goal, string name, IEnumerable<string> synonyms)
    {
        if (string.IsNullOrWhiteSpace(goal)) return false;
        var g = $" {Normalize(goal)} ";
        return synonyms.Prepend(name).Select(Normalize).Where(s => s.Length >= 2).Any(s => g.Contains($" {s} "));
    }

    private static string Normalize(string s)
    {
        var key = TaxonomyRules.Key(s);
        var sb = new StringBuilder(key.Length);
        foreach (var c in key) sb.Append(char.IsLetterOrDigit(c) ? c : ' ');
        return string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static readonly HashSet<string> Stop = new(
        ("para com uma uns umas dos das nos nas por que como mais mas sua seu suas seus minha meu quero quer preciso procuro " +
         "buscando busco ter fazer sobre entre ainda muito pela pelo isso esta este esse essa nossa nosso empresa").Split(' '));

    /// <summary>Palavras mais frequentes nos objetivos (uma contagem por visitante), para ver o que o público pede.</summary>
    public static List<(string Term, int Count)> TopTerms(IEnumerable<string?> goals, int take = 12)
    {
        var counts = new Dictionary<string, int>();
        foreach (var goal in goals)
            foreach (var w in Normalize(goal ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries)
                         .Where(w => w.Length >= 4 && !Stop.Contains(w)).Distinct())
                counts[w] = counts.GetValueOrDefault(w) + 1;
        return counts.Where(kv => kv.Value >= 2).OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key)
            .Take(take).Select(kv => (kv.Key, kv.Value)).ToList();
    }
}

/// <summary>CSV para Excel em português: separador ";" e BOM UTF-8. Neutraliza fórmulas (injeção de CSV).</summary>
public static class CsvWriter
{
    public static string Cell(object? value)
    {
        var s = value switch
        {
            null => "",
            double d => d.ToString("0.####", System.Globalization.CultureInfo.GetCultureInfo("pt-BR")),
            bool b => b ? "sim" : "não",
            _ => value.ToString() ?? "",
        };
        // Células que começam com = + - @ (ou tab/CR) seriam lidas como fórmula pelo Excel.
        if (s.Length > 0 && (s[0] is '=' or '+' or '-' or '@' or '\t' or '\r') && !double.TryParse(s, out _)) s = "'" + s;
        return s.IndexOfAny([';', '"', '\n', '\r']) >= 0 ? $"\"{s.Replace("\"", "\"\"")}\"" : s;
    }

    public static string Write(IEnumerable<string> headers, IEnumerable<IEnumerable<object?>> rows)
    {
        var sb = new StringBuilder("﻿");
        sb.Append(string.Join(';', headers.Select(h => Cell(h)))).Append("\r\n");
        foreach (var r in rows) sb.Append(string.Join(';', r.Select(Cell))).Append("\r\n");
        return sb.ToString();
    }
}
