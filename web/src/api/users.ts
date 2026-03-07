import { apiClient } from "./client";
import { DEFAULT_PAGE_SIZE } from "../constants/appConstants";
import type { UserResponse, UsersListResponse, UserRole, User } from "../types/user";

// Re-export User type for convenience
export type { User };

// Get current user profile
export async function getProfile(): Promise<UserResponse> {
  const response = await apiClient.get<{ data: UserResponse }>("/users/me");
  return response.data.data;
}

// Update profile info
export async function updateProfile(data: {
  fullName: string;
  phoneNumber: string;
  email: string;
}): Promise<UserResponse> {
  const response = await apiClient.put<{ data: UserResponse }>(
    "/users/profile",
    data,
  );
  return response.data.data;
}

// Change password (requires old password for verification)
export async function changePassword(data: {
  oldPassword: string;
  newPassword: string;
  confirmPassword: string;
}): Promise<void> {
  await apiClient.put("/users/change-password", data);
}

// Upload profile photo
export async function uploadProfilePhoto(file: File): Promise<string> {
  const formData = new FormData();
  formData.append("photo", file);
  const response = await apiClient.put<{ data: { profilePhotoUrl: string } }>(
    "/users/profile-photo",
    formData,
    { headers: { "Content-Type": "multipart/form-data" } }
  );
  return response.data.data.profilePhotoUrl;
}

// Get all users (for admin)
export async function getUsers(
  page = 1,
  pageSize = DEFAULT_PAGE_SIZE,
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

// Get workers assigned to the current supervisor
export async function getMyWorkers(): Promise<UserResponse[]> {
  const response = await apiClient.get<{ data: UserResponse[] }>("/users/my-workers");
  return response.data.data;
}

// Get workers for assignment
export async function getWorkers(): Promise<UserResponse[]> {
  const response = await apiClient.get<{ data: UserResponse[] }>("/users/by-role/Worker");
  return response.data.data;
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

// Update user status
export async function updateUserStatus(
  userId: number,
  status: string,
): Promise<void> {
  await apiClient.put(`/users/${userId}/status`, { status });
}

// Assign zones to user
export async function assignUserZones(userId: number, zoneIds: number[]): Promise<void> {
  await apiClient.post(`/users/${userId}/zones`, { zoneIds });
}

// Get user assigned zones returns IDs
export async function getUserZones(userId: number): Promise<number[]> {
    const response = await apiClient.get<{ data: { zoneId: number }[] }>(`/users/${userId}/zones`);
    return response.data.data.map(z => z.zoneId);
}

// Update full user details
export async function updateUser(userId: number, data: Partial<UserResponse> & { password?: string, zoneIds?: number[] }): Promise<UserResponse> {
  const response = await apiClient.put<{ data: UserResponse }>(`/users/${userId}`, data);
  return response.data.data;
}

// Transfer specific workers to a different supervisor in a single bulk request
export async function transferWorkers(workerIds: number[], newSupervisorId: number): Promise<void> {
  await apiClient.post('/users/bulk-reassign-supervisor', { workerIds, newSupervisorId });
}

