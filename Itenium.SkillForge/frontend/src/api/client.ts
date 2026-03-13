import axios from 'axios';
import { useAuthStore } from '../stores';
import { queryClient } from '../lib/queryClient';

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
      queryClient.clear();
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

export interface Team {
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

export async function fetchTeamMembers(teamId: number): Promise<UserDto[]> {
  const response = await api.get<UserDto[]>(`/api/team/${teamId}/members`);
  return response.data;
}

export async function addTeamMember(teamId: number, userId: string): Promise<void> {
  await api.post(`/api/team/${teamId}/members/${userId}`);
}

export async function removeTeamMember(teamId: number, userId: string): Promise<void> {
  await api.delete(`/api/team/${teamId}/members/${userId}`);
}

export interface EnrollmentProgress {
  courseId: number;
  courseName: string;
  status: EnrollmentStatus;
  enrolledAt: string;
  completedAt: string | null;
}

export interface TeamMemberProgress {
  userId: string;
  fullName: string;
  email: string;
  enrollments: EnrollmentProgress[];
}

export async function fetchTeamProgress(teamId: number): Promise<TeamMemberProgress[]> {
  const response = await api.get<TeamMemberProgress[]>(`/api/team/${teamId}/progress`);
  return response.data;
}

export interface UserDto {
  id: string;
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  roles: string[];
  isActive: boolean;
  teams: number[];
  lastLoginAt: string | null;
}

export async function fetchUsers(): Promise<UserDto[]> {
  const response = await api.get<UserDto[]>('/api/user');
  return response.data;
}

export async function updateUserRoles(id: string, roles: string[]): Promise<void> {
  await api.put(`/api/user/${id}/roles`, { roles });
}

export async function setUserActive(id: string, isActive: boolean): Promise<void> {
  await api.put(`/api/user/${id}/active`, { isActive });
}

export interface CreateUserRequest {
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  password: string;
}

export async function createUser(data: CreateUserRequest): Promise<UserDto> {
  const response = await api.post<UserDto>('/api/user', data);
  return response.data;
}

export interface LoginHistoryEntry {
  id: number;
  userId: string;
  loggedInAt: string;
}

export async function recordActivity(): Promise<void> {
  await api.post('/api/user/activity');
}

export async function fetchLoginHistory(userId: string): Promise<LoginHistoryEntry[]> {
  const response = await api.get<LoginHistoryEntry[]>(`/api/user/${userId}/history`);
  return response.data;
}


export type EnrollmentStatus = 'Enrolled' | 'InProgress' | 'Completed';

export interface Enrollment {
  id: number;
  userId: string;
  courseId: number;
  course: Course;
  status: EnrollmentStatus;
  enrolledAt: string;
  completedAt: string | null;
}

export async function fetchEnrollments(): Promise<Enrollment[]> {
  const response = await api.get<Enrollment[]>('/api/enrollment');
  return response.data;
}

export async function enrollInCourse(courseId: number): Promise<Enrollment> {
  const response = await api.post<Enrollment>(`/api/enrollment/${courseId}`);
  return response.data;
}

export async function updateEnrollmentStatus(id: number, status: EnrollmentStatus): Promise<void> {
  await api.put(`/api/enrollment/${id}/status`, { status });
}

export async function unenroll(id: number): Promise<void> {
  await api.delete(`/api/enrollment/${id}`);
}

export type CourseStatus = 'Draft' | 'Published' | 'Archived';

export interface Course {
  id: number;
  name: string;
  description: string | null;
  category: string | null;
  level: string | null;
  status: CourseStatus;
  isMandatory: boolean;
}

export interface CourseFormData {
  name: string;
  description: string;
  category: string;
  level: string;
  status: CourseStatus;
  isMandatory: boolean;
}

export async function fetchCourses(): Promise<Course[]> {
  const response = await api.get<Course[]>('/api/course');
  return response.data;
}

export interface DashboardStats {
  activeLearners: number;
  completedEnrollments: number;
}

export async function fetchDashboardStats(): Promise<DashboardStats> {
  const response = await api.get<DashboardStats>('/api/dashboard/stats');
  return response.data;
}

export async function createCourse(data: CourseFormData): Promise<Course> {
  const response = await api.post<Course>('/api/course', data);
  return response.data;
}

export async function updateCourse(id: number, data: CourseFormData): Promise<Course> {
  const response = await api.put<Course>(`/api/course/${id}`, data);
  return response.data;
}

export async function deleteCourse(id: number): Promise<void> {
  await api.delete(`/api/course/${id}`);
}

export interface Feedback {
  id: number;
  userId: string;
  courseId: number;
  course: Course;
  rating: number;
  comment: string | null;
  createdAt: string;
}

export async function fetchFeedback(courseId?: number): Promise<Feedback[]> {
  const params = courseId != null ? `?courseId=${courseId}` : '';
  const response = await api.get<Feedback[]>(`/api/feedback${params}`);
  return response.data;
}

export async function submitFeedback(courseId: number, rating: number, comment: string | null): Promise<Feedback> {
  const response = await api.post<Feedback>(`/api/feedback/${courseId}`, { rating, comment });
  return response.data;
}

export async function deleteFeedback(id: number): Promise<void> {
  await api.delete(`/api/feedback/${id}`);
}

export interface TeamAssignment {
  courseId: number;
  courseName: string;
  isMandatory: boolean;
  assignedAt: string;
  userId: string | null;
  userFullName: string | null;
}

export async function fetchTeamAssignments(teamId: number): Promise<TeamAssignment[]> {
  const response = await api.get<TeamAssignment[]>(`/api/team/${teamId}/assignments`);
  return response.data;
}

export async function assignCourse(
  teamId: number,
  courseId: number,
  isMandatory: boolean,
  userId?: string,
): Promise<void> {
  await api.post(`/api/team/${teamId}/assignments/${courseId}`, { isMandatory, userId: userId ?? null });
}

export async function unassignCourse(teamId: number, courseId: number, userId?: string): Promise<void> {
  const params = userId ? `?userId=${encodeURIComponent(userId)}` : '';
  await api.delete(`/api/team/${teamId}/assignments/${courseId}${params}`);
}

export async function updateAssignment(
  teamId: number,
  courseId: number,
  isMandatory: boolean,
  userId?: string,
): Promise<void> {
  await api.put(`/api/team/${teamId}/assignments/${courseId}`, { isMandatory, userId: userId ?? null });
}
