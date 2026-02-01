import { apiClient } from "./client";

export interface Department {
  departmentId: number;
  name: string;
  nameEnglish?: string;
  code: string;
  description?: string;
  isActive: boolean;
  usersCount: number;
  createdAt: string;
}

export interface CreateDepartmentRequest {
  name: string;
  nameEnglish?: string;
  code: string;
  description?: string;
}

export interface UpdateDepartmentRequest {
  name?: string;
  nameEnglish?: string;
  code?: string;
  description?: string;
  isActive?: boolean;
}

// Get all departments
export async function getDepartments(activeOnly?: boolean): Promise<Department[]> {
  const params = activeOnly ? "?activeOnly=true" : "";
  const response = await apiClient.get<{ data: Department[] }>(`/departments${params}`);
  return response.data.data;
}

// Get department by ID
export async function getDepartment(id: number): Promise<Department> {
  const response = await apiClient.get<{ data: Department }>(`/departments/${id}`);
  return response.data.data;
}

// Create department
export async function createDepartment(request: CreateDepartmentRequest): Promise<Department> {
  const response = await apiClient.post<{ data: Department }>("/departments", request);
  return response.data.data;
}

// Update department
export async function updateDepartment(id: number, request: UpdateDepartmentRequest): Promise<Department> {
  const response = await apiClient.put<{ data: Department }>(`/departments/${id}`, request);
  return response.data.data;
}

// Delete department
export async function deleteDepartment(id: number): Promise<void> {
  await apiClient.delete(`/departments/${id}`);
}
