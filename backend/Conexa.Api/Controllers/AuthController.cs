using Microsoft.Extensions.Options;

namespace Conexa.Api.Controllers;

[Route("api/auth")]
public class AuthController(
    UserManager<ApplicationUser> users,
    ApplicationDbContext db,
    ITokenService tokens,
    IOptions<RegistrationOptions> registration) : ApiControllerBase
{
    [HttpPost("register"), AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest req, CancellationToken ct)
    {
        var opts = registration.Value;
        if (!opts.Open) return Problem(statusCode: 403, title: "O cadastro está fechado no momento.");
        if (!string.IsNullOrWhiteSpace(opts.InviteCode) && req.InviteCode?.Trim() != opts.InviteCode)
            return Problem(statusCode: 403, title: "Código de convite inválido.");

        var email = req.Email.Trim();
        if (await users.FindByEmailAsync(email) is not null)
            return Problem(statusCode: 409, title: "Já existe uma conta com este e-mail.");

        var user = new ApplicationUser { UserName = email, Email = email, DisplayName = req.Name.Trim() };
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var created = await users.CreateAsync(user, req.Password);
        if (!created.Succeeded) return IdentityProblem(created);

        // Organizador/admin nunca são criados por cadastro público (RF01: conforme convite/autorização).
        var exhibitor = req.AccountType == AccountType.Exhibitor;
        await users.AddToRoleAsync(user, exhibitor ? Roles.Exhibitor : Roles.Participant);

        db.UserProfiles.Add(new UserProfile { UserId = user.Id });
        db.ContactPrivacies.Add(new ContactPrivacy { UserId = user.Id }); // contatos ocultos por padrão

        var activeEvent = await db.Events.Where(e => e.Status == EventStatus.Active)
            .OrderBy(e => e.StartsAt).FirstOrDefaultAsync(ct);
        if (activeEvent is not null)
            db.EventMemberships.Add(new EventMembership
            {
                EventId = activeEvent.Id, UserId = user.Id,
                RoleInEvent = exhibitor ? EventRole.Exhibitor : EventRole.Participant
            });

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return StatusCode(StatusCodes.Status201Created, await BuildAuthAsync(user));
    }

    [HttpPost("login"), AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest req)
    {
        const string invalid = "E-mail ou senha inválidos.";
        var user = await users.FindByEmailAsync(req.Email.Trim());
        if (user is null) return Problem(statusCode: 401, title: invalid);

        if (await users.IsLockedOutAsync(user))
            return Problem(statusCode: 429, title: "Muitas tentativas. Tente novamente em alguns minutos.");

        if (!await users.CheckPasswordAsync(user, req.Password))
        {
            await users.AccessFailedAsync(user);
            return Problem(statusCode: 401, title: invalid);
        }
        await users.ResetAccessFailedCountAsync(user);

        if (!user.IsActive) return Problem(statusCode: 403, title: "Conta suspensa. Procure a organização.");
        return await BuildAuthAsync(user);
    }

    /// <summary>Tokens JWT são stateless: o logout efetivo é descartar o token no cliente.</summary>
    [HttpPost("logout")]
    public IActionResult Logout() => NoContent();

    [HttpGet("me")]
    public async Task<ActionResult<MeResponse>> Me()
    {
        var user = await users.FindByIdAsync(CurrentUserId.ToString());
        if (user is null) return Unauthorized();
        return await BuildMeAsync(user);
    }

    private async Task<MeResponse> BuildMeAsync(ApplicationUser user)
    {
        var roles = (await users.GetRolesAsync(user)).ToList();
        var photo = await db.UserProfiles.Where(p => p.UserId == user.Id)
            .Select(p => p.PhotoUrl).FirstOrDefaultAsync();
        return new MeResponse(user.Id, user.Email ?? "", user.DisplayName, photo, roles);
    }

    private async Task<AuthResponse> BuildAuthAsync(ApplicationUser user)
    {
        var me = await BuildMeAsync(user);
        var (token, expires) = tokens.Create(user, me.Roles);
        return new AuthResponse(token, expires, me);
    }
}
