using Conexa.Api.Contracts.Requests;
using Conexa.Api.Contracts.Responses;
using Conexa.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Conexa.Api.Tests;

public class CompanyProfileTests
{
    private static ApplicationDbContext NewDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private sealed class FakeAccess(bool organizer) : IEventAccessService
    {
        public Task<bool> IsMemberAsync(Guid e, ClaimsPrincipal u, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> IsOrganizerAsync(Guid e, ClaimsPrincipal u, CancellationToken ct = default) => Task.FromResult(organizer);
    }

    private static CompaniesController Controller(ApplicationDbContext db, Guid userId, bool organizer = false) =>
        new(db, new FakeAccess(organizer)) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext
        { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "t")) } } };

    private static readonly CompanyRequest Req = new("  Nimbus   Cloud ", "Nuvem para indústria", "Tecnologia",
        "Migração e operação em nuvem", "B-06", null, "Indústrias de médio porte");

    private static async Task<(ApplicationDbContext Db, Guid Ev, Guid User)> Setup(EventRole role, MembershipStatus st = MembershipStatus.Active)
    {
        var db = NewDb();
        var ev = new Event { Name = "Feira" };
        var uid = Guid.NewGuid();
        db.AddRange(ev, new EventMembership { EventId = ev.Id, UserId = uid, RoleInEvent = role, Status = st });
        await db.SaveChangesAsync();
        return (db, ev.Id, uid);
    }

    [Fact]
    public async Task Exhibitor_creates_company_and_becomes_representative()
    {
        var (db, ev, uid) = await Setup(EventRole.Exhibitor);
        var res = await Controller(db, uid).Create(ev, Req, default);
        Assert.IsType<CreatedAtActionResult>(res.Result);
        var c = await db.Companies.SingleAsync();
        Assert.Equal("Nimbus Cloud", c.Name);
        Assert.Equal("Indústrias de médio porte", c.IdealCustomer);
        Assert.Single(await db.CompanyMemberships.Where(m => m.UserId == uid).ToListAsync());
    }

    [Fact]
    public async Task Exhibitor_cannot_create_a_second_company_in_the_same_event()
    {
        var (db, ev, uid) = await Setup(EventRole.Exhibitor);
        await Controller(db, uid).Create(ev, Req, default);
        var again = await Controller(db, uid).Create(ev, Req, default);
        Assert.Equal(409, ((ObjectResult)again.Result!).StatusCode);
        Assert.Equal(1, await db.Companies.CountAsync());
    }

    [Theory]
    [InlineData(EventRole.Participant, MembershipStatus.Active)]
    [InlineData(EventRole.Exhibitor, MembershipStatus.Disabled)]
    public async Task Participants_and_inactive_exhibitors_cannot_create(EventRole role, MembershipStatus st)
    {
        var (db, ev, uid) = await Setup(role, st);
        var res = await Controller(db, uid).Create(ev, Req, default);
        Assert.Equal(403, ((ObjectResult)res.Result!).StatusCode);
        Assert.Equal(0, await db.Companies.CountAsync());
    }

    [Fact]
    public async Task Organizer_creates_without_becoming_representative()
    {
        var (db, ev, uid) = await Setup(EventRole.Organizer);
        await Controller(db, uid, organizer: true).Create(ev, Req, default);
        Assert.Equal(1, await db.Companies.CountAsync());
        Assert.Equal(0, await db.CompanyMemberships.CountAsync());
    }

    [Fact]
    public async Task Update_is_allowed_for_representative_and_organizer_but_not_strangers()
    {
        var (db, ev, rep) = await Setup(EventRole.Exhibitor);
        await Controller(db, rep).Create(ev, Req, default);
        var id = (await db.Companies.SingleAsync()).Id;
        var edit = Req with { Name = "Nimbus", ProductsServices = "Só nuvem" };

        Assert.IsNotType<ForbidResult>((await Controller(db, rep).Update(ev, id, edit, default)).Result);
        Assert.Equal("Nimbus", (await db.Companies.FindAsync(id))!.Name);

        Assert.IsNotType<ForbidResult>((await Controller(db, Guid.NewGuid(), organizer: true).Update(ev, id, edit, default)).Result);
        Assert.IsType<ForbidResult>((await Controller(db, Guid.NewGuid()).Update(ev, id, edit, default)).Result);
        Assert.IsType<NotFoundResult>((await Controller(db, rep).Update(ev, Guid.NewGuid(), edit, default)).Result);
    }

    [Fact]
    public async Task Mine_lists_only_my_companies_in_this_event()
    {
        var (db, ev, uid) = await Setup(EventRole.Exhibitor);
        await Controller(db, uid).Create(ev, Req, default);
        var other = new Company { EventId = Guid.NewGuid(), Name = "Outra" };
        db.Companies.Add(other);
        db.CompanyMemberships.Add(new CompanyMembership { CompanyId = other.Id, UserId = uid });
        await db.SaveChangesAsync();

        var mine = (await Controller(db, uid).Mine(ev, default)).Value!;
        Assert.Equal(["Nimbus Cloud"], mine.Select(m => m.Name));
    }
}
