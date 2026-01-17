import { apiClient } from "./client";
import type { WorkerLocation } from "../types/tracking";

// Get current locations of all workers
export async function getWorkerLocations(): Promise<WorkerLocation[]> {
  const response = await apiClient.get<{ data: WorkerLocation[] }>("/tracking/locations");
  return response.data.data;
}

// Get location history for a specific worker
export async function getWorkerLocationHistory(userId: number): Promise<WorkerLocation[]> {
  const response = await apiClient.get<{ data: WorkerLocation[] }>(`/tracking/history/${userId}`);
  return response.data.data;
}
