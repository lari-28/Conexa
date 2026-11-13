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
