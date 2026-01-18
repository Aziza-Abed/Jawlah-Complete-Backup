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
  format: "excel" | "csv" | "pdf",
  filters?: { period?: string; startDate?: string; endDate?: string }
): string {
  const params = new URLSearchParams();
  if (filters?.period) params.append("period", filters.period);
  if (filters?.startDate) params.append("startDate", filters.startDate);
  if (filters?.endDate) params.append("endDate", filters.endDate);

  const baseUrl = "/api/reports/attendance";
  if (format === "pdf") return `${baseUrl}/pdf?${params.toString()}`;
  params.append("format", format);
  return `${baseUrl}?${params.toString()}`;
}

// Download tasks report as file
export function getTasksReportUrl(
  format: "excel" | "csv" | "pdf",
  filters?: { period?: string; startDate?: string; endDate?: string; status?: string }
): string {
  const params = new URLSearchParams();
  if (filters?.period) params.append("period", filters.period);
  if (filters?.startDate) params.append("startDate", filters.startDate);
  if (filters?.endDate) params.append("endDate", filters.endDate);
  if (filters?.status && filters.status !== "all") params.append("status", filters.status);

  const baseUrl = "/api/reports/tasks";
  if (format === "pdf") return `${baseUrl}/pdf?${params.toString()}`;
  params.append("format", format);
  return `${baseUrl}?${params.toString()}`;
}
