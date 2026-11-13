import { api } from './client';
import type {
  AdminUser, AuthResponse, CompanyDetail, CompanySummary, EventInfo, EventRole, MembershipStatus, Me,
  Member, MyEvent, Paged, ParticipantDetail, ParticipantFilters, ParticipantSummary, Post, PostComment,
  Privacy, Profile, PublicProfile, ReactionType, Report, ReportStatus, ReportTargetType,
} from '../../types';

export const authApi = {
  login: (email: string, password: string) => api.post<AuthResponse>('/api/auth/login', { email, password }),
  register: (data: { name: string; email: string; password: string; accountType: 'Participant' | 'Exhibitor'; inviteCode?: string }) =>
    api.post<AuthResponse>('/api/auth/register', data),
  logout: () => api.post<void>('/api/auth/logout'),
  me: () => api.get<Me>('/api/auth/me'),
};

export const eventsApi = {
  mine: () => api.get<MyEvent[]>('/api/events/mine'),
};

export interface ProfileInput {
  displayName: string;
  photoUrl: string | null;
  companyName: string | null;
  jobTitle: string | null;
  area: string | null;
  bio: string | null;
  interests: string[];
  lookingFor: string | null;
  offers: string | null;
}
export interface PrivacyInput {
  emailVisible: boolean;
  phoneVisible: boolean;
  phone: string | null;
  consentAccepted: boolean;
}
export const profileApi = {
  me: () => api.get<Profile>('/api/profile/me'),
  update: (data: ProfileInput) => api.put<Profile>('/api/profile/me', data),
  updatePrivacy: (data: PrivacyInput) => api.put<Privacy>('/api/profile/me/privacy', data),
  publicProfile: (id: string) => api.get<PublicProfile>(`/api/users/${id}/profile`),
};

export const participantsApi = {
  list: (eventId: string, q: { q?: string; area?: string; interest?: string; page?: number }) =>
    api.get<Paged<ParticipantSummary>>(`/api/events/${eventId}/participants`, q),
  filters: (eventId: string) => api.get<ParticipantFilters>(`/api/events/${eventId}/participants/filters`),
  get: (eventId: string, userId: string) => api.get<ParticipantDetail>(`/api/events/${eventId}/participants/${userId}`),
};

export const companiesApi = {
  list: (eventId: string, q: { q?: string; page?: number }) =>
    api.get<Paged<CompanySummary>>(`/api/events/${eventId}/companies`, q),
  get: (eventId: string, id: string) => api.get<CompanyDetail>(`/api/events/${eventId}/companies/${id}`),
};

export interface ReactionResult { count: number; mine: ReactionType | null }

export const forumApi = {
  posts: (eventId: string, page: number, category?: string) =>
    api.get<Paged<Post>>(`/api/events/${eventId}/posts`, { page, category }),
  createPost: (eventId: string, text: string, category: string | null) =>
    api.post<Post>(`/api/events/${eventId}/posts`, { text, category }),
  deletePost: (id: string) => api.delete<void>(`/api/posts/${id}`),
  comments: (postId: string) => api.get<Paged<PostComment>>(`/api/posts/${postId}/comments`),
  addComment: (postId: string, text: string) => api.post<PostComment>(`/api/posts/${postId}/comments`, { text }),
  deleteComment: (id: string) => api.delete<void>(`/api/comments/${id}`),
  react: (postId: string, type: ReactionType) => api.post<ReactionResult>(`/api/posts/${postId}/reactions`, { type }),
  unreact: (postId: string) => api.delete<ReactionResult>(`/api/posts/${postId}/reactions`),
  report: (eventId: string, targetType: ReportTargetType, targetId: string, reason: string) =>
    api.post<{ id: string }>('/api/reports', { eventId, targetType, targetId, reason }),
};

export interface EventInput {
  name: string;
  description: string | null;
  startsAt: string;
  endsAt: string;
  location: string | null;
  status?: string;
}
export const organizerApi = {
  updateEvent: (eventId: string, data: EventInput) => api.put<EventInfo>(`/api/organizer/events/${eventId}`, data),
  members: (eventId: string, q?: string) => api.get<Paged<Member>>(`/api/organizer/events/${eventId}/members`, { q }),
  addMember: (eventId: string, email: string, roleInEvent: EventRole) =>
    api.post<void>(`/api/organizer/events/${eventId}/members`, { email, roleInEvent }),
  updateMember: (eventId: string, userId: string, data: { status?: MembershipStatus; roleInEvent?: EventRole }) =>
    api.patch<void>(`/api/organizer/events/${eventId}/members/${userId}`, data),
  reports: (eventId: string, status?: ReportStatus) =>
    api.get<Paged<Report>>(`/api/organizer/events/${eventId}/reports`, { status }),
  reviewReport: (id: string, data: { status: ReportStatus; hideContent: boolean; disableAuthorAccess: boolean }) =>
    api.patch<void>(`/api/organizer/reports/${id}`, data),
};

export const adminApi = {
  events: () => api.get<EventInfo[]>('/api/admin/events'),
  createEvent: (data: EventInput) => api.post<EventInfo>('/api/admin/events', data),
  users: (q?: string, page?: number) => api.get<Paged<AdminUser>>('/api/admin/users', { q, page }),
  setActive: (id: string, isActive: boolean) => api.patch<void>(`/api/admin/users/${id}`, { isActive }),
  addOrganizer: (eventId: string, email: string) => api.post<void>(`/api/admin/events/${eventId}/organizers`, { email }),
};
