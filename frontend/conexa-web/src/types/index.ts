export type Role = 'Participant' | 'Exhibitor' | 'Organizer' | 'PlatformAdmin';
export type EventRole = 'Participant' | 'Exhibitor' | 'Organizer';
export type EventStatus = 'Draft' | 'Active' | 'Finished' | 'Archived';
export type MembershipStatus = 'Active' | 'Disabled' | 'Removed';
export type ReactionType = 'Like' | 'Insightful' | 'Celebrate';
export type ReportTargetType = 'Post' | 'Comment' | 'Profile';
export type ReportStatus = 'Open' | 'Dismissed' | 'ActionTaken';

export interface Paged<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface Me {
  id: string;
  email: string;
  displayName: string;
  photoUrl: string | null;
  roles: Role[];
}
export interface AuthResponse {
  token: string;
  expiresAt: string;
  user: Me;
}

export interface EventInfo {
  id: string;
  name: string;
  description: string | null;
  startsAt: string;
  endsAt: string;
  location: string | null;
  status: EventStatus;
}
export interface EventMapInfo {
  id: string;
  description: string;
  fileName: string;
  imageDataUrl: string;
  createdAt: string;
}
export interface MyEvent {
  event: EventInfo;
  roleInEvent: EventRole;
}

export interface Privacy {
  emailVisible: boolean;
  phoneVisible: boolean;
  phone: string | null;
  consentGiven: boolean;
}
export interface Profile {
  userId: string;
  displayName: string;
  email: string;
  photoUrl: string | null;
  companyName: string | null;
  jobTitle: string | null;
  area: string | null;
  bio: string | null;
  interests: string[];
  lookingFor: string | null;
  offers: string | null;
  privacy: Privacy;
}
export interface PublicProfile {
  userId: string;
  displayName: string;
  photoUrl: string | null;
  companyName: string | null;
  jobTitle: string | null;
  area: string | null;
  bio: string | null;
  interests: string[];
  lookingFor: string | null;
  offers: string | null;
  email: string | null;
  phone: string | null;
}

export interface ParticipantSummary {
  userId: string;
  displayName: string;
  photoUrl: string | null;
  companyName: string | null;
  jobTitle: string | null;
  area: string | null;
  interests: string[];
  lookingFor: string | null;
  roleInEvent: EventRole;
}
export interface ParticipantDetail {
  profile: PublicProfile;
  roleInEvent: EventRole;
  companies: { id: string; name: string }[];
}
export interface ParticipantFilters {
  areas: string[];
  interests: string[];
}

export interface CompanySummary {
  id: string;
  name: string;
  segment: string | null;
  description: string | null;
  boothLocation: string | null;
  representativeCount: number;
}
export interface Representative {
  userId: string;
  displayName: string;
  photoUrl: string | null;
  title: string | null;
  email: string | null;
  phone: string | null;
}
export interface CompanyDetail {
  id: string;
  name: string;
  description: string | null;
  segment: string | null;
  productsServices: string | null;
  boothLocation: string | null;
  website: string | null;
  representatives: Representative[];
  idealCustomer: string | null;
  tags: Tag[];
}

export interface Author {
  id: string;
  displayName: string;
  photoUrl: string | null;
}
export interface Post {
  id: string;
  eventId: string;
  author: Author;
  text: string;
  category: string | null;
  createdAt: string;
  updatedAt: string | null;
  commentCount: number;
  reactionCount: number;
  myReaction: ReactionType | null;
  canEdit: boolean;
  canDelete: boolean;
}
export interface PostComment {
  id: string;
  postId: string;
  author: Author;
  text: string;
  createdAt: string;
  canDelete: boolean;
}

export interface Member {
  userId: string;
  displayName: string;
  email: string;
  roleInEvent: EventRole;
  status: MembershipStatus;
  joinedAt: string;
}
export interface Report {
  id: string;
  targetType: ReportTargetType;
  targetId: string;
  targetPreview: string | null;
  reason: string;
  status: ReportStatus;
  createdAt: string;
  reporterName: string;
}
export interface AdminUser {
  id: string;
  displayName: string;
  email: string;
  isActive: boolean;
  roles: string[];
  createdAt: string;
}

export type TagStatus = 'Suggested' | 'Approved' | 'Rejected';
export interface Tag {
  id: string;
  parentId: string | null;
  name: string;
  description: string | null;
  synonyms: string[];
  status: TagStatus;
  libraryKey: string | null;
}
export interface SuggestionGroup {
  key: string;
  name: string;
  items: Tag[];
  matchesTagId: string | null;
  matchesTagName: string | null;
}
export interface LibrarySector { key: string; name: string; macroAreas: number; themes: number }
export interface ReviewResult { approved: number; rejected: number; merged: number }

export type VisitTimeframe = 'Exploring' | 'ThisQuarter' | 'ThisMonth' | 'Now';
export interface VisitorProfile {
  eventId: string;
  goal: string | null;
  timeframe: VisitTimeframe | null;
  tags: Tag[];
  consents: {
    matching: boolean; matchingAt: string | null;
    shareWithExhibitors: boolean; shareWithExhibitorsAt: string | null;
    externalAi: boolean; externalAiAt: string | null;
  };
  readyForMatching: boolean;
  updatedAt: string | null;
}

export interface Invite {
  id: string; code: string; role: EventRole; maxUses: number | null; uses: number;
  expiresAt: string | null; revokedAt: string | null; valid: boolean; createdAt: string;
}
export interface InvitePreview { valid: boolean; reason: string | null; eventName: string | null; role: EventRole | null }
export interface ImportRow { line: number; name: string; status: 'Created' | 'Skipped' | 'Invalid'; messages: string[] }
export interface ImportReport {
  committed: boolean; total: number; created: number; skipped: number; invalid: number;
  rows: ImportRow[]; fileErrors: string[]; newThemeSuggestions: string[];
}

export type MatchBand = 'None' | 'Low' | 'Medium' | 'High';
export interface BoothMatch {
  companyId: string; name: string; segment: string | null; boothLocation: string | null;
  score: number; band: MatchBand; reasons: string[];
}
export interface BoothMatchesResponse { ready: boolean; message: string | null; items: BoothMatch[] }
export interface LeadMatch {
  userId: string; displayName: string; companyName: string | null; jobTitle: string | null; goal: string | null;
  score: number; band: MatchBand; reasons: string[]; email: string | null; phone: string | null;
}
export interface LeadMatchesResponse { items: LeadMatch[]; consentingVisitors: number }

export interface AppNotification {
  id: string; eventId: string; title: string; body: string; link: string | null; createdAt: string; read: boolean;
}
export interface CheckInResult { companyId: string; companyName: string; alreadyRecorded: boolean }

export interface ThemeInsight { tagId: string; name: string; area: string | null; demand: number; supply: number }
export interface EventInsights {
  totals: {
    visitorsWithProfile: number; consentedMatching: number; consentedSharing: number; booths: number; boothsWithThemes: number;
    checkIns: number; checkedInVisitors: number; feedbackUseful: number; feedbackNotUseful: number; usefulRate: number | null;
  };
  themes: ThemeInsight[];
  gaps: ThemeInsight[];
  exposure: { boothsRecommended: number; boothsNeverRecommended: number; gini: number; top20Share: number; mostRecommended: string | null; mostRecommendedCount: number };
  profileGroups: { area: string; visitors: number; topThemes: string[] }[];
  topTerms: { term: string; count: number }[];
  notes: string[];
}
export interface BoothMetrics {
  companyId: string; name: string; boothLocation: string | null; checkIns: number; checkedInVisitors: number;
  recommendedTo: number; highAffinity: number; feedbackUseful: number; feedbackNotUseful: number; leadsAvailable: number;
}
