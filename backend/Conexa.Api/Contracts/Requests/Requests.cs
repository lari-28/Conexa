using System.ComponentModel.DataAnnotations;

namespace Conexa.Api.Contracts.Requests;

public enum AccountType { Participant, Exhibitor }

public record RegisterRequest(
    [Required, StringLength(120, MinimumLength = 2)] string Name,
    [Required, EmailAddress, StringLength(200)] string Email,
    [Required, StringLength(100, MinimumLength = 8)] string Password,
    AccountType AccountType = AccountType.Participant,
    string? InviteCode = null);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public record UpdateProfileRequest(
    [Required, StringLength(120, MinimumLength = 2)] string DisplayName,
    [Url, StringLength(500)] string? PhotoUrl,
    [StringLength(150)] string? CompanyName,
    [StringLength(150)] string? JobTitle,
    [StringLength(100)] string? Area,
    [StringLength(1000)] string? Bio,
    List<string>? Interests,
    [StringLength(500)] string? LookingFor,
    [StringLength(500)] string? Offers);

public record UpdatePrivacyRequest(
    bool EmailVisible,
    bool PhoneVisible,
    [StringLength(30)] string? Phone,
    bool ConsentAccepted);

public record PostRequest(
    [Required, StringLength(2000, MinimumLength = 1)] string Text,
    [StringLength(50)] string? Category);

public record CommentRequest([Required, StringLength(1000, MinimumLength = 1)] string Text);

public record ReactionRequest(ReactionType Type = ReactionType.Like);

public record CreateReportRequest(
    Guid EventId,
    ReportTargetType TargetType,
    Guid TargetId,
    [Required, StringLength(500, MinimumLength = 5)] string Reason);

public record EventRequest(
    [Required, StringLength(200, MinimumLength = 2)] string Name,
    [StringLength(2000)] string? Description,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    [StringLength(200)] string? Location,
    EventStatus? Status = null);

public record EventActivityRequest(
    EventActivityKind Kind,
    [Required, StringLength(200, MinimumLength = 2)] string Title,
    [StringLength(2000)] string? Description,
    [Required, StringLength(200, MinimumLength = 2)] string Location,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    List<Guid>? PersonIds);

public record AddMemberRequest([Required, EmailAddress] string Email, EventRole RoleInEvent = EventRole.Participant);

public record UpdateMemberRequest(MembershipStatus? Status, EventRole? RoleInEvent);

public record ReviewReportRequest(ReportStatus Status, bool HideContent = false, bool DisableAuthorAccess = false);

public record SetUserActiveRequest(bool IsActive);

public record AddOrganizerRequest([Required, EmailAddress] string Email);
