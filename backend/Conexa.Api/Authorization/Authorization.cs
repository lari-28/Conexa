using Microsoft.AspNetCore.Routing;

namespace Conexa.Api.Authorization;

public static class Roles
{
    public const string Participant = "Participant";
    public const string Exhibitor = "Exhibitor";
    public const string Organizer = "Organizer";
    public const string PlatformAdmin = "PlatformAdmin";
    public static readonly string[] All = [Participant, Exhibitor, Organizer, PlatformAdmin];
}

public static class Policies
{
    /// <summary>Usuário autenticado e com vínculo ativo ao evento da rota ({eventId}) — RN02/RN06.</summary>
    public const string EventMember = "EventMember";
    /// <summary>Organizador do evento da rota ({eventId}) ou administrador — RN07.</summary>
    public const string EventOrganizer = "EventOrganizer";
}

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;
}

public class EventAccessRequirement(bool organizerOnly) : IAuthorizationRequirement
{
    public bool OrganizerOnly { get; } = organizerOnly;
}

public class EventAccessHandler(IHttpContextAccessor accessor, IEventAccessService access)
    : AuthorizationHandler<EventAccessRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, EventAccessRequirement requirement)
    {
        var http = accessor.HttpContext;
        if (http is null) return;
        if (!Guid.TryParse(http.GetRouteValue("eventId")?.ToString(), out var eventId)) return;

        var allowed = requirement.OrganizerOnly
            ? await access.IsOrganizerAsync(eventId, context.User)
            : await access.IsMemberAsync(eventId, context.User);

        if (allowed) context.Succeed(requirement);
    }
}
