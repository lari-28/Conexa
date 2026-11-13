namespace Conexa.Api.Controllers;

[ApiController, Authorize]
public abstract class ApiControllerBase : ControllerBase
{
    protected Guid CurrentUserId => User.GetUserId();
    protected bool IsPlatformAdmin => User.IsInRole(Roles.PlatformAdmin);

    protected ActionResult ValidationFailure(string field, string message)
    {
        ModelState.AddModelError(field, message);
        return ValidationProblem(ModelState);
    }

    protected ActionResult IdentityProblem(IdentityResult result)
    {
        foreach (var e in result.Errors) ModelState.AddModelError(e.Code, e.Description);
        return ValidationProblem(ModelState);
    }

    /// <summary>Cria registro de auditoria (o chamador faz o SaveChanges).</summary>
    protected AuditLog Audit(Guid? eventId, string action, string targetType, Guid targetId, string? details = null) => new()
    {
        EventId = eventId, ActorId = CurrentUserId, Action = action,
        TargetType = targetType, TargetId = targetId, Details = details
    };
}
