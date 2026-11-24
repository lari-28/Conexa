using Conexa.Api.Controllers;
using Conexa.Api.Services.Matching;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Conexa.Api.Tests;

public class EngagementTests
{
    private sealed class FakeAccess(bool organizer) : IEventAccessService
    {
        public Task<bool> IsMemberAsync(Guid e, ClaimsPrincipal u, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> IsOrganizerAsync(Guid e, ClaimsPrincipal u, CancellationToken ct = default) => Task.FromResult(organizer);
    }

    private static ControllerContext Ctx(Guid uid) => new() { HttpContext = new DefaultHttpContext
    { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, uid.ToString())], "t")) } };

    private static EngagementController Eng(ApplicationDbContext db, Guid uid, bool organizer = false) =>
        new(db, new FakeAccess(organizer)) { ControllerContext = Ctx(uid) };

    private sealed class World
    {
        public ApplicationDbContext Db = new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        public Event Ev = new() { Name = "Feira" };
        public Company Mes = null!;
        public ApplicationUser Rep = null!, Visitor = null!;
        public InterestTag Tag = null!;

        public static async Task<World> Create(bool visitorShares = true)
        {
            var w = new World();
            w.Tag = new InterestTag { EventId = w.Ev.Id, Name = "Controle de produção", Status = TagStatus.Approved, Synonyms = ["mes"] };
            w.Mes = new Company
            {
                EventId = w.Ev.Id, Name = "Prodtrack",
                Description = "Controle de produção e apontamento de ordens no chão de fábrica",
                ProductsServices = "Sistema de controle de produção para indústrias", IdealCustomer = "Indústrias com gerente de produção",
            };
            w.Rep = new ApplicationUser { UserName = "rep", Email = "rep@x.com", DisplayName = "Carla" };
            w.Visitor = new ApplicationUser { UserName = "v", Email = "v@x.com", DisplayName = "Diego Rocha",
                Profile = new UserProfile { JobTitle = "Gerente de produção" }, Privacy = new ContactPrivacy() };
            w.Db.AddRange(w.Ev, w.Tag, w.Mes, w.Rep, w.Visitor);
            w.Db.CompanyTags.Add(new CompanyTag { CompanyId = w.Mes.Id, TagId = w.Tag.Id });
            w.Db.CompanyMemberships.Add(new CompanyMembership { CompanyId = w.Mes.Id, UserId = w.Rep.Id });
            w.Db.EventMemberships.AddRange(
                new EventMembership { EventId = w.Ev.Id, UserId = w.Rep.Id, RoleInEvent = EventRole.Exhibitor },
                new EventMembership { EventId = w.Ev.Id, UserId = w.Visitor.Id });
            await w.Db.SaveChangesAsync();
            w.SetProfile(visitorShares);
            await w.Db.SaveChangesAsync();
            return w;
        }

        public void SetProfile(bool share)
        {
            var p = Db.ParticipantEventProfiles.Include(x => x.Tags).FirstOrDefault(x => x.EventId == Ev.Id && x.UserId == Visitor.Id);
            if (p is null)
            {
                p = new ParticipantEventProfile { EventId = Ev.Id, UserId = Visitor.Id };
                Db.ParticipantEventProfiles.Add(p);
                p.Tags.Add(new ParticipantTag { EventId = Ev.Id, UserId = Visitor.Id, TagId = Tag.Id });
            }
            p.Goal = "Quero um sistema de controle de produção e apontamento de ordens para a minha fábrica";
            p.ConsentMatchingAt = DateTimeOffset.UtcNow;
            p.ConsentShareWithExhibitorsAt = share ? DateTimeOffset.UtcNow : null;
        }

        public MatchNotifier Notifier() => new(Db, new MatchingService(Db));
    }

    // ----- check-in -----

    [Fact]
    public async Task Representative_gets_a_stable_code_and_strangers_are_refused()
    {
        var w = await World.Create();
        var a = (await Eng(w.Db, w.Rep.Id).Code(w.Ev.Id, w.Mes.Id, default)).Value!;
        var b = (await Eng(w.Db, w.Rep.Id).Code(w.Ev.Id, w.Mes.Id, default)).Value!;
        Assert.Equal(a.Code, b.Code);
        Assert.IsType<ForbidResult>((await Eng(w.Db, w.Visitor.Id).Code(w.Ev.Id, w.Mes.Id, default)).Result);
        Assert.NotNull((await Eng(w.Db, Guid.NewGuid(), organizer: true).Code(w.Ev.Id, w.Mes.Id, default)).Value);
    }

    [Fact]
    public async Task Checkin_records_once_within_cooldown_and_rejects_unknown_codes()
    {
        var w = await World.Create();
        var code = (await Eng(w.Db, w.Rep.Id).Code(w.Ev.Id, w.Mes.Id, default)).Value!.Code;

        var first = (await Eng(w.Db, w.Visitor.Id).CheckIn(w.Ev.Id, new CheckInRequest(code), default)).Value!;
        Assert.False(first.AlreadyRecorded);
        Assert.Equal("Prodtrack", first.CompanyName);
        var again = (await Eng(w.Db, w.Visitor.Id).CheckIn(w.Ev.Id, new CheckInRequest(code), default)).Value!;
        Assert.True(again.AlreadyRecorded);
        Assert.Equal(1, await w.Db.Interactions.CountAsync(i => i.Type == InteractionType.CheckIn));

        // passado o intervalo, vale de novo
        var row = await w.Db.Interactions.SingleAsync();
        row.CreatedAt = DateTimeOffset.UtcNow - EngagementController.CheckInCooldown - TimeSpan.FromMinutes(1);
        await w.Db.SaveChangesAsync();
        Assert.False((await Eng(w.Db, w.Visitor.Id).CheckIn(w.Ev.Id, new CheckInRequest(code), default)).Value!.AlreadyRecorded);

        Assert.Equal(404, ((ObjectResult)(await Eng(w.Db, w.Visitor.Id).CheckIn(w.Ev.Id, new CheckInRequest("nao-existe"), default)).Result!).StatusCode);
        Assert.Equal(404, ((ObjectResult)(await Eng(w.Db, w.Visitor.Id).CheckIn(Guid.NewGuid(), new CheckInRequest(code), default)).Result!).StatusCode); // outro evento
    }

    // ----- feedback -----

    [Fact]
    public async Task Feedback_only_for_recommended_booths_and_last_answer_wins()
    {
        var w = await World.Create();
        var eng = Eng(w.Db, w.Visitor.Id);
        Assert.IsType<ObjectResult>(await eng.Feedback(w.Ev.Id, new FeedbackRequest(w.Mes.Id, true), default)); // ainda não recomendado

        await new MatchingService(w.Db).BoothsForAsync(w.Ev.Id, w.Visitor.Id, 10, default);
        Assert.IsType<NoContentResult>(await eng.Feedback(w.Ev.Id, new FeedbackRequest(w.Mes.Id, true), default));
        Assert.IsType<NoContentResult>(await eng.Feedback(w.Ev.Id, new FeedbackRequest(w.Mes.Id, true), default)); // repetido: não duplica
        Assert.Equal(1, await w.Db.Interactions.CountAsync(i => i.Type == InteractionType.FeedbackUseful));

        await eng.Feedback(w.Ev.Id, new FeedbackRequest(w.Mes.Id, false), default); // mudou de ideia
        Assert.Equal(0, await w.Db.Interactions.CountAsync(i => i.Type == InteractionType.FeedbackUseful));
        Assert.Equal(1, await w.Db.Interactions.CountAsync(i => i.Type == InteractionType.FeedbackNotUseful));
    }

    // ----- avisos -----

    [Fact]
    public async Task Sharing_visitor_notifies_himself_once_and_the_representative_of_a_high_match()
    {
        var w = await World.Create(visitorShares: true);
        var n = w.Notifier();
        await n.AfterProfileSavedAsync(w.Ev.Id, w.Visitor.Id, default);
        await n.AfterProfileSavedAsync(w.Ev.Id, w.Visitor.Id, default); // salvar de novo não repete

        var mine = await w.Db.Notifications.Where(x => x.UserId == w.Visitor.Id).ToListAsync();
        Assert.Single(mine);
        var rep = await w.Db.Notifications.Where(x => x.UserId == w.Rep.Id).ToListAsync();
        var lead = Assert.Single(rep);
        Assert.Contains("Diego Rocha", lead.Body);
        Assert.Equal(MatchNotifier.LeadKey(w.Ev.Id, w.Mes.Id, w.Visitor.Id), lead.DedupeKey);
    }

    [Fact]
    public async Task Visitor_who_did_not_consent_sharing_never_appears_in_exhibitor_notices()
    {
        var w = await World.Create(visitorShares: false);
        await w.Notifier().AfterProfileSavedAsync(w.Ev.Id, w.Visitor.Id, default);
        Assert.Single(await w.Db.Notifications.Where(x => x.UserId == w.Visitor.Id).ToListAsync());
        Assert.Empty(await w.Db.Notifications.Where(x => x.UserId == w.Rep.Id).ToListAsync());
    }

    [Fact]
    public async Task Revoking_sharing_erases_the_lead_notices_with_the_visitors_name()
    {
        var w = await World.Create(visitorShares: true);
        await w.Notifier().AfterProfileSavedAsync(w.Ev.Id, w.Visitor.Id, default);
        Assert.Single(await w.Db.Notifications.Where(x => x.UserId == w.Rep.Id).ToListAsync());

        w.SetProfile(share: false);
        await w.Db.SaveChangesAsync();
        await w.Notifier().AfterProfileSavedAsync(w.Ev.Id, w.Visitor.Id, default);
        Assert.Empty(await w.Db.Notifications.Where(x => x.UserId == w.Rep.Id).ToListAsync());
    }

    [Fact]
    public async Task Notifications_are_private_countable_and_markable_as_read()
    {
        var w = await World.Create();
        await w.Notifier().AfterProfileSavedAsync(w.Ev.Id, w.Visitor.Id, default);
        NotificationsController Ctl(Guid id) => new(w.Db) { ControllerContext = Ctx(id) };

        Assert.Equal(1, (await Ctl(w.Rep.Id).Count(default)).Value);
        Assert.Equal(0, (await Ctl(Guid.NewGuid()).Count(default)).Value);
        var item = (await Ctl(w.Rep.Id).List(false, 30, default)).Value!.Single();

        Assert.IsType<NotFoundResult>(await Ctl(w.Visitor.Id).Read(item.Id, default)); // não é dele
        Assert.IsType<NoContentResult>(await Ctl(w.Rep.Id).Read(item.Id, default));
        Assert.Equal(0, (await Ctl(w.Rep.Id).Count(default)).Value);
        Assert.Empty((await Ctl(w.Rep.Id).List(true, 30, default)).Value!);

        await Ctl(w.Visitor.Id).ReadAll(default);
        Assert.Equal(0, (await Ctl(w.Visitor.Id).Count(default)).Value);
    }
}
