using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Conexa.Api.Services;
using Conexa.Api.Services.Matching;

// Avalia o motor de matching contra um catálogo e visitantes com gabarito.
// Uso: dotnet run --project backend/Conexa.Eval -- <catalogo.csv> <visitantes.json> [saida.md]
//
// Regras para os números valerem (iguais às do avaliador anterior):
//  - os pesos só podem ser comparados/ajustados nos visitantes de DESENVOLVIMENTO; os números oficiais são os de TESTE;
//  - a divisão é fixa (ids ímpares = desenvolvimento, pares = teste) e não depende dos resultados;
//  - visitantes sem estande relevante medem se o sistema fica em dúvida; os de objetivo vago são reportados à parte.

var pt = CultureInfo.GetCultureInfo("pt-BR");
if (args.Length < 2) { Console.Error.WriteLine("Uso: Conexa.Eval <catalogo.csv> <visitantes.json> [saida.md]"); return 2; }

var parse = CatalogCsv.Parse(File.ReadAllText(args[0]));
if (parse.FileErrors.Count > 0) { Console.Error.WriteLine(string.Join("\n", parse.FileErrors)); return 1; }
var visitors = JsonSerializer.Deserialize<List<Vis>>(File.ReadAllText(args[1]), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

// ---------- catálogo -> estandes (mesma montagem do MatchingService, sem temas: o catálogo não tem taxonomia) ----------
var booths = parse.Rows.Select(r => new MatchBooth(Guid.NewGuid(), r.Name,
    string.Join(". ", new[] { r.Name, r.Segment, r.Description, r.Products }.Where(s => !string.IsNullOrWhiteSpace(s))),
    r.IdealCustomer, new HashSet<Guid>())).ToList();
var byName = booths.ToDictionary(b => TaxonomyRules.Key(b.Name), b => b);
var empty = new Dictionary<Guid, TagInfo>();

var problems = new List<string>();
if (booths.Count != byName.Count) problems.Add("Há nomes de estande repetidos (por chave).");
var gains = new Dictionary<string, Dictionary<Guid, int>>();
foreach (var v in visitors)
{
    var g = new Dictionary<Guid, int>();
    foreach (var n in v.RelevantesAlgum) if (byName.TryGetValue(TaxonomyRules.Key(n), out var b)) g[b.CompanyId] = 1; else problems.Add($"{v.Id}: estande inexistente \"{n}\"");
    foreach (var n in v.RelevantesMuito) if (byName.TryGetValue(TaxonomyRules.Key(n), out var b)) g[b.CompanyId] = 2; else problems.Add($"{v.Id}: estande inexistente \"{n}\"");
    gains[v.Id] = g;
}

// ---------- grupos ----------
int Num(Vis v) => int.Parse(v.Id[1..]);
bool Labeled(Vis v) => gains[v.Id].Count > 0;
var zero = visitors.Where(v => !Labeled(v)).ToList();
var vague = visitors.Where(v => Labeled(v) && Num(v) <= 20).ToList();           // V09-V20: objetivo vago, rótulo discutível
var specific = visitors.Where(v => Labeled(v) && Num(v) > 20).ToList();         // V21-V60
var dev = specific.Where(v => Num(v) % 2 == 1).ToList();
var test = specific.Where(v => Num(v) % 2 == 0).ToList();

// ---------- métodos ----------
MatchVisitor ToMv(Vis v) => new(Guid.NewGuid(), v.Objetivo, new HashSet<Guid>(), v.Cargo, v.Setor);
Dictionary<string, Func<Vis, IReadOnlyList<(Guid Id, double Score)>>> methods = new();

IReadOnlyList<(Guid Id, double Score)> EngineRank(MatchingEngine e, Vis v) =>
    booths.Select(b => (Id: b.CompanyId, Score: e.Score(ToMv(v), b).Score)).OrderByDescending(x => x.Score).ThenBy(x => x.Id).ToList();

var eText = new MatchingEngine(booths, empty, new MatchOptions(0, 1, 0));
var eDefault = new MatchingEngine(booths, empty, MatchOptions.Default);
var eAud = new MatchingEngine(booths, empty, new MatchOptions(0, 0.5, 0.5));
var stop = new HashSet<string>("para com uma uns umas dos das nos nas por que como mais mas sua seu suas seus minha meu quero quer preciso procuro buscando busco ter fazer sobre entre ainda muito pela pelo isso esta este esse essa nosso nossa tambem alem pra tá tô vim".Split(' ').Select(TaxonomyRules.Key));
HashSet<string> Words(string s) => System.Text.RegularExpressions.Regex.Matches(TaxonomyRules.Key(s), "[a-z0-9]+").Select(m => m.Value).Where(w => w.Length >= 4 && !stop.Contains(w)).ToHashSet();
var boothWords = booths.ToDictionary(b => b.CompanyId, b => Words(b.Text));

methods["aleatório (média de 200 sorteios)"] = v => Array.Empty<(Guid, double)>();   // tratado à parte
methods["busca por palavra (sem sinônimos)"] = v =>
{
    var q = Words(v.Objetivo);
    return booths.Select(b => (b.CompanyId, (double)q.Count(boothWords[b.CompanyId].Contains))).OrderByDescending(x => x.Item2).ThenBy(x => x.CompanyId).ToList();
};
methods["motor: só texto (TF-IDF de n-gramas)"] = v => EngineRank(eText, v);
methods["motor: texto + público-alvo (padrão atual)"] = v => EngineRank(eDefault, v);
methods["motor: texto 50% + público-alvo 50%"] = v => EngineRank(eAud, v);
// Experimento: o público-alvo vira o sinal principal só quando o objetivo é fraco (melhor score de texto < 0,20).
// O 0,20 foi fixado ANTES de olhar o teste (coincide com o limiar de "baixa" discutido); não foi varrido.
methods["motor: adaptativo (público-alvo pesa mais se o texto é fraco)"] = v =>
    EngineRank(eText, v)[0].Score < 0.20 ? EngineRank(eAud, v) : EngineRank(eDefault, v);

// ---------- métricas ----------
double Dcg(IEnumerable<int> g) => g.Select((x, i) => x / Math.Log2(i + 2)).Sum();
Metrics Eval(Vis v, IReadOnlyList<Guid> ranked)
{
    var g = gains[v.Id];
    var top5 = ranked.Take(5).Select(id => g.GetValueOrDefault(id)).ToList();
    var ideal = g.Values.OrderByDescending(x => x).Take(5).ToList();
    var firstRel = ranked.Select((id, i) => (id, i)).FirstOrDefault(x => g.ContainsKey(x.id));
    return new Metrics(
        Dcg(top5) / Dcg(ideal),
        top5.Count(x => x > 0) / 5.0,
        top5.Count(x => x > 0) / (double)g.Count,
        ranked.Take(3).Any(id => g.ContainsKey(id)) ? 1 : 0,
        g.ContainsKey(firstRel.id) ? 1.0 / (firstRel.i + 1) : 0);
}
Metrics Avg(IEnumerable<Metrics> m) { var l = m.ToList(); return new(l.Average(x => x.Ndcg), l.Average(x => x.P5), l.Average(x => x.R5), l.Average(x => x.Hit3), l.Average(x => x.Mrr)); }

var rng = new Random(20261123);
Dictionary<string, Dictionary<string, Metrics>> perVisitor = new();   // método -> id -> métricas
foreach (var (name, fn) in methods)
{
    var d = new Dictionary<string, Metrics>();
    foreach (var v in visitors.Where(Labeled))
    {
        if (name.StartsWith("aleatório"))
        {
            var all = booths.Select(b => b.CompanyId).ToList();
            var runs = Enumerable.Range(0, 200).Select(_ => Eval(v, all.OrderBy(_ => rng.Next()).ToList())).ToList();
            d[v.Id] = Avg(runs);
        }
        else d[v.Id] = Eval(v, fn(v).Select(x => x.Id).ToList());
    }
    perVisitor[name] = d;
}

// ---------- bootstrap pareado (teste): motor padrão vs busca por palavra ----------
(double Lo, double Hi, double Mean) Boot(string a, string b, IReadOnlyList<Vis> set)
{
    var diffs = new double[set.Count];
    for (var i = 0; i < set.Count; i++) diffs[i] = perVisitor[a][set[i].Id].Ndcg - perVisitor[b][set[i].Id].Ndcg;
    var r = new Random(7); var means = new double[5000];
    for (var k = 0; k < means.Length; k++) { double s = 0; for (var i = 0; i < diffs.Length; i++) s += diffs[r.Next(diffs.Length)]; means[k] = s / diffs.Length; }
    Array.Sort(means);
    return (means[(int)(0.025 * means.Length)], means[(int)(0.975 * means.Length)], diffs.Average());
}

// ---------- relatório ----------
string P(double x) => x.ToString("P0", pt);
var sb = new StringBuilder();
sb.AppendLine("# Avaliação do matching: catálogo e visitantes gerados pelo Gemini");
sb.AppendLine();
sb.AppendLine($"Gerado por `Conexa.Eval` em {DateTime.Now:dd/MM/yyyy}. Catálogo: {booths.Count} estandes. Visitantes: {visitors.Count} ({specific.Count} com objetivo específico, {vague.Count} vagos, {zero.Count} sem estande relevante).");
sb.AppendLine();
sb.AppendLine("> **Leia isto antes dos números.** Catálogo, visitantes e gabarito vieram do mesmo modelo de IA, e o objetivo dos visitantes reaproveita muitas palavras do catálogo (os visitantes específicos descrevem a necessidade com os termos técnicos dos estandes). Os valores absolutos são otimistas; as comparações entre métodos são mais confiáveis do que os números isolados. O catálogo não tem taxonomia, então o sinal de **tema não foi medido** aqui.");
sb.AppendLine();
if (problems.Count > 0) { sb.AppendLine("## Problemas nos dados"); problems.ForEach(p => sb.AppendLine($"- {p}")); sb.AppendLine(); }

sb.AppendLine("## Qualidade do ranking (visitantes específicos)");
sb.AppendLine();
sb.AppendLine($"Desenvolvimento = ids ímpares ({dev.Count}); **teste = ids pares ({test.Count}), números oficiais**. nDCG@5 usa ganho 2 (muito relevante) e 1 (alguma relevância).");
sb.AppendLine();
void Table(string title, IReadOnlyList<Vis> set)
{
    sb.AppendLine($"### {title} (n = {set.Count})");
    sb.AppendLine();
    sb.AppendLine("| Método | nDCG@5 | Precisão@5 | Revocação@5 | Acerta em top-3 | MRR |");
    sb.AppendLine("|---|---|---|---|---|---|");
    foreach (var name in methods.Keys)
    {
        var m = Avg(set.Select(v => perVisitor[name][v.Id]));
        sb.AppendLine($"| {name} | {P(m.Ndcg)} | {P(m.P5)} | {P(m.R5)} | {P(m.Hit3)} | {m.Mrr.ToString("0.00", pt)} |");
    }
    sb.AppendLine();
}
Table("Teste (oficial)", test);
Table("Desenvolvimento", dev);

var def = "motor: texto + público-alvo (padrão atual)";
var kw = "busca por palavra (sem sinônimos)";
var bt = Boot(def, kw, test); var bd = Boot(def, kw, dev);
sb.AppendLine($"Diferença de nDCG@5, motor padrão menos busca por palavra (bootstrap pareado, 5000 reamostragens): **teste {bt.Mean * 100:+0.0;-0.0} pontos (IC 95%: {bt.Lo * 100:+0.0;-0.0} a {bt.Hi * 100:+0.0;-0.0})**; desenvolvimento {bd.Mean * 100:+0.0;-0.0} pontos (IC: {bd.Lo * 100:+0.0;-0.0} a {bd.Hi * 100:+0.0;-0.0}).");
sb.AppendLine();

Table("Objetivo vago (V09 a V20): só descritivo, o rótulo é discutível", vague);

// ---------- limiar de "sem match" ----------
sb.AppendLine("## Limiar de \"sem match\" e calibração das faixas");
sb.AppendLine();
sb.AppendLine("Motor padrão. Para cada limiar de score: **cobertura** = % dos visitantes específicos com ao menos uma recomendação; **falso alarme** = % dos visitantes sem estande relevante que ainda receberiam alguma recomendação; **precisão** = % das recomendações que são relevantes (visitantes específicos, até 10 por pessoa).");
sb.AppendLine();
sb.AppendLine("| Limiar | Cobertura | Falso alarme | Precisão das recomendações |");
sb.AppendLine("|---|---|---|---|");
var scored = visitors.Where(v => Labeled(v) ? Num(v) > 20 : true).ToDictionary(v => v.Id, v => EngineRank(eDefault, v));
foreach (var t in new[] { 0.05, 0.08, 0.10, 0.12, 0.15, 0.20, 0.25, 0.30, 0.40, 0.45 })
{
    var cover = specific.Count(v => scored[v.Id].Any(x => x.Score >= t)) / (double)specific.Count;
    var fa = zero.Count(v => scored[v.Id].Any(x => x.Score >= t)) / (double)zero.Count;
    var recs = specific.SelectMany(v => scored[v.Id].Where(x => x.Score >= t).Take(10).Select(x => gains[v.Id].ContainsKey(x.Id))).ToList();
    sb.AppendLine($"| {t.ToString("0.00", pt)} | {P(cover)} | {P(fa)} | {(recs.Count == 0 ? "—" : P(recs.Count(x => x) / (double)recs.Count))} ({recs.Count} itens) |");
}
sb.AppendLine();
var o = MatchOptions.Default;
sb.AppendLine($"Limiares atuais do sistema: baixa ≥ {o.LowAt.ToString("0.00", pt)}, média ≥ {o.MediumAt.ToString("0.00", pt)}, alta ≥ {o.HighAt.ToString("0.00", pt)}.");
sb.AppendLine();
sb.AppendLine("| Faixa atual | Recomendações (específicos) | % relevantes |");
sb.AppendLine("|---|---|---|");
foreach (var (label, lo, hi) in new[] { ("alta", o.HighAt, 9.0), ("média", o.MediumAt, o.HighAt), ("baixa", o.LowAt, o.MediumAt) })
{
    var items = specific.SelectMany(v => scored[v.Id].Where(x => x.Score >= lo && x.Score < hi).Take(10).Select(x => gains[v.Id].ContainsKey(x.Id))).ToList();
    sb.AppendLine($"| {label} | {items.Count} | {(items.Count == 0 ? "—" : P(items.Count(x => x) / (double)items.Count))} |");
}
sb.AppendLine();
sb.AppendLine("Melhor score de cada visitante sem estande relevante (deveriam ficar abaixo do limiar):");
sb.AppendLine();
sb.AppendLine("| Visitante | Objetivo | Melhor score | Faixa |");
sb.AppendLine("|---|---|---|---|");
foreach (var v in zero)
{
    var best = scored[v.Id][0].Score;
    var band = best >= o.HighAt ? "alta" : best >= o.MediumAt ? "média" : best >= o.LowAt ? "baixa" : "sem match";
    sb.AppendLine($"| {v.Id} | {Trunc(v.Objetivo, 70)} | {best.ToString("0.00", pt)} | {band} |");
}
sb.AppendLine();
var topSpec = specific.Select(v => scored[v.Id][0].Score).OrderBy(x => x).ToList();
sb.AppendLine($"Melhor score dos visitantes específicos: mínimo {topSpec.First().ToString("0.00", pt)}, mediana {topSpec[topSpec.Count / 2].ToString("0.00", pt)}, máximo {topSpec.Last().ToString("0.00", pt)}.");
sb.AppendLine();

// ---------- diagnóstico: onde falha ----------
sb.AppendLine("## Onde o motor erra (visitantes específicos, pior nDCG@5 primeiro)");
sb.AppendLine();
sb.AppendLine("| Visitante | nDCG@5 | 1º lugar do motor | Relevantes (muito) |");
sb.AppendLine("|---|---|---|---|");
var nameById = booths.ToDictionary(b => b.CompanyId, b => b.Name);
foreach (var v in specific.OrderBy(v => perVisitor[def][v.Id].Ndcg).Take(8))
    sb.AppendLine($"| {v.Id} | {P(perVisitor[def][v.Id].Ndcg)} | {Trunc(nameById[scored[v.Id][0].Id], 38)} | {Trunc(string.Join("; ", v.RelevantesMuito), 70)} |");
sb.AppendLine();

// ---------- exposição ----------
var exposure = booths.ToDictionary(b => b.CompanyId, _ => 0);
foreach (var v in visitors.Where(v => !Labeled(v) || Num(v) > 20 || true)) foreach (var x in EngineRank(eDefault, v).Where(x => x.Score >= o.LowAt).Take(5)) exposure[x.Id]++;
var counts = exposure.Values.ToList();
sb.AppendLine("## Exposição entre estandes (top-5 de cada um dos 60 visitantes)");
sb.AppendLine();
sb.AppendLine($"Estandes que aparecem para alguém: {counts.Count(c => c > 0)} de {counts.Count}. Nunca aparecem: {counts.Count(c => c == 0)}. Mais recomendado: {exposure.OrderByDescending(kv => kv.Value).First().Value} visitantes. Gini {InsightsGini(counts).ToString("0.00", pt)}.");
sb.AppendLine();

var report = sb.ToString();
Console.WriteLine(report);
if (args.Length > 2) File.WriteAllText(args[2], report, new UTF8Encoding(false));
return 0;

static string Trunc(string s, int n) => s.Length <= n ? s : s[..(n - 1)] + "…";
static double InsightsGini(IReadOnlyList<int> v) => Conexa.Api.Services.Insights.InsightMath.Gini(v);

record Vis(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("cargo")] string Cargo,
    [property: JsonPropertyName("setor")] string Setor,
    [property: JsonPropertyName("porte")] string Porte,
    [property: JsonPropertyName("objetivo")] string Objetivo,
    [property: JsonPropertyName("relevantes_muito")] List<string> RelevantesMuito,
    [property: JsonPropertyName("relevantes_algum")] List<string> RelevantesAlgum);
record Metrics(double Ndcg, double P5, double R5, double Hit3, double Mrr);
