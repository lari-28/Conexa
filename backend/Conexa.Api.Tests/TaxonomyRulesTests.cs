namespace Conexa.Api.Tests;

public class TaxonomyRulesTests
{
    private static readonly Guid Ev = Guid.NewGuid();

    private static InterestTag Tag(string name, TagStatus status = TagStatus.Approved, Guid? parent = null, Guid? ev = null,
        params string[] synonyms) =>
        new() { EventId = ev ?? Ev, Name = name, Status = status, ParentId = parent, Synonyms = synonyms.ToList() };

    [Theory]
    [InlineData("Inteligência  Artificial", "inteligencia artificial")]
    [InlineData("  IoT ", "iot")]
    [InlineData("Gestão", "gestao")]
    [InlineData(null, "")]
    public void Key_ignores_case_accents_and_extra_spaces(string? input, string expected) =>
        Assert.Equal(expected, TaxonomyRules.Key(input));

    [Fact]
    public void Synonyms_drop_duplicates_empty_long_and_the_name_itself()
    {
        var result = TaxonomyRules.NormalizeSynonyms(
            ["IA", " ia ", "Inteligência Artificial", "", new string('x', 61), "Machine  Learning"], "inteligencia artificial");
        Assert.Equal(["IA", "Machine Learning"], result);
    }

    [Fact]
    public void Synonyms_are_capped()
    {
        var many = Enumerable.Range(0, 40).Select(i => $"termo {i}");
        Assert.Equal(TaxonomyRules.MaxSynonyms, TaxonomyRules.NormalizeSynonyms(many, "x").Count);
    }

    [Fact]
    public void Parent_must_be_an_approved_root_of_same_event()
    {
        var root = Tag("Gestão");
        Assert.Null(TaxonomyRules.ValidateParent(root, Ev, null));
        Assert.NotNull(TaxonomyRules.ValidateParent(null, Ev, null));
        Assert.NotNull(TaxonomyRules.ValidateParent(Tag("Outro", ev: Guid.NewGuid()), Ev, null));
        Assert.NotNull(TaxonomyRules.ValidateParent(Tag("Filho", parent: root.Id), Ev, null)); // 3º nível
        Assert.NotNull(TaxonomyRules.ValidateParent(Tag("Rej", TagStatus.Rejected), Ev, null));
        Assert.NotNull(TaxonomyRules.ValidateParent(root, Ev, root.Id)); // pai de si mesmo
    }

    [Fact]
    public void Company_tags_allow_up_to_three_approved_tags_of_the_event()
    {
        var a = Tag("A"); var b = Tag("B"); var c = Tag("C"); var d = Tag("D");
        Assert.Null(TaxonomyRules.ValidateCompanyTags([a.Id, b.Id, c.Id], [a, b, c], Ev));
        Assert.NotNull(TaxonomyRules.ValidateCompanyTags([a.Id, b.Id, c.Id, d.Id], [a, b, c, d], Ev));
        Assert.Null(TaxonomyRules.ValidateCompanyTags([a.Id, a.Id], [a], Ev)); // repetido conta uma vez
        Assert.NotNull(TaxonomyRules.ValidateCompanyTags([a.Id, Guid.NewGuid()], [a], Ev)); // inexistente
        Assert.NotNull(TaxonomyRules.ValidateCompanyTags([a.Id], [Tag("Pend", TagStatus.Suggested)], Ev));
        Assert.NotNull(TaxonomyRules.ValidateCompanyTags([a.Id], [Tag("X", ev: Guid.NewGuid())], Ev));
    }

    [Fact]
    public void Suggestions_with_same_key_are_grouped_and_matches_are_flagged()
    {
        var iot = Tag("IoT industrial", synonyms: ["sensores"]);
        var groups = TaxonomyRules.GroupSuggestions(
        [
            Tag("Robótica", TagStatus.Suggested), Tag("robotica", TagStatus.Suggested),
            Tag("Sensores", TagStatus.Suggested), Tag("Logística", TagStatus.Suggested),
        ], [iot]);

        Assert.Equal(3, groups.Count);
        var robotica = Assert.Single(groups, g => g.Key == "robotica");
        Assert.Equal(2, robotica.Items.Count);
        Assert.Null(robotica.MatchesTagId);

        var sensores = Assert.Single(groups, g => g.Key == "sensores");
        Assert.Equal(iot.Id, sensores.MatchesTagId); // bate com sinônimo de tema aprovado → sugerir fusão
        Assert.Equal("IoT industrial", sensores.MatchesTagName);
    }

    [Fact]
    public void Library_has_unique_keys_and_every_theme_has_description_and_synonyms()
    {
        var keys = TaxonomyLibrary.Sectors.SelectMany(s => s.Areas)
            .SelectMany(a => new[] { a.Key }.Concat(a.Themes.Select(t => t.Key))).ToList();
        Assert.Equal(keys.Count, keys.Distinct().Count());
        foreach (var t in TaxonomyLibrary.Sectors.SelectMany(s => s.Areas).SelectMany(a => a.Themes))
        {
            Assert.False(string.IsNullOrWhiteSpace(t.Description), t.Key);
            Assert.NotEmpty(t.Synonyms);
        }
        Assert.NotNull(TaxonomyLibrary.Find("INDUSTRIA"));
        Assert.Null(TaxonomyLibrary.Find("inexistente"));
    }
}
