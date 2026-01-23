import { apiClient } from "./client";
import type { UserResponse, UsersListResponse, UserRole, CreateUserRequest } from "../types/user";

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

// Change password (requires old password for verification)
export async function changePassword(data: {
  oldPassword: string;
  newPassword: string;
}): Promise<void> {
  await apiClient.put("/users/change-password", data);
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

// Get workers assigned to the current supervisor
export async function getMyWorkers(): Promise<UserResponse[]> {
  const response = await apiClient.get<{ data: UserResponse[] }>("/users/my-workers");
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

// Update device ID manually
export async function updateDeviceId(userId: number, deviceId: string): Promise<void> {
  await apiClient.put(`/users/${userId}/device-id`, { deviceId });
}

// Admin reset password
export async function resetUserPassword(
  userId: number,
  newPassword: string,
): Promise<void> {
  await apiClient.post(`/users/${userId}/reset-password`, { newPassword });
}

// Create new user (optional, if needed for Accounts page)
export async function createUser(data: CreateUserRequest): Promise<UserResponse> {
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

// Assign zones to user
export async function assignUserZones(userId: number, zoneIds: number[]): Promise<void> {
  await apiClient.post(`/users/${userId}/zones`, { zoneIds });
}

// Get user assigned zones returns IDs
export async function getUserZones(userId: number): Promise<number[]> {
    const response = await apiClient.get<{ data: number[] }>(`/users/${userId}/zones`);
    return response.data.data;
}

// Update full user details
export async function updateUser(userId: number, data: Partial<UserResponse> & { password?: string, zoneIds?: number[] }): Promise<UserResponse> {
  const response = await apiClient.put<{ data: UserResponse }>(`/users/${userId}`, data);
  return response.data.data;
}

// Transfer specific workers to a different supervisor (updates each worker individually)
export async function transferWorkers(workerIds: number[], newSupervisorId: number | null): Promise<void> {
  // Update each worker's supervisorId
  await Promise.all(workerIds.map(workerId =>
    apiClient.put(`/users/${workerId}`, { supervisorId: newSupervisorId ?? 0 })
  ));
}

// Transfer ALL workers from one supervisor to another (bulk operation)
export async function transferAllWorkers(oldSupervisorId: number, newSupervisorId: number, reason?: string): Promise<void> {
  await apiClient.post(`/users/supervisors/${oldSupervisorId}/transfer-workers`, {
    newSupervisorId,
    transferReason: reason
  });
}

// Get users by role
export async function getUsersByRole(role: 'Admin' | 'Supervisor' | 'Worker'): Promise<UserResponse[]> {
  const response = await apiClient.get<{ data: UserResponse[] }>(`/users/by-role/${role}`);
  return response.data.data;
}

// Get supervisor's workers (admin can query any supervisor)
export async function getSupervisorWorkers(supervisorId: number): Promise<UserResponse[]> {
  const data = await getUsers(1, 1000, "Worker");
  return data.items.filter(w => w.supervisorId === supervisorId);
}
