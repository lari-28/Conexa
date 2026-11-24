using Conexa.Api.Contracts.Requests;
using Conexa.Api.Contracts.Responses;
using Conexa.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Conexa.Api.Tests;

/// <summary>Fluxos da fila de aprovação e dos temas do estande, com banco em memória.</summary>
public class TaxonomyControllerTests
{
    private static ApplicationDbContext NewDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static TaxonomyController Controller(ApplicationDbContext db, Guid userId, bool organizer = true)
    {
        var claims = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "t"));
        return new TaxonomyController(db, new FakeAccess(organizer))
        { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = claims } } };
    }

    private sealed class FakeAccess(bool organizer) : IEventAccessService
    {
        public Task<bool> IsMemberAsync(Guid e, ClaimsPrincipal u, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> IsOrganizerAsync(Guid e, ClaimsPrincipal u, CancellationToken ct = default) => Task.FromResult(organizer);
    }

    private static T Value<T>(ActionResult<T> r) => r.Value ?? (T)((ObjectResult)r.Result!).Value!;

    [Fact]
    public async Task Import_library_is_idempotent_and_builds_two_levels()
    {
        using var db = NewDb();
        var ev = new Event { Name = "Feira" }; db.Events.Add(ev); await db.SaveChangesAsync();
        var c = Controller(db, Guid.NewGuid());

        var first = Value(await c.ImportLibrary(ev.Id, new ImportLibraryRequest("industria"), default));
        Assert.NotEmpty(first);
        Assert.All(first, t => Assert.Equal(TagStatus.Approved, t.Status));
        Assert.Contains(first, t => t.ParentId is null);
        Assert.Contains(first, t => t.ParentId is not null);

        var second = Value(await c.ImportLibrary(ev.Id, new ImportLibraryRequest("industria"), default));
        Assert.Empty(second);
        Assert.Equal(first.Count, await db.InterestTags.CountAsync());
    }

    [Fact]
    public async Task Suggest_returns_existing_tag_by_name_or_synonym_instead_of_duplicating()
    {
        using var db = NewDb();
        var ev = new Event { Name = "Feira" };
        var iot = new InterestTag { EventId = ev.Id, Name = "IoT industrial", Status = TagStatus.Approved, Synonyms = ["sensores"] };
        db.AddRange(ev, iot); await db.SaveChangesAsync();
        var c = Controller(db, Guid.NewGuid(), organizer: false);

        var res = await c.Suggest(ev.Id, new SuggestTagRequest("Sensores", null, null), default);
        Assert.Equal(iot.Id, Value(res).Id);
        Assert.Equal(1, await db.InterestTags.CountAsync());

        var fresh = Value(await c.Suggest(ev.Id, new SuggestTagRequest("Logística", null, null), default));
        Assert.Equal(TagStatus.Suggested, fresh.Status);
    }

    [Fact]
    public async Task Review_merge_moves_links_without_duplicates_and_keeps_name_as_synonym()
    {
        using var db = NewDb();
        var ev = new Event { Name = "Feira" };
        var target = new InterestTag { EventId = ev.Id, Name = "IoT industrial", Status = TagStatus.Approved };
        var sug = new InterestTag { EventId = ev.Id, Name = "Sensores", Status = TagStatus.Suggested };
        var co1 = new Company { EventId = ev.Id, Name = "A" };
        var co2 = new Company { EventId = ev.Id, Name = "B" };
        db.AddRange(ev, target, sug, co1, co2);
        db.CompanyTags.AddRange(
            new CompanyTag { CompanyId = co1.Id, TagId = sug.Id },
            new CompanyTag { CompanyId = co2.Id, TagId = sug.Id },
            new CompanyTag { CompanyId = co2.Id, TagId = target.Id }); // já tem o destino
        await db.SaveChangesAsync();

        var res = Value(await Controller(db, Guid.NewGuid()).Review(ev.Id,
            new ReviewTagsRequest(ReviewAction.Merge, [sug.Id], target.Id), default));

        Assert.Equal(1, res.Merged);
        Assert.False(await db.InterestTags.AnyAsync(t => t.Id == sug.Id));
        Assert.Equal(2, await db.CompanyTags.CountAsync(x => x.TagId == target.Id)); // co1 e co2, sem duplicar
        Assert.Contains("Sensores", (await db.InterestTags.FindAsync(target.Id))!.Synonyms);
    }

    [Fact]
    public async Task Review_approve_and_reject_in_batch_and_merge_needs_approved_target()
    {
        using var db = NewDb();
        var ev = new Event { Name = "Feira" };
        var a = new InterestTag { EventId = ev.Id, Name = "A", Status = TagStatus.Suggested };
        var b = new InterestTag { EventId = ev.Id, Name = "B", Status = TagStatus.Suggested };
        db.AddRange(ev, a, b); await db.SaveChangesAsync();
        var c = Controller(db, Guid.NewGuid());

        Assert.Equal(1, Value(await c.Review(ev.Id, new ReviewTagsRequest(ReviewAction.Approve, [a.Id], null), default)).Approved);
        Assert.Equal(1, Value(await c.Review(ev.Id, new ReviewTagsRequest(ReviewAction.Reject, [b.Id], null), default)).Rejected);
        Assert.Equal(TagStatus.Approved, (await db.InterestTags.FindAsync(a.Id))!.Status);
        Assert.Equal(TagStatus.Rejected, (await db.InterestTags.FindAsync(b.Id))!.Status);

        var bad = await c.Review(ev.Id, new ReviewTagsRequest(ReviewAction.Merge, [b.Id], b.Id), default);
        Assert.IsType<ObjectResult>(bad.Result); // destino dentro da seleção
    }

    [Fact]
    public async Task Company_tags_enforce_limit_and_permission()
    {
        using var db = NewDb();
        var ev = new Event { Name = "Feira" };
        var co = new Company { EventId = ev.Id, Name = "Nimbus" };
        var tags = Enumerable.Range(0, 4).Select(i =>
            new InterestTag { EventId = ev.Id, Name = $"T{i}", Status = TagStatus.Approved }).ToList();
        var rep = Guid.NewGuid();
        db.AddRange(ev, co); db.AddRange(tags);
        db.CompanyMemberships.Add(new CompanyMembership { CompanyId = co.Id, UserId = rep });
        await db.SaveChangesAsync();

        // representante da empresa pode, até 3
        var ok = await Controller(db, rep, organizer: false).SetCompanyTags(ev.Id, co.Id,
            new SetCompanyTagsRequest(tags.Take(3).Select(t => t.Id).ToList()), default);
        Assert.Equal(3, Value(ok).Count);

        var over = await Controller(db, rep, organizer: false).SetCompanyTags(ev.Id, co.Id,
            new SetCompanyTagsRequest(tags.Select(t => t.Id).ToList()), default);
        Assert.IsType<ObjectResult>(over.Result);
        Assert.Equal(3, await db.CompanyTags.CountAsync()); // inalterado

        // estranho sem vínculo não pode
        var denied = await Controller(db, Guid.NewGuid(), organizer: false).SetCompanyTags(ev.Id, co.Id,
            new SetCompanyTagsRequest([tags[0].Id]), default);
        Assert.IsType<ForbidResult>(denied.Result);
    }

    [Fact]
    public async Task Non_organizer_cannot_list_pending_tags()
    {
        using var db = NewDb();
        var ev = new Event { Name = "Feira" }; db.Events.Add(ev); await db.SaveChangesAsync();
        var res = await Controller(db, Guid.NewGuid(), organizer: false).List(ev.Id, TagStatus.Suggested, default);
        Assert.IsType<ForbidResult>(res.Result);
    }
}
