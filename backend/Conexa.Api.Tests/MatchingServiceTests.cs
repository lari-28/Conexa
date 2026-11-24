using Conexa.Api.Services.Matching;

namespace Conexa.Api.Tests;

/// <summary>Regras de privacidade e persistência do matching, com banco em memória.</summary>
public class MatchingServiceTests
{
    private sealed class World
    {
        public ApplicationDbContext Db = new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        public Event Ev = new() { Name = "Feira" };
        public Company Mes = null!, Pdv = null!;

        public static async Task<World> Create()
        {
            var w = new World();
            w.Mes = new Company
            {
                EventId = w.Ev.Id, Name = "Prodtrack", Description = "Controle de produção e apontamento de ordens na fábrica",
                IdealCustomer = "Indústrias de transformação",
            };
            w.Pdv = new Company { EventId = w.Ev.Id, Name = "Caixa Fácil", Description = "Frente de caixa e loja virtual para o varejo" };
            w.Db.AddRange(w.Ev, w.Mes, w.Pdv);
            await w.Db.SaveChangesAsync();
            return w;
        }

        public async Task<ApplicationUser> Visitor(string name, string? goal, bool matching = true, bool share = true,
            bool active = true, bool emailVisible = false, MembershipStatus status = MembershipStatus.Active)
        {
            var u = new ApplicationUser { UserName = name, Email = $"{name}@x.com", DisplayName = name, IsActive = active,
                Profile = new UserProfile { JobTitle = "Gerente", CompanyName = "Acme" },
                Privacy = new ContactPrivacy { EmailVisible = emailVisible, ConsentGivenAt = emailVisible ? DateTimeOffset.UtcNow : null } };
            Db.Add(u);
            Db.Add(new EventMembership { EventId = Ev.Id, UserId = u.Id, Status = status });
            Db.Add(new ParticipantEventProfile
            {
                EventId = Ev.Id, UserId = u.Id, Goal = goal,
                ConsentMatchingAt = matching ? DateTimeOffset.UtcNow : null,
                ConsentShareWithExhibitorsAt = share ? DateTimeOffset.UtcNow : null,
            });
            await Db.SaveChangesAsync();
            return u;
        }
    }

    private const string ProductionGoal = "Preciso controlar a produção e os apontamentos de ordens na minha fábrica";

    [Fact]
    public async Task Booths_are_recommended_and_persisted_with_explanation()
    {
        var w = await World.Create();
        var ana = await w.Visitor("ana", ProductionGoal);
        var (ready, items) = await new MatchingService(w.Db).BoothsForAsync(w.Ev.Id, ana.Id, 10, default);

        Assert.True(ready);
        Assert.Equal(w.Mes.Id, items[0].CompanyId);
        Assert.NotEmpty(items[0].Reasons);
        var row = await w.Db.MatchScores.SingleAsync(m => m.UserId == ana.Id && m.CompanyId == w.Mes.Id);
        Assert.True(row.Score > 0);
        Assert.NotNull(row.TextScore);

        // recalcular atualiza a mesma linha, não duplica
        await new MatchingService(w.Db).BoothsForAsync(w.Ev.Id, ana.Id, 10, default);
        Assert.Equal(1, await w.Db.MatchScores.CountAsync(m => m.UserId == ana.Id && m.CompanyId == w.Mes.Id));
    }

    [Fact]
    public async Task Without_matching_consent_or_profile_nothing_is_computed_or_stored()
    {
        var w = await World.Create();
        var noConsent = await w.Visitor("sem", ProductionGoal, matching: false, share: false);
        var svc = new MatchingService(w.Db);

        Assert.False((await svc.BoothsForAsync(w.Ev.Id, noConsent.Id, 10, default)).Ready);
        Assert.False((await svc.BoothsForAsync(w.Ev.Id, Guid.NewGuid(), 10, default)).Ready);
        Assert.Equal(0, await w.Db.MatchScores.CountAsync());
    }

    [Fact]
    public async Task Leads_require_both_matching_and_share_consent()
    {
        var w = await World.Create();
        await w.Visitor("ok", ProductionGoal);
        await w.Visitor("sem_share", ProductionGoal, share: false);
        await w.Visitor("sem_matching", ProductionGoal, matching: false, share: false);

        var leads = await new MatchingService(w.Db).LeadsForAsync(w.Ev.Id, w.Mes.Id, 20, default);
        Assert.Equal(["ok"], leads.Select(l => l.DisplayName));
    }

    [Fact]
    public async Task Leads_exclude_suspended_users_and_inactive_memberships()
    {
        var w = await World.Create();
        await w.Visitor("ok", ProductionGoal);
        await w.Visitor("suspenso", ProductionGoal, active: false);
        await w.Visitor("removido", ProductionGoal, status: MembershipStatus.Removed);

        var leads = await new MatchingService(w.Db).LeadsForAsync(w.Ev.Id, w.Mes.Id, 20, default);
        Assert.Equal(["ok"], leads.Select(l => l.DisplayName));
    }

    [Fact]
    public async Task Lead_contact_follows_the_privacy_settings_not_the_share_consent()
    {
        var w = await World.Create();
        await w.Visitor("fechado", ProductionGoal, emailVisible: false);
        await w.Visitor("aberto", ProductionGoal, emailVisible: true);

        var leads = (await new MatchingService(w.Db).LeadsForAsync(w.Ev.Id, w.Mes.Id, 20, default))
            .ToDictionary(l => l.DisplayName);
        Assert.Null(leads["fechado"].Email);
        Assert.Equal("aberto@x.com", leads["aberto"].Email);
    }

    [Fact]
    public async Task Leads_for_an_unrelated_booth_or_unknown_booth_are_empty()
    {
        var w = await World.Create();
        await w.Visitor("ana", ProductionGoal);
        var svc = new MatchingService(w.Db);
        Assert.Empty(await svc.LeadsForAsync(w.Ev.Id, w.Pdv.Id, 20, default));
        Assert.Empty(await svc.LeadsForAsync(w.Ev.Id, Guid.NewGuid(), 20, default));
    }

    [Fact]
    public async Task Archived_or_pending_tags_do_not_count_as_booth_themes()
    {
        var w = await World.Create();
        var pending = new InterestTag { EventId = w.Ev.Id, Name = "Produção", Status = TagStatus.Suggested };
        w.Db.Add(pending);
        w.Db.Add(new CompanyTag { CompanyId = w.Mes.Id, TagId = pending.Id });
        var ana = await w.Visitor("ana", null);
        w.Db.Add(new ParticipantTag { EventId = w.Ev.Id, UserId = ana.Id, TagId = pending.Id });
        await w.Db.SaveChangesAsync();

        var (_, items) = await new MatchingService(w.Db).BoothsForAsync(w.Ev.Id, ana.Id, 10, default);
        Assert.Empty(items); // tema pendente não conecta ninguém
    }
}
