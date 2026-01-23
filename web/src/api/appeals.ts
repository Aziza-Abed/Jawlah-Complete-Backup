import { apiClient } from "./client";
import type { AppealResponse, ReviewAppealRequest } from "../types/appeal";

// Get all pending appeals (supervisor/admin)
export async function getPendingAppeals(): Promise<AppealResponse[]> {
  const response = await apiClient.get<{ data: AppealResponse[] }>("/appeals/pending");
  return response.data.data;
}

// Get appeal by ID
export async function getAppeal(id: number): Promise<AppealResponse> {
  const response = await apiClient.get<{ data: AppealResponse }>(`/appeals/${id}`);
  return response.data.data;
}

// Review appeal (approve or reject)
export async function reviewAppeal(id: number, request: ReviewAppealRequest): Promise<void> {
  await apiClient.post(`/appeals/${id}/review`, request);
}

// Approve appeal
export async function approveAppeal(id: number, notes?: string): Promise<void> {
  await reviewAppeal(id, { approved: true, reviewNotes: notes });
}

// Reject appeal
export async function rejectAppeal(id: number, notes: string): Promise<void> {
  await reviewAppeal(id, { approved: false, reviewNotes: notes });
}
