using Conexa.Api.Contracts.Requests;
using Conexa.Api.Contracts.Responses;
using Conexa.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Conexa.Api.Tests;

public class VisitorProfileTests
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset T1 = T0.AddDays(1);
    private static readonly ConsentState None = new(null, null, null);

    // ----- regras puras -----

    [Fact]
    public void Granting_sets_the_date_and_regranting_keeps_the_original()
    {
        var (s1, e1) = VisitorProfileRules.ApplyConsents(None, true, false, false, T0);
        Assert.Null(e1);
        Assert.Equal(T0, s1!.Matching);
        Assert.Null(s1.ShareWithExhibitors);

        var (s2, _) = VisitorProfileRules.ApplyConsents(s1, true, true, false, T1);
        Assert.Equal(T0, s2!.Matching);            // data original preservada
        Assert.Equal(T1, s2.ShareWithExhibitors);  // nova concessão
    }

    [Fact]
    public void Revoking_matching_revokes_the_dependents_and_dependents_require_matching()
    {
        var all = new ConsentState(T0, T0, T0);
        var (revoked, err) = VisitorProfileRules.ApplyConsents(all, false, false, false, T1);
        Assert.Null(err);
        Assert.Equal(new ConsentState(null, null, null), revoked);

        var (state, error) = VisitorProfileRules.ApplyConsents(None, false, true, false, T1);
        Assert.Null(state);
        Assert.NotNull(error);
        Assert.NotNull(VisitorProfileRules.ApplyConsents(None, false, false, true, T1).Error);
    }

    [Fact]
    public void Revoking_only_share_keeps_matching()
    {
        var (s, _) = VisitorProfileRules.ApplyConsents(new ConsentState(T0, T0, null), true, false, false, T1);
        Assert.Equal(T0, s!.Matching);
        Assert.Null(s.ShareWithExhibitors);
    }

    [Fact]
    public void Ready_for_matching_needs_consent_and_goal_or_tags()
    {
        Assert.False(VisitorProfileRules.IsReadyForMatching("quero automatizar a produção", 0, null)); // sem consentimento
        Assert.False(VisitorProfileRules.IsReadyForMatching("curto", 0, T0));                           // objetivo curto, sem temas
        Assert.False(VisitorProfileRules.IsReadyForMatching(null, 0, T0));
        Assert.True(VisitorProfileRules.IsReadyForMatching("quero automatizar a produção", 0, T0));
        Assert.True(VisitorProfileRules.IsReadyForMatching(null, 1, T0));                               // só chips
    }

    [Fact]
    public void Goal_is_cleaned_and_empty_becomes_null()
    {
        Assert.Equal("quero ERP", VisitorProfileRules.CleanGoal("  quero  ERP "));
        Assert.Null(VisitorProfileRules.CleanGoal("   "));
        Assert.Null(VisitorProfileRules.CleanGoal(null));
    }

    [Fact]
    public void Tag_validation_caps_at_eight_and_requires_approved_tags_of_the_event()
    {
        var ev = Guid.NewGuid();
        var tags = Enumerable.Range(0, 9).Select(i => new InterestTag { EventId = ev, Name = $"T{i}", Status = TagStatus.Approved }).ToList();
        Assert.Null(VisitorProfileRules.ValidateTags([], [], ev)); // temas são opcionais
        Assert.Null(VisitorProfileRules.ValidateTags(tags.Take(8).Select(t => t.Id).ToList(), tags.Take(8).ToList(), ev));
        Assert.NotNull(VisitorProfileRules.ValidateTags(tags.Select(t => t.Id).ToList(), tags, ev));
        var pend = new InterestTag { EventId = ev, Name = "P", Status = TagStatus.Suggested };
        Assert.NotNull(VisitorProfileRules.ValidateTags([pend.Id], [pend], ev));
    }

    // ----- controller -----

    private static ApplicationDbContext NewDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static VisitorProfileController Controller(ApplicationDbContext db, Guid userId) =>
        new(db) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext
        { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "t")) } } };

    private static T Value<T>(ActionResult<T> r) => r.Value ?? (T)((ObjectResult)r.Result!).Value!;

    [Fact]
    public async Task Get_without_profile_returns_empty_with_no_consent()
    {
        using var db = NewDb();
        var r = Value(await Controller(db, Guid.NewGuid()).Get(Guid.NewGuid(), default));
        Assert.Null(r.Goal);
        Assert.False(r.Consents.Matching);
        Assert.False(r.ReadyForMatching);
        Assert.Empty(r.Tags);
    }

    [Fact]
    public async Task Put_creates_then_updates_swapping_tags_and_keeping_consent_date()
    {
        using var db = NewDb();
        var ev = new Event { Name = "Feira" };
        var a = new InterestTag { EventId = ev.Id, Name = "A", Status = TagStatus.Approved };
        var b = new InterestTag { EventId = ev.Id, Name = "B", Status = TagStatus.Approved };
        db.AddRange(ev, a, b); await db.SaveChangesAsync();
        var uid = Guid.NewGuid();

        var first = Value(await Controller(db, uid).Put(ev.Id, new UpdateVisitorProfileRequest(
            "  Quero automatizar a produção  ", VisitTimeframe.ThisQuarter, [a.Id], true, false, false), default));
        Assert.Equal("Quero automatizar a produção", first.Goal);
        Assert.True(first.ReadyForMatching);
        Assert.Equal(["A"], first.Tags.Select(t => t.Name));
        var grantedAt = first.Consents.MatchingAt;

        var second = Value(await Controller(db, uid).Put(ev.Id, new UpdateVisitorProfileRequest(
            "Quero automatizar a produção", VisitTimeframe.Now, [a.Id, b.Id], true, true, false), default));
        Assert.Equal(["A", "B"], second.Tags.Select(t => t.Name));
        Assert.Equal(grantedAt, second.Consents.MatchingAt);
        Assert.True(second.Consents.ShareWithExhibitors);
        Assert.Equal(1, await db.ParticipantEventProfiles.CountAsync());

        var third = Value(await Controller(db, uid).Put(ev.Id, new UpdateVisitorProfileRequest(
            null, null, [b.Id], false, false, false), default));
        Assert.Equal(["B"], third.Tags.Select(t => t.Name));
        Assert.False(third.Consents.Matching);
        Assert.False(third.ReadyForMatching);
        Assert.Equal(1, await db.ParticipantTags.CountAsync());
    }

    [Fact]
    public async Task Put_rejects_dependent_consent_without_matching_and_changes_nothing()
    {
        using var db = NewDb();
        var ev = new Event { Name = "Feira" }; db.Events.Add(ev); await db.SaveChangesAsync();
        var res = await Controller(db, Guid.NewGuid()).Put(ev.Id,
            new UpdateVisitorProfileRequest("objetivo qualquer", null, null, false, true, false), default);
        Assert.IsType<ObjectResult>(res.Result);
        Assert.Equal(0, await db.ParticipantEventProfiles.CountAsync());
    }

    [Fact]
    public async Task Profiles_are_isolated_per_user()
    {
        using var db = NewDb();
        var ev = new Event { Name = "Feira" }; db.Events.Add(ev); await db.SaveChangesAsync();
        var ana = Guid.NewGuid();
        await Controller(db, ana).Put(ev.Id, new UpdateVisitorProfileRequest("objetivo da Ana aqui", null, null, true, false, false), default);

        var other = Value(await Controller(db, Guid.NewGuid()).Get(ev.Id, default));
        Assert.Null(other.Goal);
    }
}
