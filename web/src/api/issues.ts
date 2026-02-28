import { apiClient } from "./client";
import type { IssueResponse, UpdateIssueStatusRequest, ForwardIssueRequest, CreateTaskFromIssueRequest } from "../types/issue";
import type { ApiResponse } from "../types/api";

// Get all issues
export async function getIssues(): Promise<IssueResponse[]> {
  const response = await apiClient.get<ApiResponse<IssueResponse[]>>("/issues");
  return response.data.data || [];
}

// Get single issue by ID
export async function getIssue(id: number): Promise<IssueResponse> {
  const response = await apiClient.get<ApiResponse<IssueResponse>>(`/issues/${id}`);
  if (response.data.success && response.data.data) {
    return response.data.data;
  }
  throw new Error(response.data.message || "Failed to fetch issue");
}

// Update issue status
export async function updateIssueStatus(id: number, request: UpdateIssueStatusRequest): Promise<IssueResponse> {
  const response = await apiClient.put<ApiResponse<IssueResponse>>(`/issues/${id}/status`, request);
  if (response.data.success && response.data.data) {
    return response.data.data;
  }
  throw new Error(response.data.message || "Failed to update issue status");
}

// Delete issue
export async function deleteIssue(id: number): Promise<void> {
  await apiClient.delete(`/issues/${id}`);
}

// forward issue to a department
export async function forwardIssue(id: number, request: ForwardIssueRequest): Promise<IssueResponse> {
  const response = await apiClient.post<ApiResponse<IssueResponse>>(`/issues/${id}/forward`, request);
  if (response.data.success && response.data.data) {
    return response.data.data;
  }
  throw new Error(response.data.message || "فشل تحويل البلاغ");
}

// convert issue to a task (supervisor/admin only)
export async function createTaskFromIssue(issueId: number, request: CreateTaskFromIssueRequest): Promise<{ taskId: number }> {
  const response = await apiClient.post<ApiResponse<{ taskId: number }>>(`/issues/${issueId}/create-task`, request);
  if (response.data.success && response.data.data) {
    return response.data.data;
  }
  throw new Error(response.data.message || "فشل تحويل البلاغ إلى مهمة");
}

