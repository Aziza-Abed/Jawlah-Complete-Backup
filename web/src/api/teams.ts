import { apiClient } from "./client";

export interface Team {
  teamId: number;
  departmentId: number;
  departmentName: string;
  name: string;
  code: string;
  description?: string;
  teamLeaderId?: number;
  teamLeaderName?: string;
  maxMembers: number;
  membersCount: number;
  isActive: boolean;
  createdAt: string;
}

// Get all teams for task assignment
export async function getTeams(activeOnly = true): Promise<Team[]> {
  const params = activeOnly ? "?activeOnly=true" : "";
  const response = await apiClient.get<{ data: Team[] }>(`/teams${params}`);
  return response.data.data;
}

// Get team by ID
export async function getTeam(id: number): Promise<Team> {
  const response = await apiClient.get<{ data: Team }>(`/teams/${id}`);
  return response.data.data;
}

// Create team request interface
export interface CreateTeamRequest {
  name: string;
  code: string;
  description?: string;
  departmentId: number;
  teamLeaderId?: number;
  maxMembers: number;
  isActive: boolean;
}

// Update team request interface
export interface UpdateTeamRequest {
  name: string;
  code: string;
  description?: string;
  teamLeaderId?: number;
  maxMembers: number;
  isActive: boolean;
}

// Create new team
export async function createTeam(data: CreateTeamRequest): Promise<Team> {
  const response = await apiClient.post<{ data: Team }>("/teams", data);
  return response.data.data;
}

// Update team
export async function updateTeam(id: number, data: UpdateTeamRequest): Promise<Team> {
  const response = await apiClient.put<{ data: Team }>(`/teams/${id}`, data);
  return response.data.data;
}

// Delete team
export async function deleteTeam(id: number): Promise<void> {
  await apiClient.delete(`/teams/${id}`);
}

// Team member interface
export interface TeamMember {
  userId: number;
  fullName: string;
  username: string;
  isTeamLeader?: boolean;
}

// Get team members
export async function getTeamMembers(teamId: number): Promise<TeamMember[]> {
  const response = await apiClient.get<{ data: TeamMember[] }>(`/teams/${teamId}/members`);
  return response.data.data;
}

// Add worker to team
export async function addWorkerToTeam(teamId: number, workerId: number): Promise<void> {
  await apiClient.post(`/teams/${teamId}/members/${workerId}`);
}

// Remove worker from team
export async function removeWorkerFromTeam(teamId: number, workerId: number): Promise<void> {
  await apiClient.delete(`/teams/${teamId}/members/${workerId}`);
}
