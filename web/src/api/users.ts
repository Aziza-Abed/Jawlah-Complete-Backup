import { apiClient } from "./client";
import type { UserResponse, UsersListResponse } from "../types/user";

// Get all users (paginated)
export async function getUsers(page = 1, pageSize = 50): Promise<UsersListResponse> {
  const response = await apiClient.get<{ data: UsersListResponse }>(`/users?page=${page}&pageSize=${pageSize}`);
  return response.data.data;
}

// Get users by role (e.g., "Worker")
export async function getUsersByRole(role: string): Promise<UserResponse[]> {
  const response = await apiClient.get<{ data: UserResponse[] }>(`/users/by-role/${role}`);
  return response.data.data;
}

// Get single user by ID
export async function getUser(id: number): Promise<UserResponse> {
  const response = await apiClient.get<{ data: UserResponse }>(`/users/${id}`);
  return response.data.data;
}

// Get workers (shorthand for getUsersByRole("Worker"))
export async function getWorkers(): Promise<UserResponse[]> {
  return getUsersByRole("Worker");
}
