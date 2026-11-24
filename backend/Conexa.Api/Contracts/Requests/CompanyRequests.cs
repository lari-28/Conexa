using System.ComponentModel.DataAnnotations;

namespace Conexa.Api.Contracts.Requests;

/// <summary>Perfil do estande. A oferta e o cliente ideal alimentam o matching.</summary>
public record CompanyRequest(
    [Required, StringLength(200, MinimumLength = 2)] string Name,
    [StringLength(2000)] string? Description,
    [StringLength(100)] string? Segment,
    [StringLength(2000)] string? ProductsServices,
    [StringLength(100)] string? BoothLocation,
    [Url, StringLength(300)] string? Website,
    [StringLength(1000)] string? IdealCustomer);
