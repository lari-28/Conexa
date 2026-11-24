namespace Conexa.Api.Tests;

public class TaxonomyModelTests
{
    [Fact]
    public async Task Taxonomy_and_event_profile_roundtrip()
    {
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        var ev = new Event { Name = "Feira", MainThemes = "Indústria 4.0" };
        var area = new InterestTag { EventId = ev.Id, Name = "Gestão", Status = TagStatus.Approved };
        var theme = new InterestTag
        {
            EventId = ev.Id, ParentId = area.Id, Name = "ERP", Status = TagStatus.Approved,
            Synonyms = ["sistema de gestão", "erp"], Description = "Gestão integrada de empresas",
        };
        var company = new Company { EventId = ev.Id, Name = "Nimbus", IdealCustomer = "Indústrias de médio porte" };
        var userId = Guid.NewGuid();
        db.AddRange(ev, area, theme, company);
        db.Add(new CompanyTag { CompanyId = company.Id, TagId = theme.Id });
        db.Add(new ParticipantEventProfile
        {
            EventId = ev.Id, UserId = userId, Goal = "Automatizar a produção",
            Timeframe = VisitTimeframe.ThisQuarter, ConsentMatchingAt = DateTimeOffset.UtcNow,
            Tags = { new ParticipantTag { EventId = ev.Id, UserId = userId, TagId = theme.Id } },
        });
        await db.SaveChangesAsync();

        var loaded = await db.InterestTags.SingleAsync(t => t.Name == "ERP");
        Assert.Equal(area.Id, loaded.ParentId);
        Assert.Equal(2, loaded.Synonyms.Count);

        var profile = await db.ParticipantEventProfiles.Include(p => p.Tags).SingleAsync();
        Assert.Single(profile.Tags);
        Assert.NotNull(profile.ConsentMatchingAt);
        Assert.Null(profile.ConsentExternalAiAt); // consentimento por finalidade: nada é presumido
        Assert.Equal(1, await db.CompanyTags.CountAsync());
    }

    [Fact]
    public void Company_tag_limit_is_three() => Assert.Equal(3, InterestTag.MaxPerCompany);
}
