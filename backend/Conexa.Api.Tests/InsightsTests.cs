using System.Text;
using Conexa.Api.Controllers;
using Conexa.Api.Services.Insights;
using Conexa.Api.Services.Matching;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Conexa.Api.Tests;

public class InsightMathTests
{
    [Fact]
    public void Gini_is_zero_when_equal_and_high_when_concentrated()
    {
        Assert.Equal(0, InsightMath.Gini([5, 5, 5, 5]));
        Assert.Equal(0, InsightMath.Gini([]));
        Assert.Equal(0, InsightMath.Gini([0, 0, 0]));
        Assert.Equal(0.75, InsightMath.Gini([0, 0, 0, 10]), 4);          // tudo num estande de 4
        Assert.True(InsightMath.Gini([1, 1, 1, 9]) > InsightMath.Gini([3, 3, 3, 3]));
    }

    [Fact]
    public void Top_share_counts_at_least_one_booth()
    {
        Assert.Equal(0.7143, InsightMath.TopShare([10, 2, 1, 1, 0]), 4); // 20% de 5 = 1 estande: 10/14
        Assert.Equal(1.0, InsightMath.TopShare([7]));
        Assert.Equal(0, InsightMath.TopShare([0, 0]));
    }

    [Fact]
    public void Mentions_matches_whole_words_without_accents_and_via_synonyms()
    {
        Assert.True(InsightMath.Mentions("Quero migrar para a NUVEM", "Nuvem", []));
        Assert.True(InsightMath.Mentions("preciso de inteligencia artificial", "IA", ["inteligência artificial"]));
        Assert.False(InsightMath.Mentions("quero um ERP novo", "Erp industrial", []));
        Assert.False(InsightMath.Mentions("materiais de limpeza", "IA", [])); // "ia" dentro de palavra não conta
        Assert.False(InsightMath.Mentions(null, "Nuvem", []));
    }

    [Fact]
    public void Top_terms_count_once_per_visitor_and_ignore_stopwords_and_singletons()
    {
        var terms = InsightMath.TopTerms(["quero automação de produção e produção", "automação para fábrica", "energia solar"]);
        Assert.Equal([("automacao", 2)], terms);
    }

    [Theory]
    [InlineData("=CMD()", "'=CMD()")]
    [InlineData("+1+1", "'+1+1")]
    [InlineData("@soma", "'@soma")]
    [InlineData("normal", "normal")]
    [InlineData("a;b", "\"a;b\"")]
    [InlineData("diz \"oi\"", "\"diz \"\"oi\"\"\"")]
    public void Csv_cells_are_escaped_and_formula_safe(string input, string expected) =>
        Assert.Equal(expected, CsvWriter.Cell(input));

    [Fact]
    public void Csv_has_bom_semicolons_and_crlf()
    {
        var csv = CsvWriter.Write(["A", "B"], [new object?[] { 1, true }, new object?[] { null, 0.5 }]);
        Assert.StartsWith("﻿A;B\r\n", csv);
        Assert.Contains("1;sim\r\n", csv);
        Assert.EndsWith(";0,5\r\n", csv);
    }
}

public class InsightsServiceTests
{
    private sealed class World
    {
        public ApplicationDbContext Db = new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        public Event Ev = new() { Name = "Feira" };
        public InterestTag Cloud = null!, Solar = null!;
        public Company A = null!, B = null!;

        public static async Task<World> Create()
        {
            var w = new World();
            w.Cloud = new InterestTag { EventId = w.Ev.Id, Name = "Nuvem", Status = TagStatus.Approved, Synonyms = ["cloud"] };
            w.Solar = new InterestTag { EventId = w.Ev.Id, Name = "Energia solar", Status = TagStatus.Approved };
            w.A = new Company { EventId = w.Ev.Id, Name = "Nimbus" };
            w.B = new Company { EventId = w.Ev.Id, Name = "Verde" };
            w.Db.AddRange(w.Ev, w.Cloud, w.Solar, w.A, w.B);
            w.Db.CompanyTags.Add(new CompanyTag { CompanyId = w.A.Id, TagId = w.Cloud.Id }); // só nuvem tem oferta
            await w.Db.SaveChangesAsync();
            return w;
        }

        public async Task<ApplicationUser> Visitor(string name, string goal, string? area, bool matching = true, bool share = false, params InterestTag[] tags)
        {
            var u = new ApplicationUser { UserName = name, Email = $"{name}@x.com", DisplayName = name, Profile = new UserProfile { Area = area } };
            Db.Add(u);
            await Db.SaveChangesAsync(); // Identity gera o Id ao salvar
            var p = new ParticipantEventProfile { EventId = Ev.Id, UserId = u.Id, Goal = goal,
                ConsentMatchingAt = matching ? DateTimeOffset.UtcNow : null, ConsentShareWithExhibitorsAt = share ? DateTimeOffset.UtcNow : null };
            foreach (var t in tags) p.Tags.Add(new ParticipantTag { EventId = Ev.Id, UserId = u.Id, TagId = t.Id });
            Db.Add(p);
            Db.Add(new EventMembership { EventId = Ev.Id, UserId = u.Id });
            await Db.SaveChangesAsync();
            return u;
        }
    }

    [Fact]
    public async Task Gaps_are_themes_with_demand_and_no_supply_counting_chips_and_goal_mentions()
    {
        var w = await World.Create();
        await w.Visitor("v1", "quero reduzir a conta com energia solar", "Indústria");  // menciona
        await w.Visitor("v2", "algo", "Indústria", tags: w.Solar);                       // escolheu o chip
        await w.Visitor("v3", "migrar para cloud", "Tecnologia");                        // sinônimo
        await w.Visitor("v4", "energia solar", "Indústria", matching: false);            // sem consentimento: não conta

        var r = await new InsightsService(w.Db).EventAsync(w.Ev.Id, default);
        var solar = r.Themes.Single(t => t.Name == "Energia solar");
        Assert.Equal((2, 0), (solar.Demand, solar.Supply));
        var cloud = r.Themes.Single(t => t.Name == "Nuvem");
        Assert.Equal((1, 1), (cloud.Demand, cloud.Supply));
        Assert.Equal(["Energia solar"], r.Gaps.Select(g => g.Name));
        Assert.Equal(4, r.Totals.VisitorsWithProfile);
        Assert.Equal(3, r.Totals.ConsentedMatching);
    }

    [Fact]
    public async Task Exposure_concentration_and_notes_reflect_the_data()
    {
        var w = await World.Create();
        var v = await w.Visitor("v1", "nuvem", "TI");
        w.Db.MatchScores.Add(new MatchScore { EventId = w.Ev.Id, UserId = v.Id, CompanyId = w.A.Id, Band = "High", Score = 0.6 });
        await w.Db.SaveChangesAsync();

        var r = await new InsightsService(w.Db).EventAsync(w.Ev.Id, default);
        Assert.Equal(1, r.Exposure.BoothsRecommended);
        Assert.Equal(1, r.Exposure.BoothsNeverRecommended);
        Assert.Equal("Nimbus", r.Exposure.MostRecommended);
        Assert.Equal(1.0, r.Exposure.Top20Share);
        Assert.Contains(r.Notes, n => n.Contains("Poucos visitantes"));
        Assert.Contains(r.Notes, n => n.Contains("check-in"));
    }

    [Fact]
    public async Task Profile_groups_by_area_and_totals_include_checkins_and_feedback()
    {
        var w = await World.Create();
        var a = await w.Visitor("a", "x", "Indústria", tags: w.Cloud);
        await w.Visitor("b", "y", "Indústria");
        var c = await w.Visitor("c", "z", null);
        w.Db.Interactions.AddRange(
            new Interaction { EventId = w.Ev.Id, UserId = a.Id, CompanyId = w.A.Id, Type = InteractionType.CheckIn },
            new Interaction { EventId = w.Ev.Id, UserId = a.Id, CompanyId = w.B.Id, Type = InteractionType.CheckIn },
            new Interaction { EventId = w.Ev.Id, UserId = c.Id, CompanyId = w.A.Id, Type = InteractionType.FeedbackUseful },
            new Interaction { EventId = w.Ev.Id, UserId = a.Id, CompanyId = w.A.Id, Type = InteractionType.FeedbackNotUseful },
            new Interaction { EventId = w.Ev.Id, UserId = c.Id, CompanyId = w.B.Id, Type = InteractionType.FeedbackUseful });
        await w.Db.SaveChangesAsync();

        var r = await new InsightsService(w.Db).EventAsync(w.Ev.Id, default);
        Assert.Equal(("Indústria", 2), (r.ProfileGroups[0].Area, r.ProfileGroups[0].Visitors));
        Assert.Equal(["Nuvem"], r.ProfileGroups[0].TopThemes);
        Assert.Contains(r.ProfileGroups, g => g.Area == "Não informado");
        Assert.Equal((2, 1), (r.Totals.CheckIns, r.Totals.CheckedInVisitors));
        Assert.Equal(0.6667, r.Totals.UsefulRate!.Value, 4);
    }

    [Fact]
    public async Task Booth_metrics_count_only_sharing_visitors_as_available_leads()
    {
        var w = await World.Create();
        var shares = await w.Visitor("s", "nuvem", null, share: true);
        var keeps = await w.Visitor("k", "nuvem", null, share: false);
        w.Db.MatchScores.AddRange(
            new MatchScore { EventId = w.Ev.Id, UserId = shares.Id, CompanyId = w.A.Id, Band = "High" },
            new MatchScore { EventId = w.Ev.Id, UserId = keeps.Id, CompanyId = w.A.Id, Band = "Medium" });
        await w.Db.SaveChangesAsync();

        var m = (await new InsightsService(w.Db).BoothAsync(w.Ev.Id, w.A.Id, default))!;
        Assert.Equal((2, 1, 1), (m.RecommendedTo, m.HighAffinity, m.LeadsAvailable));
        Assert.Null(await new InsightsService(w.Db).BoothAsync(w.Ev.Id, Guid.NewGuid(), default));
    }

    // ----- permissões e CSV do controller -----

    private sealed class FakeAccess(bool organizer) : IEventAccessService
    {
        public Task<bool> IsMemberAsync(Guid e, ClaimsPrincipal u, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> IsOrganizerAsync(Guid e, ClaimsPrincipal u, CancellationToken ct = default) => Task.FromResult(organizer);
    }

    private static InsightsController Ctl(World w, Guid uid, bool organizer) =>
        new(w.Db, new InsightsService(w.Db), new MatchingService(w.Db), new FakeAccess(organizer))
        { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext
            { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, uid.ToString())], "t")) } } };

    [Fact]
    public async Task Booth_report_and_leads_csv_are_limited_to_organizer_or_representative()
    {
        var w = await World.Create();
        var rep = Guid.NewGuid();
        w.Db.CompanyMemberships.Add(new CompanyMembership { CompanyId = w.A.Id, UserId = rep });
        await w.Db.SaveChangesAsync();

        Assert.IsType<ForbidResult>((await Ctl(w, Guid.NewGuid(), false).BoothReport(w.Ev.Id, w.A.Id, default)).Result);
        Assert.NotNull((await Ctl(w, rep, false).BoothReport(w.Ev.Id, w.A.Id, default)).Value);
        Assert.IsType<ForbidResult>(await Ctl(w, rep, false).ExportLeads(w.Ev.Id, w.B.Id, default));   // outro estande
        Assert.IsType<FileContentResult>(await Ctl(w, Guid.NewGuid(), true).ExportLeads(w.Ev.Id, w.B.Id, default));
    }

    [Fact]
    public async Task Leads_csv_contains_only_consenting_visitors_and_neutralises_formulas()
    {
        var w = await World.Create();
        w.A.Description = "Nuvem e migração de sistemas para nuvem com segurança";
        await w.Visitor("=HYPERLINK(x)", "migrar sistemas para a nuvem com segurança", null, share: true);
        await w.Visitor("fechado", "migrar sistemas para a nuvem com segurança", null, share: false);
        await w.Db.SaveChangesAsync();

        var file = Assert.IsType<FileContentResult>(await Ctl(w, Guid.NewGuid(), true).ExportLeads(w.Ev.Id, w.A.Id, default));
        var text = Encoding.UTF8.GetString(file.FileContents);
        Assert.Contains("'=HYPERLINK(x)", text);
        Assert.DoesNotContain("fechado", text);
        Assert.Equal("leads.csv", file.FileDownloadName);
    }

    [Fact]
    public async Task Themes_csv_marks_gaps()
    {
        var w = await World.Create();
        await w.Visitor("v", "energia solar", null);
        var file = Assert.IsType<FileContentResult>(await Ctl(w, Guid.NewGuid(), true).ExportThemes(w.Ev.Id, default));
        var text = Encoding.UTF8.GetString(file.FileContents);
        Assert.Contains("Energia solar;;1;0;sim", text);
    }
}
