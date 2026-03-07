import { apiClient } from "./client";
import type { WorkerLocation, LocationHistoryPoint } from "../types/tracking";

// Get current locations of all workers
export async function getWorkerLocations(): Promise<WorkerLocation[]> {
  const response = await apiClient.get<{ data: WorkerLocation[] }>("/tracking/locations");
  return response.data.data;
}

// Get location history for a specific worker
export async function getWorkerLocationHistory(userId: number, date?: string): Promise<LocationHistoryPoint[]> {
  const params = date ? { date } : {};
  const response = await apiClient.get<{ data: LocationHistoryPoint[] }>(`/tracking/history/${userId}`, { params });
  return response.data.data;
}
