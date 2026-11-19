namespace Conexa.Api.Contracts.Responses;

public record ConsentsResponse(
    bool Matching, DateTimeOffset? MatchingAt,
    bool ShareWithExhibitors, DateTimeOffset? ShareWithExhibitorsAt,
    bool ExternalAi, DateTimeOffset? ExternalAiAt);

public record VisitorProfileResponse(
    Guid EventId, string? Goal, VisitTimeframe? Timeframe, List<TagResponse> Tags,
    ConsentsResponse Consents, bool ReadyForMatching, DateTimeOffset? UpdatedAt);
