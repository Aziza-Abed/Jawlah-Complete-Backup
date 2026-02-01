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

// ========== ADMIN SUPERVISOR MONITORING ==========

export interface SupervisorMonitoringItem {
    userId: number;
    fullName: string;
    username: string;
    phoneNumber?: string;
    status: string;
    lastLoginAt?: string;
    workersCount: number;
    activeWorkersToday: number;
    tasksAssignedThisMonth: number;
    tasksCompletedThisMonth: number;
    tasksPendingReview: number;
    tasksDelayed: number;
    completionRate: number;
    avgResponseTimeHours: number;
    issuesReportedByWorkers: number;
    issuesResolved: number;
    issuesPending: number;
    performanceStatus: "Good" | "Warning" | "Critical";
}

export interface AdminAlert {
    id: number;
    type: "TooManyWorkers" | "PerformanceDrop" | "HighDelayRate" | "LowActivity";
    severity: "Info" | "Warning" | "Critical";
    message: string;
    supervisorId?: number;
    supervisorName?: string;
    createdAt: string;
}

export interface AdminSummary {
    totalSupervisors: number;
    activeSupervisors: number;
    totalWorkers: number;
    activeWorkersToday: number;
    totalTasksThisMonth: number;
    completedTasksThisMonth: number;
    overallCompletionRate: number;
    totalPendingIssues: number;
}

export interface AdminSupervisorMonitoringData {
    supervisors: SupervisorMonitoringItem[];
    alerts: AdminAlert[];
    summary: AdminSummary;
}

export async function getAdminSupervisorsMonitoring(): Promise<AdminSupervisorMonitoringData> {
    const response = await apiClient.get<{ data: AdminSupervisorMonitoringData }>("/reports/admin/supervisors-monitoring");
    return response.data.data;
}
// Download attendance report with authentication
export async function downloadAttendanceReport(
  format: "excel" | "pdf",
  filters?: { period?: string; startDate?: string; endDate?: string }
): Promise<void> {
  const params = new URLSearchParams();
  if (filters?.period) params.append("period", filters.period);
  if (filters?.startDate) params.append("startDate", filters.startDate);
  if (filters?.endDate) params.append("endDate", filters.endDate);

  const endpoint = format === "pdf" ? "/reports/attendance/pdf" : "/reports/attendance/excel";
  const url = `${endpoint}?${params.toString()}`;

  const response = await apiClient.get(url, { responseType: "blob" });

  // Create download link
  const blob = new Blob([response.data], {
    type: format === "pdf" ? "application/pdf" : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
  });
  const downloadUrl = window.URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = downloadUrl;
  link.download = `attendance_report_${new Date().toISOString().split("T")[0]}.${format === "pdf" ? "pdf" : "xlsx"}`;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  window.URL.revokeObjectURL(downloadUrl);
}

// Download tasks report with authentication
export async function downloadTasksReport(
  format: "excel" | "pdf",
  filters?: { period?: string; startDate?: string; endDate?: string; status?: string }
): Promise<void> {
  const params = new URLSearchParams();
  if (filters?.period) params.append("period", filters.period);
  if (filters?.startDate) params.append("startDate", filters.startDate);
  if (filters?.endDate) params.append("endDate", filters.endDate);
  if (filters?.status && filters.status !== "all") params.append("status", filters.status);

  const endpoint = format === "pdf" ? "/reports/tasks/pdf" : "/reports/tasks/excel";
  const url = `${endpoint}?${params.toString()}`;

  const response = await apiClient.get(url, { responseType: "blob" });

  // Create download link
  const blob = new Blob([response.data], {
    type: format === "pdf" ? "application/pdf" : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
  });
  const downloadUrl = window.URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = downloadUrl;
  link.download = `tasks_report_${new Date().toISOString().split("T")[0]}.${format === "pdf" ? "pdf" : "xlsx"}`;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  window.URL.revokeObjectURL(downloadUrl);
}
