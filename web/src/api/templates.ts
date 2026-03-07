import { apiClient } from "./client";
import type { TaskPriority, TaskType } from "../types/task";

export interface TaskTemplate {
  id: number;
  title: string;
  description: string;
  municipalityId: number;
  zoneId: number | null;
  zoneName: string;
  frequency: string; // "Daily" | "Weekly" | "Monthly"
  time: string; // "HH:mm"
  isActive: boolean;
  priority: TaskPriority;
  taskType?: TaskType;
  requiresPhotoProof: boolean;
  estimatedDurationMinutes?: number;
  locationDescription?: string;
  defaultAssignedToUserId?: number;
  defaultAssignedToName?: string;
  defaultTeamId?: number;
  isTeamTask: boolean;
}

export interface CreateTaskTemplateRequest {
  title: string;
  description: string;
  zoneId?: number;
  frequency: string;
  time: string;
  priority: TaskPriority;
  taskType?: TaskType;
  requiresPhotoProof: boolean;
  estimatedDurationMinutes?: number;
  locationDescription?: string;
  defaultAssignedToUserId?: number;
  defaultTeamId?: number;
  isTeamTask: boolean;
}

// Get all task templates
export async function getTaskTemplates(): Promise<TaskTemplate[]> {
  const response = await apiClient.get<{ data: TaskTemplate[] }>("/task-templates");
  return response.data.data;
}

// Create a new task template
export async function createTaskTemplate(data: CreateTaskTemplateRequest): Promise<TaskTemplate> {
  const response = await apiClient.post<{ data: TaskTemplate }>("/task-templates", data);
  return response.data.data;
}

// Update an existing task template
export async function updateTaskTemplate(id: number, data: CreateTaskTemplateRequest): Promise<TaskTemplate> {
  const response = await apiClient.put<{ data: TaskTemplate }>(`/task-templates/${id}`, data);
  return response.data.data;
}

// Delete a task template
export async function deleteTaskTemplate(id: number): Promise<void> {
  await apiClient.delete(`/task-templates/${id}`);
}
