using Conexa.Api.Services.Matching;

namespace Conexa.Api.Tests;

public class MatchingEngineTests
{
    // Catálogo fantasia mínimo, escrito sem olhar para os visitantes dos testes.
    private static readonly Guid AreaGestao = Guid.NewGuid(), AreaAuto = Guid.NewGuid(), AreaCom = Guid.NewGuid();
    private static readonly Guid Erp = Guid.NewGuid(), Mes = Guid.NewGuid(), Iot = Guid.NewGuid(), Rob = Guid.NewGuid(), Pdv = Guid.NewGuid();

    private static readonly Dictionary<Guid, TagInfo> Tags = new()
    {
        [AreaGestao] = new("Gestão", null), [AreaAuto] = new("Automação", null), [AreaCom] = new("Varejo", null),
        [Erp] = new("ERP", AreaGestao), [Mes] = new("Controle de produção", AreaGestao),
        [Iot] = new("IoT industrial", AreaAuto), [Rob] = new("Robótica", AreaAuto), [Pdv] = new("Ponto de venda", AreaCom),
    };

    private static MatchBooth Booth(string name, string text, string? aud, params Guid[] tags) =>
        new(Guid.NewGuid(), name, $"{name}. {text}", aud, tags.ToHashSet());

    private static readonly MatchBooth BMes = Booth("Prodtrack",
        "Sistema de controle de produção e apontamento de ordens no chão de fábrica, integrado ao ERP.",
        "Indústrias de transformação com gerente de produção", Mes, Erp);
    private static readonly MatchBooth BIot = Booth("Sensa",
        "Sensores e monitoramento de máquinas em tempo real com internet das coisas.",
        "Fábricas que querem reduzir paradas de máquina", Iot);
    private static readonly MatchBooth BPdv = Booth("Caixa Fácil",
        "Frente de caixa, loja virtual e pagamentos para o comércio varejista.",
        "Lojistas e donos de comércio", Pdv);
    private static readonly MatchBooth BRob = Booth("Robô Max",
        "Robôs colaborativos e automação de linhas de montagem.", "Indústrias metalmecânicas", Rob);

    private static MatchingEngine Engine(MatchOptions? o = null) => new([BMes, BIot, BPdv, BRob], Tags, o);

    private static MatchVisitor Visitor(string? goal, string? job = null, string? area = null, params Guid[] tags) =>
        new(Guid.NewGuid(), goal, tags.ToHashSet(), job, area);

    [Fact]
    public void Free_text_alone_ranks_the_relevant_booth_first()
    {
        var v = Visitor("Quero controlar a produção e os apontamentos da minha fábrica");
        var top = Engine().RankBoothsFor(v);
        Assert.NotEmpty(top);
        Assert.Equal(BMes.CompanyId, top[0].CompanyId);
        Assert.Null(top[0].Theme); // sem chips, o sinal de tema sai
        Assert.NotNull(top[0].Text);
    }

    [Fact]
    public void Chips_alone_work_and_explain_the_shared_themes()
    {
        var v = Visitor(null, tags: [Iot]);
        var top = Engine().RankBoothsFor(v);
        Assert.Equal(BIot.CompanyId, top[0].CompanyId);
        Assert.Equal(1.0, top[0].Theme);
        Assert.Contains(top[0].Reasons, r => r.Contains("IoT industrial"));
    }

    [Fact]
    public void Sibling_theme_gets_partial_credit_so_related_areas_still_connect()
    {
        // visitante escolheu ERP; "Controle de produção" é da mesma macro-área (Gestão), mas o estande de robótica não é
        var v = Visitor(null, tags: [Erp]);
        var results = Engine().RankBoothsFor(v);
        var mes = results.Single(r => r.CompanyId == BMes.CompanyId);
        Assert.Equal(1.0, mes.Theme);
        Assert.DoesNotContain(results, r => r.CompanyId == BRob.CompanyId);

        var viaSibling = Engine().Score(Visitor(null, tags: [Mes]), new MatchBooth(Guid.NewGuid(), "X", "x", null, new HashSet<Guid> { Erp }));
        Assert.Equal(MatchOptions.Default.SiblingThemeCredit, viaSibling.Theme);
    }

    [Fact]
    public void Text_beats_wrong_chips_because_it_carries_more_weight()
    {
        // chip errado (varejo) com objetivo claro de produção: o texto deve manter o estande certo em 1º
        var v = Visitor("Preciso de um sistema para controle de produção na fábrica", tags: [Pdv]);
        var top = Engine().RankBoothsFor(v);
        Assert.Equal(BMes.CompanyId, top[0].CompanyId);
    }

    [Fact]
    public void Visitor_without_goal_or_tags_gets_no_recommendations()
    {
        Assert.Empty(Engine().RankBoothsFor(Visitor(null, job: "Gerente", area: "Operações")));
        Assert.Equal(MatchBand.None, Engine().Score(Visitor(null), BMes).Band);
    }

    [Fact]
    public void Unrelated_goal_stays_below_the_no_match_threshold_for_every_booth()
    {
        var v = Visitor("Quero comprar embalagens biodegradáveis para alimentos congelados");
        var results = new[] { BMes, BIot, BPdv, BRob }.Select(b => Engine().Score(v, b)).ToList();
        Assert.All(results, r => Assert.Equal(MatchBand.None, r.Band));
    }

    [Fact]
    public void Audience_signal_uses_job_and_area_against_ideal_customer()
    {
        var v = Visitor("controle de produção", job: "Gerente de produção", area: "Indústria de transformação");
        var r = Engine().Score(v, BMes);
        Assert.NotNull(r.Audience);
        Assert.True(r.Audience > 0.2);
        Assert.Contains(r.Reasons, s => s.Contains("perfil"));
    }

    [Fact]
    public void Missing_signals_are_renormalised_not_counted_as_zero()
    {
        // um estande sem temas e sem cliente ideal ainda pode ter score alto só pelo texto
        var b = Booth("Solo", "controle de produção e apontamento de ordens", null);
        var e = new MatchingEngine([b, BIot], Tags);
        var r = e.Score(Visitor("controle de produção e apontamento", tags: [Mes]), b);
        Assert.Null(r.Theme);
        Assert.Null(r.Audience);
        Assert.Equal(r.Text!.Value, r.Score, 6);
    }

    [Fact]
    public void Score_is_bounded_and_accent_and_case_insensitive()
    {
        var a = Engine().Score(Visitor("CONTROLE DE PRODUÇÃO"), BMes);
        var b = Engine().Score(Visitor("controle de producao"), BMes);
        Assert.InRange(a.Score, 0, 1);
        Assert.Equal(a.Score, b.Score, 9);
    }

    [Fact]
    public void Rank_visitors_for_booth_returns_leads_best_first_and_drops_no_match()
    {
        var good = Visitor("Preciso controlar a produção e apontar ordens na fábrica");
        var meh = Visitor("quero ver sistemas de produção");
        var none = Visitor("embalagens biodegradáveis para alimentos");
        var leads = Engine().RankVisitorsFor(BMes, [none, meh, good]);
        Assert.Equal(good.UserId, leads[0].UserId);
        Assert.DoesNotContain(leads, l => l.UserId == none.UserId);
    }

    [Fact]
    public void Ranking_is_deterministic_and_respects_the_limit()
    {
        var v = Visitor("automação industrial de máquinas e produção");
        var e = Engine();
        Assert.Equal(e.RankBoothsFor(v).Select(r => r.CompanyId), e.RankBoothsFor(v).Select(r => r.CompanyId));
        Assert.True(e.RankBoothsFor(v, 1).Count <= 1);
    }

    [Fact]
    public void Custom_weights_change_the_balance()
    {
        var v = Visitor("controle de produção", tags: [Pdv]);
        var textHeavy = new MatchingEngine([BMes, BPdv], Tags, new MatchOptions(0.05, 0.9, 0.05)).Score(v, BMes).Score;
        var themeHeavy = new MatchingEngine([BMes, BPdv], Tags, new MatchOptions(0.9, 0.05, 0.05)).Score(v, BMes).Score;
        Assert.True(textHeavy > themeHeavy);
    }
}
