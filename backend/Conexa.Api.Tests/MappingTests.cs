namespace Conexa.Api.Tests;

public class MappingTests
{
    private static ApplicationUser User(ContactPrivacy? privacy) =>
        new() { Email = "a@x.com", DisplayName = "Ana", Privacy = privacy };

    [Fact]
    public void Contacts_hidden_without_privacy_record()
    {
        var (email, phone) = Mapping.AuthorizedContacts(User(null));
        Assert.Null(email);
        Assert.Null(phone);
    }

    [Fact]
    public void Contacts_hidden_when_visible_but_no_consent()
    {
        var (email, phone) = Mapping.AuthorizedContacts(User(new ContactPrivacy
            { EmailVisible = true, PhoneVisible = true, Phone = "11999999999", ConsentGivenAt = null }));
        Assert.Null(email);
        Assert.Null(phone);
    }

    [Fact]
    public void Contacts_hidden_when_consented_but_flags_off()
    {
        var (email, phone) = Mapping.AuthorizedContacts(User(new ContactPrivacy
            { EmailVisible = false, PhoneVisible = false, Phone = "11999999999", ConsentGivenAt = DateTimeOffset.UtcNow }));
        Assert.Null(email);
        Assert.Null(phone);
    }

    [Fact]
    public void Only_flagged_contact_is_exposed_with_consent()
    {
        var (email, phone) = Mapping.AuthorizedContacts(User(new ContactPrivacy
            { EmailVisible = true, PhoneVisible = false, Phone = "11999999999", ConsentGivenAt = DateTimeOffset.UtcNow }));
        Assert.Equal("a@x.com", email);
        Assert.Null(phone);
    }

    [Fact]
    public void Public_profile_never_leaks_contact_without_consent()
    {
        var u = User(new ContactPrivacy { EmailVisible = true, Phone = "1", PhoneVisible = true });
        var pub = Mapping.ToPublic(u);
        Assert.Null(pub.Email);
        Assert.Null(pub.Phone);
    }

    [Fact]
    public void Interests_are_trimmed_lowercased_deduped_and_capped()
    {
        var result = Mapping.NormalizeInterests([" IA ", "ia", "Cloud", "", "  ", new string('x', 41)]);
        Assert.Equal(["ia", "cloud"], result);

        var many = Enumerable.Range(0, 50).Select(i => $"t{i}");
        Assert.Equal(20, Mapping.NormalizeInterests(many).Count);
    }

    [Fact]
    public void Interests_null_returns_empty() => Assert.Empty(Mapping.NormalizeInterests(null));

    [Fact]
    public void EscapeLike_escapes_wildcards() =>
        Assert.Equal(@"100\% \_a \\b", Mapping.EscapeLike(@"100% _a \b"));

    [Theory]
    [InlineData(0, 10, 50, 1, 10)]
    [InlineData(3, 500, 50, 3, 50)]
    [InlineData(2, 0, 50, 2, 1)]
    public void Clamp_bounds_page_and_size(int page, int size, int max, int ePage, int eSize) =>
        Assert.Equal((ePage, eSize), Mapping.Clamp(page, size, max));
}
