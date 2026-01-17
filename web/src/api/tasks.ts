import { apiClient } from "./client";
import type { TaskResponse, CreateTaskRequest, UpdateTaskRequest } from "../types/task";

// Get all tasks (admin/supervisor)
export async function getTasks(): Promise<TaskResponse[]> {
  const response = await apiClient.get<{ data: TaskResponse[] }>("/tasks/all");
  return response.data.data;
}

// Get single task by ID
export async function getTask(id: number): Promise<TaskResponse> {
  const response = await apiClient.get<{ data: TaskResponse }>(`/tasks/${id}`);
  return response.data.data;
}

// Create new task
export async function createTask(request: CreateTaskRequest): Promise<TaskResponse> {
  const response = await apiClient.post<{ data: TaskResponse }>("/tasks", request);
  return response.data.data;
}

// Update task
export async function updateTask(id: number, request: UpdateTaskRequest): Promise<TaskResponse> {
  const response = await apiClient.put<{ data: TaskResponse }>(`/tasks/${id}`, request);
  return response.data.data;
}

// Delete task
export async function deleteTask(id: number): Promise<void> {
  await apiClient.delete(`/tasks/${id}`);
}

// Update task status
export async function updateTaskStatus(id: number, status: string): Promise<TaskResponse> {
  const response = await apiClient.put<{ data: TaskResponse }>(`/tasks/${id}/status`, { status });
  return response.data.data;
}

// Approve completed task (supervisor)
export async function approveTask(id: number, notes?: string): Promise<TaskResponse> {
  const response = await apiClient.put<{ data: TaskResponse }>(`/tasks/${id}/approve`, { notes });
  return response.data.data;
}

// Reject completed task (supervisor)
export async function rejectTask(id: number, reason: string): Promise<TaskResponse> {
  const response = await apiClient.put<{ data: TaskResponse }>(`/tasks/${id}/reject`, { reason });
  return response.data.data;
}

// Assign task to worker
export async function assignTask(id: number, userId: number): Promise<TaskResponse> {
  const response = await apiClient.post<{ data: TaskResponse }>(`/tasks/${id}/assign`, { userId });
  return response.data.data;
}

// Get overdue tasks
export async function getOverdueTasks(): Promise<TaskResponse[]> {
  const response = await apiClient.get<{ data: TaskResponse[] }>("/tasks/overdue");
  return response.data.data;
}

// Get pending tasks count
export async function getPendingTasksCount(): Promise<number> {
  const response = await apiClient.get<{ data: number }>("/tasks/pending-count");
  return response.data.data;
}
