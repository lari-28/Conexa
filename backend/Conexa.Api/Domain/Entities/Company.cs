namespace Conexa.Api.Domain.Entities;

public class Company
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Segment { get; set; }
    public string? ProductsServices { get; set; }
    public string? BoothLocation { get; set; }
    public string? Website { get; set; }
    /// <summary>Que tipo de cliente o expositor quer atender.</summary>
    public string? IdealCustomer { get; set; }
    public ICollection<CompanyTag> Tags { get; set; } = new List<CompanyTag>();

    public ICollection<CompanyMembership> Members { get; set; } = new List<CompanyMembership>();
}

public class CompanyMembership
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public string? Title { get; set; }
}
