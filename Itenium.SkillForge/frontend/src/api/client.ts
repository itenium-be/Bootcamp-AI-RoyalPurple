import axios from 'axios';
import { useAuthStore } from '../stores';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

const api = axios.create({
  baseURL: API_BASE_URL,
});

// Add auth token to requests
api.interceptors.request.use((config) => {
  const token = useAuthStore.getState().accessToken;
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Handle 401 responses
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      useAuthStore.getState().logout();
    }
    return Promise.reject(error);
  },
);

interface LoginResponse {
  access_token: string;
  token_type: string;
  expires_in: number;
}

export async function loginApi(username: string, password: string): Promise<LoginResponse> {
  const params = new URLSearchParams();
  params.append('grant_type', 'password');
  params.append('username', username);
  params.append('password', password);
  params.append('client_id', 'skillforge-spa');
  params.append('scope', 'openid profile email');

  const response = await axios.post<LoginResponse>(`${API_BASE_URL}/connect/token`, params, {
    headers: {
      'Content-Type': 'application/x-www-form-urlencoded',
    },
  });

  return response.data;
}

interface Team {
  id: number;
  name: string;
}

export async function fetchUserTeams(): Promise<Team[]> {
  const response = await api.get<Team[]>('/api/team');
  return response.data;
}

export async function createTeam(name: string): Promise<Team> {
  const response = await api.post<Team>('/api/team', { name });
  return response.data;
}

export async function updateTeam(id: number, name: string): Promise<Team> {
  const response = await api.put<Team>(`/api/team/${id}`, { name });
  return response.data;
}

export async function deleteTeam(id: number): Promise<void> {
  await api.delete(`/api/team/${id}`);
}

interface Course {
  id: number;
  name: string;
  description: string | null;
  category: string | null;
  level: string | null;
  teamId: number | null;
}

export async function fetchCourses(teamId?: number): Promise<Course[]> {
  const params = teamId !== undefined ? { teamId } : {};
  const response = await api.get<Course[]>('/api/course', { params });
  return response.data;
}

interface ConsultantProfile {
  userId: string;
  teamId: number | null;
  teamName: string | null;
  firstName: string | null;
  lastName: string | null;
}

export async function fetchConsultants(): Promise<ConsultantProfile[]> {
  const response = await api.get<ConsultantProfile[]>('/api/consultantprofile/consultants');
  return response.data;
}

export async function assignProfile(userId: string, teamId: number): Promise<void> {
  await api.put(`/api/consultantprofile/consultants/${encodeURIComponent(userId)}`, { teamId });
}

export async function removeProfile(userId: string): Promise<void> {
  await api.delete(`/api/consultantprofile/consultants/${encodeURIComponent(userId)}`);
}

export async function fetchMyProfile(): Promise<ConsultantProfile> {
  const response = await api.get<ConsultantProfile>('/api/consultantprofile/me');
  return response.data;
}

export interface UserDto {
  id: string;
  userName: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  teams: number[];
}

export async function fetchUsers(): Promise<UserDto[]> {
  const response = await api.get<UserDto[]>('/api/user');
  return response.data;
}

export async function fetchCurrentUser(): Promise<UserDto> {
  const response = await api.get<UserDto>('/api/user/me');
  return response.data;
}

export async function fetchMyCoaches(): Promise<UserDto[]> {
  const response = await api.get<UserDto[]>('/api/user/coach');
  return response.data;
}

export interface CreateUserRequest {
  userName: string;
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  role: string;
  teams: number[];
}

export async function createUser(request: CreateUserRequest): Promise<UserDto> {
  const response = await api.post<UserDto>('/api/user', request);
  return response.data;
}

export async function updateUserRole(userId: string, role: string): Promise<void> {
  await api.put(`/api/user/${userId}/role`, { role });
}

export async function updateUserTeams(userId: string, teamIds: number[]): Promise<void> {
  await api.put(`/api/user/${userId}/teams`, { teamIds });
}

export interface RoadmapNode {
  id: number;
  name: string;
  description: string | null;
  tier: number;
  teamId: number;
  prerequisites: string[];
}

export async function fetchRoadmap(showAll = false): Promise<RoadmapNode[]> {
  const response = await api.get<RoadmapNode[]>(`/api/roadmap?showAll=${showAll}`);
  return response.data;
}

export interface ConsultantSummary {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  teams: number[];
  lastActivityAt: string | null;
  isInactive: boolean;
  activeGoalCount: number;
  isReady: boolean;
}

export async function fetchCoachDashboard(): Promise<ConsultantSummary[]> {
  const response = await api.get<ConsultantSummary[]>('/api/dashboard');
  return response.data;
}

export type CourseResourceType = 'Video' | 'Article' | 'Exercise' | 'Book' | 'Link' | 'Other';

export interface CourseResource {
  id: number;
  courseId: number;
  title: string;
  url: string | null;
  type: CourseResourceType;
  description: string | null;
  durationMinutes: number | null;
  order: number;
  skillId: number | null;
  toLevel: number | null;
}

export async function fetchCourseResources(courseId: number): Promise<CourseResource[]> {
  const response = await api.get<CourseResource[]>(`/api/course/${courseId}/resource`);
  return response.data;
}

export interface CourseRequest {
  name: string;
  description: string | null;
  category: string | null;
  level: string | null;
}

export async function createCourse(request: CourseRequest): Promise<Course> {
  const response = await api.post<Course>('/api/course', request);
  return response.data;
}

export async function updateCourse(id: number, request: CourseRequest): Promise<Course> {
  const response = await api.put<Course>(`/api/course/${id}`, request);
  return response.data;
}

export async function deleteCourse(id: number): Promise<void> {
  await api.delete(`/api/course/${id}`);
}

export interface CourseResourceRequest {
  title: string;
  url: string | null;
  type: CourseResourceType;
  description: string | null;
  durationMinutes: number | null;
  order: number;
  skillId: number | null;
  toLevel: number | null;
}

export async function createCourseResource(courseId: number, request: CourseResourceRequest): Promise<CourseResource> {
  const response = await api.post<CourseResource>(`/api/course/${courseId}/resource`, request);
  return response.data;
}

export async function updateCourseResource(
  courseId: number,
  resourceId: number,
  request: CourseResourceRequest,
): Promise<CourseResource> {
  const response = await api.put<CourseResource>(`/api/course/${courseId}/resource/${resourceId}`, request);
  return response.data;
}

export async function deleteCourseResource(courseId: number, resourceId: number): Promise<void> {
  await api.delete(`/api/course/${courseId}/resource/${resourceId}`);
}
