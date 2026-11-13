namespace Conexa.Api.Services;

/// <summary>Helpers de normalização e mapeamento. Contatos só saem daqui com consentimento (RN04).</summary>
public static class Mapping
{
    public static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    public static List<string> NormalizeInterests(IEnumerable<string>? items) =>
        (items ?? Array.Empty<string>())
            .Select(i => i.Trim().ToLowerInvariant())
            .Where(i => i.Length > 0 && i.Length <= 40)
            .Distinct()
            .Take(20)
            .ToList();

    public static string EscapeLike(string s) =>
        s.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    public static (int Page, int PageSize) Clamp(int page, int pageSize, int max = 50) =>
        (Math.Max(1, page), Math.Clamp(pageSize, 1, max));

    public static (string? Email, string? Phone) AuthorizedContacts(ApplicationUser u)
    {
        var pr = u.Privacy;
        var consented = pr?.ConsentGivenAt is not null;
        return (consented && pr!.EmailVisible ? u.Email : null,
                consented && pr!.PhoneVisible ? Clean(pr.Phone) : null);
    }

    public static EventResponse ToEvent(Event e) =>
        new(e.Id, e.Name, e.Description, e.StartsAt, e.EndsAt, e.Location, e.Status);

    public static ProfileResponse ToProfile(ApplicationUser u)
    {
        var p = u.Profile ?? new UserProfile();
        var pr = u.Privacy ?? new ContactPrivacy();
        return new ProfileResponse(u.Id, u.DisplayName, u.Email ?? "", p.PhotoUrl, p.CompanyName, p.JobTitle,
            p.Area, p.Bio, p.Interests, p.LookingFor, p.Offers,
            new PrivacyResponse(pr.EmailVisible, pr.PhoneVisible, pr.Phone, pr.ConsentGivenAt is not null));
    }

    public static PublicProfileResponse ToPublic(ApplicationUser u)
    {
        var p = u.Profile ?? new UserProfile();
        var (email, phone) = AuthorizedContacts(u);
        return new PublicProfileResponse(u.Id, u.DisplayName, p.PhotoUrl, p.CompanyName, p.JobTitle, p.Area,
            p.Bio, p.Interests, p.LookingFor, p.Offers, email, phone);
    }
}
