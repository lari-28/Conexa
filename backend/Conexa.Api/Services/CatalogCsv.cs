using System.Text;

namespace Conexa.Api.Services;

public record CatalogRow(int Line, string Name, string? Segment, string? Description, string? Products,
    string? IdealCustomer, string? Booth, string? Website, List<string> Themes, string? Email, List<string> Problems);

public record CatalogParse(List<CatalogRow> Rows, List<string> FileErrors);

/// <summary>
/// Leitor do catálogo de expositores em CSV (vírgula, ponto e vírgula ou tab; aspas; BOM).
/// Cabeçalhos aceitam variações de nome e acento. Planilhas do Excel podem ser exportadas como CSV.
/// </summary>
public static class CatalogCsv
{
    public const int MaxRows = 1000;
    public const int MaxThemesPerRow = InterestTag.MaxPerCompany;

    private static readonly Dictionary<string, string> Aliases = new()
    {
        ["nome"] = "name", ["empresa"] = "name", ["expositor"] = "name", ["razao social"] = "name",
        ["segmento"] = "segment", ["setor"] = "segment",
        ["descricao"] = "description", ["sobre"] = "description",
        ["produtos"] = "products", ["servicos"] = "products", ["produtos e servicos"] = "products", ["produtos servicos"] = "products", ["oferta"] = "products",
        ["cliente ideal"] = "ideal", ["publico"] = "ideal", ["publico alvo"] = "ideal", ["cliente"] = "ideal",
        ["estande"] = "booth", ["localizacao"] = "booth", ["stand"] = "booth", ["pavilhao"] = "booth",
        ["site"] = "website", ["website"] = "website", ["url"] = "website",
        ["temas"] = "themes", ["tags"] = "themes", ["categorias"] = "themes",
        ["email"] = "email", ["e-mail"] = "email", ["email responsavel"] = "email",
    };

    private static string Canon(string h) => Aliases.GetValueOrDefault(TaxonomyRules.Key(h).Replace('_', ' ')) ?? "";

    public static CatalogParse Parse(string? text)
    {
        var errors = new List<string>();
        var rows = new List<CatalogRow>();
        if (string.IsNullOrWhiteSpace(text)) return new(rows, ["O arquivo está vazio."]);

        var records = Records(text.TrimStart('﻿'), Delimiter(text));
        if (records.Count == 0) return new(rows, ["O arquivo está vazio."]);

        var header = records[0].Select(Canon).ToList();
        if (!header.Contains("name")) return new(rows, ["Não encontrei a coluna do nome (use \"nome\" ou \"empresa\")."]);
        if (records.Count - 1 > MaxRows) return new(rows, [$"O arquivo tem mais de {MaxRows} linhas. Divida em partes."]);

        for (var i = 1; i < records.Count; i++)
        {
            var cells = records[i];
            if (cells.All(string.IsNullOrWhiteSpace)) continue;
            string? Cell(string col)
            {
                var idx = header.IndexOf(col);
                return idx >= 0 && idx < cells.Count ? Mapping.Clean(cells[idx]) : null;
            }
            var problems = new List<string>();
            var name = TaxonomyRules.CleanName(Cell("name") ?? "");
            if (name.Length < 2) problems.Add("Nome obrigatório.");
            if (name.Length > 200) problems.Add("Nome com mais de 200 caracteres.");

            var website = Cell("website");
            if (website is not null && !(Uri.TryCreate(website, UriKind.Absolute, out var u) && (u.Scheme is "http" or "https")))
                problems.Add("Site inválido (use http:// ou https://).");

            var email = Cell("email");
            if (email is not null && !(email.Contains('@') && email.Contains('.') && email.Length <= 200))
                problems.Add("E-mail do responsável inválido.");

            var themes = (Cell("themes") ?? "").Split([';', '|', '/'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(TaxonomyRules.CleanName).Where(t => t.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (themes.Count > MaxThemesPerRow)
            {
                problems.Add($"Mais de {MaxThemesPerRow} temas: só os {MaxThemesPerRow} primeiros foram considerados.");
                themes = themes.Take(MaxThemesPerRow).ToList();
            }

            rows.Add(new CatalogRow(i + 1, name, Cell("segment"), Cell("description"), Cell("products"),
                Cell("ideal"), Cell("booth"), website, themes, email, problems));
        }
        if (rows.Count == 0) errors.Add("Nenhuma linha com dados.");
        return new(rows, errors);
    }

    /// <summary>Uma linha é inválida (não importável) se tiver algum problema que não seja apenas aviso de temas.</summary>
    public static bool IsBlocking(CatalogRow r) => r.Problems.Any(p => !p.StartsWith("Mais de "));

    private static char Delimiter(string text)
    {
        var first = text.TrimStart('﻿').Split('\n', 2)[0];
        var counts = new[] { ';', ',', '\t' }.Select(c => (c, n: first.Count(x => x == c))).OrderByDescending(x => x.n).First();
        return counts.n == 0 ? ',' : counts.c;
    }

    /// <summary>Divide em registros respeitando aspas (inclusive quebras de linha dentro de aspas).</summary>
    private static List<List<string>> Records(string text, char delim)
    {
        var records = new List<List<string>>();
        var row = new List<string>();
        var cell = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (quoted)
            {
                if (c == '"' && i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; }
                else if (c == '"') quoted = false;
                else cell.Append(c);
            }
            else if (c == '"') quoted = true;
            else if (c == delim) { row.Add(cell.ToString()); cell.Clear(); }
            else if (c == '\r') { }
            else if (c == '\n') { row.Add(cell.ToString()); cell.Clear(); records.Add(row); row = []; }
            else cell.Append(c);
        }
        if (cell.Length > 0 || row.Count > 0) { row.Add(cell.ToString()); records.Add(row); }
        return records;
    }
}
