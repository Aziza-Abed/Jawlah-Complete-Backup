import { apiClient } from "./client";
import type { UserResponse, UsersListResponse, UserRole } from "../types/user";

// Get current user profile
export async function getProfile(): Promise<UserResponse> {
  const response = await apiClient.get<{ data: UserResponse }>("/auth/me");
  return response.data.data;
}

// Update profile info
export async function updateProfile(data: {
  fullName: string;
  phoneNumber: string;
  email: string;
}): Promise<UserResponse> {
  const response = await apiClient.put<{ data: UserResponse }>(
    "/auth/profile",
    data,
  );
  return response.data.data;
}

// Change password
export async function changePassword(data: {
  oldPassword?: string;
  newPassword: string;
}): Promise<void> {
  await apiClient.post("/auth/change-password", data);
}

// Get all users (for admin)
export async function getUsers(
  page = 1,
  pageSize = 50,
  role?: UserRole,
): Promise<UsersListResponse> {
  const params = new URLSearchParams({
    page: page.toString(),
    pageSize: pageSize.toString(),
  });
  if (role) params.append("role", role);

  const response = await apiClient.get<{ data: UsersListResponse }>(
    `/users?${params.toString()}`,
  );
  return response.data.data;
}

// Get workers for assignment
export async function getWorkers(): Promise<UserResponse[]> {
  const data = await getUsers(1, 1000, "Worker");
  return data.items;
}

// Reset device ID (unbind device)
export async function resetDeviceId(userId: number): Promise<void> {
  await apiClient.post(`/users/${userId}/reset-device`);
}

// Admin reset password
export async function resetUserPassword(
  userId: number,
  newPassword: string,
): Promise<void> {
  await apiClient.post(`/users/${userId}/reset-password`, { newPassword });
}

// Create new user (optional, if needed for Accounts page)
export async function createUser(data: any): Promise<UserResponse> {
  const response = await apiClient.post<{ data: UserResponse }>("/users", data);
  return response.data.data;
}

// Update user status
export async function updateUserStatus(
  userId: number,
  status: string,
): Promise<void> {
  await apiClient.put(`/users/${userId}/status`, { status });
}
