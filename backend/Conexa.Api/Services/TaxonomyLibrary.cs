namespace Conexa.Api.Services;

public record LibraryTheme(string Key, string Name, string Description, string[] Synonyms);
public record LibraryArea(string Key, string Name, LibraryTheme[] Themes);
public record LibrarySectorDef(string Key, string Name, LibraryArea[] Areas);

/// <summary>
/// Biblioteca-base por setor: ponto de partida para o organizador, que aprova e ajusta.
/// Não precisa ser exaustiva (no teste com catálogo fantasia, cerca de 30% dos estandes ficou sem tema da biblioteca):
/// as sugestões de visitantes e expositores completam a taxonomia pela fila de aprovação.
/// </summary>
public static class TaxonomyLibrary
{
    public static readonly IReadOnlyList<LibrarySectorDef> Sectors =
    [
        new("industria", "Indústria e tecnologia industrial",
        [
            new("ind.gestao", "Gestão e sistemas",
            [
                new("ind.gestao.erp", "ERP e gestão integrada", "Sistemas que integram finanças, estoque, compras e produção.", ["erp", "sistema de gestão", "gestão integrada"]),
                new("ind.gestao.mes", "Controle de produção (MES)", "Acompanhamento da produção no chão de fábrica, ordens e apontamentos.", ["mes", "apontamento de produção", "controle de produção"]),
            ]),
            new("ind.automacao", "Automação e IoT",
            [
                new("ind.automacao.iot", "IoT industrial", "Sensores e dispositivos conectados que coletam dados de máquinas.", ["iot", "sensores", "internet das coisas", "monitoramento de máquinas"]),
                new("ind.automacao.robotica", "Robótica e automação", "Robôs, CLPs e automação de linhas de produção.", ["robôs", "clp", "automação industrial"]),
            ]),
            new("ind.insumos", "Máquinas e insumos",
            [
                new("ind.insumos.usinagem", "Usinagem e metalmecânica", "Máquinas, ferramentas e serviços de corte e conformação de metais.", ["cnc", "torno", "ferramentaria"]),
                new("ind.insumos.epi", "Segurança e EPIs", "Equipamentos de proteção individual e segurança do trabalho.", ["epi", "segurança do trabalho"]),
            ]),
        ]),
        new("tecnologia", "Tecnologia e negócios digitais",
        [
            new("tec.dados", "Dados e IA",
            [
                new("tec.dados.ia", "Inteligência artificial", "Modelos e soluções de IA aplicados a negócios.", ["ia", "machine learning", "aprendizado de máquina"]),
                new("tec.dados.bi", "Análise de dados e BI", "Painéis, relatórios e análise de dados para decisão.", ["bi", "business intelligence", "dashboards"]),
            ]),
            new("tec.infra", "Infraestrutura e segurança",
            [
                new("tec.infra.cloud", "Nuvem", "Hospedagem, migração e operação de sistemas em nuvem.", ["cloud", "computação em nuvem"]),
                new("tec.infra.ciber", "Cibersegurança", "Proteção de sistemas, dados e redes.", ["segurança da informação", "proteção de dados"]),
            ]),
        ]),
        new("varejo", "Varejo e serviços",
        [
            new("var.vendas", "Vendas e pagamentos",
            [
                new("var.vendas.pdv", "Ponto de venda e e-commerce", "Sistemas de caixa, lojas virtuais e omnichannel.", ["pdv", "loja virtual", "e-commerce"]),
                new("var.vendas.pagamentos", "Meios de pagamento", "Adquirência, maquininhas, Pix e crédito para empresas.", ["pix", "maquininha", "crédito b2b"]),
            ]),
        ]),
    ];

    public static LibrarySectorDef? Find(string key) =>
        Sectors.FirstOrDefault(s => string.Equals(s.Key, key, StringComparison.OrdinalIgnoreCase));
}
