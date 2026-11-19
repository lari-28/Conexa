namespace Conexa.Api.Tests;

public class EventAccessServiceTests
{
    private static ApplicationDbContext NewDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static ClaimsPrincipal Principal(Guid id, params string[] roles)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, id.ToString()) };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    private static async Task<(ApplicationDbContext Db, Guid EventId)> Seed(
        Guid userId, EventRole role, MembershipStatus status)
    {
        var db = NewDb();
        var ev = new Event { Name = "Feira" };
        db.Events.Add(ev);
        db.EventMemberships.Add(new EventMembership { EventId = ev.Id, UserId = userId, RoleInEvent = role, Status = status });
        await db.SaveChangesAsync();
        return (db, ev.Id);
    }

    [Fact]
    public async Task Active_member_is_member_but_not_organizer()
    {
        var uid = Guid.NewGuid();
        var (db, ev) = await Seed(uid, EventRole.Participant, MembershipStatus.Active);
        var svc = new EventAccessService(db);
        Assert.True(await svc.IsMemberAsync(ev, Principal(uid, Roles.Participant)));
        Assert.False(await svc.IsOrganizerAsync(ev, Principal(uid, Roles.Participant)));
    }

    [Fact]
    public async Task Active_organizer_is_both()
    {
        var uid = Guid.NewGuid();
        var (db, ev) = await Seed(uid, EventRole.Organizer, MembershipStatus.Active);
        var svc = new EventAccessService(db);
        Assert.True(await svc.IsMemberAsync(ev, Principal(uid, Roles.Organizer)));
        Assert.True(await svc.IsOrganizerAsync(ev, Principal(uid, Roles.Organizer)));
    }

    [Theory]
    [InlineData(MembershipStatus.Disabled)]
    [InlineData(MembershipStatus.Removed)]
    public async Task Inactive_membership_has_no_access(MembershipStatus status)
    {
        var uid = Guid.NewGuid();
        var (db, ev) = await Seed(uid, EventRole.Organizer, status);
        var svc = new EventAccessService(db);
        Assert.False(await svc.IsMemberAsync(ev, Principal(uid, Roles.Organizer)));
        Assert.False(await svc.IsOrganizerAsync(ev, Principal(uid, Roles.Organizer)));
    }

    [Fact]
    public async Task Member_of_another_event_has_no_access()
    {
        var uid = Guid.NewGuid();
        var (db, _) = await Seed(uid, EventRole.Participant, MembershipStatus.Active);
        var svc = new EventAccessService(db);
        Assert.False(await svc.IsMemberAsync(Guid.NewGuid(), Principal(uid, Roles.Participant)));
    }

    [Fact]
    public async Task Stranger_has_no_access()
    {
        var (db, ev) = await Seed(Guid.NewGuid(), EventRole.Participant, MembershipStatus.Active);
        var svc = new EventAccessService(db);
        Assert.False(await svc.IsMemberAsync(ev, Principal(Guid.NewGuid(), Roles.Participant)));
    }

    [Fact]
    public async Task Platform_admin_has_global_access_without_membership()
    {
        var db = NewDb();
        var svc = new EventAccessService(db);
        var admin = Principal(Guid.NewGuid(), Roles.PlatformAdmin);
        Assert.True(await svc.IsMemberAsync(Guid.NewGuid(), admin));
        Assert.True(await svc.IsOrganizerAsync(Guid.NewGuid(), admin));
    }

    [Fact]
    public void GetUserId_returns_empty_for_missing_or_invalid_claim()
    {
        Assert.Equal(Guid.Empty, new ClaimsPrincipal(new ClaimsIdentity()).GetUserId());
        var bad = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "x")]));
        Assert.Equal(Guid.Empty, bad.GetUserId());
    }
}
