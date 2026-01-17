import { apiClient } from "./client";
import type { DashboardOverview, WorkerStatus } from "../types/dashboard";

// Get dashboard overview stats
export async function getDashboardOverview(): Promise<DashboardOverview> {
  const response = await apiClient.get<{ data: DashboardOverview }>("/dashboard/overview");
  return response.data.data;
}

// Get all workers status
export async function getWorkerStatuses(): Promise<WorkerStatus[]> {
  const response = await apiClient.get<{ data: WorkerStatus[] }>("/dashboard/worker-status");
  return response.data.data;
}
