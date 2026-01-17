import { apiClient } from "./client";
import type {
  TasksReportData,
  WorkersReportData,
  ZonesReportData,
  ReportFilters,
} from "../types/report";

// Get tasks report summary
export async function getTasksReport(filters: ReportFilters): Promise<TasksReportData> {
  const params = new URLSearchParams();
  params.append("period", filters.period);
  if (filters.status && filters.status !== "all") params.append("status", filters.status);
  if (filters.startDate) params.append("startDate", filters.startDate);
  if (filters.endDate) params.append("endDate", filters.endDate);

  const response = await apiClient.get<{ data: TasksReportData }>(
    `/reports/tasks/summary?${params.toString()}`
  );
  return response.data.data;
}

// Get workers report summary
export async function getWorkersReport(filters: ReportFilters): Promise<WorkersReportData> {
  const params = new URLSearchParams();
  params.append("period", filters.period);
  if (filters.startDate) params.append("startDate", filters.startDate);
  if (filters.endDate) params.append("endDate", filters.endDate);

  const response = await apiClient.get<{ data: WorkersReportData }>(
    `/reports/workers/summary?${params.toString()}`
  );
  return response.data.data;
}

// Get zones report summary
export async function getZonesReport(filters: ReportFilters): Promise<ZonesReportData> {
  const params = new URLSearchParams();
  params.append("period", filters.period);
  if (filters.startDate) params.append("startDate", filters.startDate);
  if (filters.endDate) params.append("endDate", filters.endDate);

  const response = await apiClient.get<{ data: ZonesReportData }>(
    `/reports/zones/summary?${params.toString()}`
  );
  return response.data.data;
}

// Download attendance report as file
export function getAttendanceReportUrl(
  format: "excel" | "csv",
  filters?: { workerId?: number; zoneId?: number; startDate?: string; endDate?: string }
): string {
  const params = new URLSearchParams();
  params.append("format", format);
  if (filters?.workerId) params.append("workerId", filters.workerId.toString());
  if (filters?.zoneId) params.append("zoneId", filters.zoneId.toString());
  if (filters?.startDate) params.append("startDate", filters.startDate);
  if (filters?.endDate) params.append("endDate", filters.endDate);
  return `/api/reports/attendance?${params.toString()}`;
}

// Download tasks report as file
export function getTasksReportUrl(
  format: "excel" | "csv",
  filters?: { workerId?: number; zoneId?: number; startDate?: string; endDate?: string; status?: string }
): string {
  const params = new URLSearchParams();
  params.append("format", format);
  if (filters?.workerId) params.append("workerId", filters.workerId.toString());
  if (filters?.zoneId) params.append("zoneId", filters.zoneId.toString());
  if (filters?.startDate) params.append("startDate", filters.startDate);
  if (filters?.endDate) params.append("endDate", filters.endDate);
  if (filters?.status) params.append("status", filters.status);
  return `/api/reports/tasks?${params.toString()}`;
}
