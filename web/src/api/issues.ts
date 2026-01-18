import { apiClient } from "./client";
import type { IssueResponse, CreateIssueRequest, UpdateIssueStatusRequest } from "../types/issue";

// Backend returns ApiResponse wrapper
interface ApiResponse<T> {
  success: boolean;
  data?: T;
  message?: string;
  errors?: string[];
}

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

// Get critical issues
export async function getCriticalIssues(): Promise<IssueResponse[]> {
  const response = await apiClient.get<ApiResponse<IssueResponse[]>>("/issues/critical");
  return response.data.data || [];
}

// Get unresolved issues count
export async function getUnresolvedIssuesCount(): Promise<number> {
  const response = await apiClient.get<ApiResponse<number>>("/issues/unresolved-count");
  return response.data.data || 0;
}

// Report new issue
export async function reportIssue(request: CreateIssueRequest): Promise<IssueResponse> {
  const response = await apiClient.post<ApiResponse<IssueResponse>>("/issues/report", request);
  if (response.data.success && response.data.data) {
    return response.data.data;
  }
  throw new Error(response.data.message || "Failed to report issue");
}

// Report issue with photo
export async function reportIssueWithPhoto(formData: FormData): Promise<IssueResponse> {
  const response = await apiClient.post<ApiResponse<IssueResponse>>("/issues/report-with-photo", formData, {
    headers: {
      "Content-Type": "multipart/form-data",
    },
  });
  if (response.data.success && response.data.data) {
    return response.data.data;
  }
  throw new Error(response.data.message || "Failed to report issue");
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

// Get issue PDF report
export async function getIssuePdf(id: number): Promise<Blob> {
  const response = await apiClient.get(`/issues/${id}/pdf`, {
    responseType: "blob",
  });
  return response.data;
}
