using Conexa.Api.Services.Matching;

namespace Conexa.Api.Tests;

public class MatchingReasonsTests
{
    [Fact]
    public void Reasons_show_the_words_with_the_visitors_own_accents()
    {
        var b = new MatchBooth(Guid.NewGuid(), "Prodtrack",
            "Prodtrack. Controle de produção e apontamento de ordens na fábrica", null, new HashSet<Guid>());
        var engine = new MatchingEngine([b], new Dictionary<Guid, TagInfo>());
        var r = engine.Score(new MatchVisitor(Guid.NewGuid(), "Quero controle de produção na minha fábrica",
            new HashSet<Guid>(), null, null), b);

        var line = Assert.Single(r.Reasons, s => s.StartsWith("Termos em comum"));
        Assert.Contains("produção", line);
        Assert.Contains("fábrica", line);
        Assert.DoesNotContain("producao", line);
    }
}
