namespace Conexa.Api.Domain.Enums;

public enum EventStatus { Draft, Active, Finished, Archived }
public enum EventRole { Participant, Exhibitor, Organizer }
public enum MembershipStatus { Active, Disabled, Removed }
public enum ContentStatus { Visible, Hidden, Deleted }
public enum ReactionType { Like, Insightful, Celebrate }
public enum ReportTargetType { Post, Comment, Profile }
public enum ReportStatus { Open, Dismissed, ActionTaken }
public enum TagStatus { Suggested, Approved, Rejected }
public enum VisitTimeframe { Exploring, ThisQuarter, ThisMonth, Now }
