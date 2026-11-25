namespace Conexa.Api.Contracts.Responses;

public record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public record MeResponse(Guid Id, string Email, string DisplayName, string? PhotoUrl, IReadOnlyList<string> Roles);
public record AuthResponse(string Token, DateTimeOffset ExpiresAt, MeResponse User);

public record EventResponse(Guid Id, string Name, string? Description, DateTimeOffset StartsAt,
    DateTimeOffset EndsAt, string? Location, EventStatus Status);
public record EventMapResponse(Guid Id, string Description, string FileName, string ImageDataUrl, DateTimeOffset CreatedAt);
public record MyEventResponse(EventResponse Event, EventRole RoleInEvent);

public record PrivacyResponse(bool EmailVisible, bool PhoneVisible, string? Phone, bool ConsentGiven);

/// <summary>Perfil do próprio usuário (inclui e-mail e privacidade).</summary>
public record ProfileResponse(Guid UserId, string DisplayName, string Email, string? PhotoUrl, string? CompanyName,
    string? JobTitle, string? Area, string? Bio, IReadOnlyList<string> Interests, string? LookingFor,
    string? Offers, PrivacyResponse Privacy);

/// <summary>Perfil visto por terceiros: Email/Phone só vêm preenchidos com consentimento do titular.</summary>
public record PublicProfileResponse(Guid UserId, string DisplayName, string? PhotoUrl, string? CompanyName,
    string? JobTitle, string? Area, string? Bio, IReadOnlyList<string> Interests, string? LookingFor,
    string? Offers, string? Email, string? Phone);

public record CompanyRef(Guid Id, string Name);
public record ParticipantSummary(Guid UserId, string DisplayName, string? PhotoUrl, string? CompanyName,
    string? JobTitle, string? Area, IReadOnlyList<string> Interests, string? LookingFor, EventRole RoleInEvent);
public record ParticipantDetailResponse(PublicProfileResponse Profile, EventRole RoleInEvent,
    IReadOnlyList<CompanyRef> Companies);
public record ParticipantFilters(IReadOnlyList<string> Areas, IReadOnlyList<string> Interests);

public record CompanySummary(Guid Id, string Name, string? Segment, string? Description,
    string? BoothLocation, int RepresentativeCount);
public record Representative(Guid UserId, string DisplayName, string? PhotoUrl, string? Title,
    string? Email, string? Phone);
public record CompanyDetail(Guid Id, string Name, string? Description, string? Segment, string? ProductsServices,
    string? BoothLocation, string? Website, IReadOnlyList<Representative> Representatives,
    string? IdealCustomer, IReadOnlyList<TagResponse> Tags);

public record AuthorInfo(Guid Id, string DisplayName, string? PhotoUrl);
public record PostResponse(Guid Id, Guid EventId, AuthorInfo Author, string Text, string? Category,
    DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt, int CommentCount, int ReactionCount,
    ReactionType? MyReaction, bool CanEdit, bool CanDelete);
public record CommentResponse(Guid Id, Guid PostId, AuthorInfo Author, string Text,
    DateTimeOffset CreatedAt, bool CanDelete);
public record ReactionResponse(int Count, ReactionType? Mine);

public record MemberResponse(Guid UserId, string DisplayName, string Email, EventRole RoleInEvent,
    MembershipStatus Status, DateTimeOffset JoinedAt);
public record ReportResponse(Guid Id, ReportTargetType TargetType, Guid TargetId, string? TargetPreview,
    string Reason, ReportStatus Status, DateTimeOffset CreatedAt, string ReporterName);
public record AdminUserResponse(Guid Id, string DisplayName, string Email, bool IsActive,
    IReadOnlyList<string> Roles, DateTimeOffset CreatedAt);
