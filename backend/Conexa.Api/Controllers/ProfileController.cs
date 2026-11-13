namespace Conexa.Api.Controllers;

[Route("api")]
public class ProfileController(ApplicationDbContext db) : ApiControllerBase
{
    [HttpGet("profile/me")]
    public async Task<ActionResult<ProfileResponse>> GetMine(CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().Include(u => u.Profile).Include(u => u.Privacy)
            .FirstOrDefaultAsync(u => u.Id == CurrentUserId, ct);
        if (user is null) return NotFound();
        return Mapping.ToProfile(user);
    }

    [HttpPut("profile/me")]
    public async Task<ActionResult<ProfileResponse>> UpdateMine(UpdateProfileRequest req, CancellationToken ct)
    {
        var user = await db.Users.Include(u => u.Profile).Include(u => u.Privacy)
            .FirstOrDefaultAsync(u => u.Id == CurrentUserId, ct);
        if (user is null) return NotFound();

        user.DisplayName = req.DisplayName.Trim();
        var p = user.Profile ??= new UserProfile { UserId = user.Id };
        p.PhotoUrl = Mapping.Clean(req.PhotoUrl);
        p.CompanyName = Mapping.Clean(req.CompanyName);
        p.JobTitle = Mapping.Clean(req.JobTitle);
        p.Area = Mapping.Clean(req.Area);
        p.Bio = Mapping.Clean(req.Bio);
        p.Interests = Mapping.NormalizeInterests(req.Interests);
        p.LookingFor = Mapping.Clean(req.LookingFor);
        p.Offers = Mapping.Clean(req.Offers);

        await db.SaveChangesAsync(ct);
        return Mapping.ToProfile(user);
    }

    /// <summary>RN04/RN05: contato só é exibido com consentimento explícito, alterável a qualquer momento.</summary>
    [HttpPut("profile/me/privacy")]
    public async Task<ActionResult<PrivacyResponse>> UpdatePrivacy(UpdatePrivacyRequest req, CancellationToken ct)
    {
        var anyVisible = req.EmailVisible || req.PhoneVisible;
        if (anyVisible && !req.ConsentAccepted)
            return ValidationFailure(nameof(req.ConsentAccepted), "Confirme o consentimento para exibir seus contatos.");
        if (req.PhoneVisible && string.IsNullOrWhiteSpace(req.Phone))
            return ValidationFailure(nameof(req.Phone), "Informe o telefone para torná-lo visível.");

        var pr = await db.ContactPrivacies.FindAsync(new object[] { CurrentUserId }, ct);
        if (pr is null)
        {
            pr = new ContactPrivacy { UserId = CurrentUserId };
            db.ContactPrivacies.Add(pr);
        }
        pr.EmailVisible = req.EmailVisible;
        pr.PhoneVisible = req.PhoneVisible;
        pr.Phone = Mapping.Clean(req.Phone);
        if (!anyVisible) pr.ConsentGivenAt = null;
        else pr.ConsentGivenAt ??= DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return new PrivacyResponse(pr.EmailVisible, pr.PhoneVisible, pr.Phone, pr.ConsentGivenAt is not null);
    }

    /// <summary>Só é possível ver usuários com quem se compartilha ao menos um evento ativo (RN02).</summary>
    [HttpGet("users/{id:guid}/profile")]
    public async Task<ActionResult<PublicProfileResponse>> GetPublic(Guid id, CancellationToken ct)
    {
        var uid = CurrentUserId;
        var allowed = IsPlatformAdmin || await db.EventMemberships.AsNoTracking().AnyAsync(a =>
            a.UserId == uid && a.Status == MembershipStatus.Active &&
            db.EventMemberships.Any(b => b.UserId == id && b.EventId == a.EventId && b.Status == MembershipStatus.Active), ct);
        if (!allowed) return NotFound();

        var user = await db.Users.AsNoTracking().Include(u => u.Profile).Include(u => u.Privacy)
            .FirstOrDefaultAsync(u => u.Id == id && u.IsActive, ct);
        if (user is null) return NotFound();
        return Mapping.ToPublic(user);
    }
}
