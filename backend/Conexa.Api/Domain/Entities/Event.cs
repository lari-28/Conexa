namespace Conexa.Api.Domain.Entities;

public class Event
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public string? Location { get; set; }
    public EventStatus Status { get; set; } = EventStatus.Draft;
    /// <summary>Temas e ideias principais da feira, informados pelo organizador.</summary>
    public string? MainThemes { get; set; }

    public ICollection<EventMembership> Memberships { get; set; } = new List<EventMembership>();
    public ICollection<EventMap> Maps { get; set; } = new List<EventMap>();
    public ICollection<EventActivity> Activities { get; set; } = new List<EventActivity>();
}

public class EventActivity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;
    public EventActivityKind Kind { get; set; } = EventActivityKind.Activity;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Location { get; set; } = string.Empty;
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public ICollection<EventActivityPerson> People { get; set; } = new List<EventActivityPerson>();
}

public class EventActivityPerson
{
    public Guid ActivityId { get; set; }
    public EventActivity Activity { get; set; } = null!;
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
}

public class EventMap
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public byte[] Image { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class EventMembership
{
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public EventRole RoleInEvent { get; set; } = EventRole.Participant;
    public MembershipStatus Status { get; set; } = MembershipStatus.Active;
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;
}
