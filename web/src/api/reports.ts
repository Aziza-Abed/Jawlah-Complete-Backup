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

// admin supervisor monitoring

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
// Generic report download helper
async function downloadReport(
  reportType: string,
  format: "excel" | "pdf",
  filters?: { period?: string; startDate?: string; endDate?: string; status?: string }
): Promise<void> {
  const params = new URLSearchParams();
  if (filters?.period) params.append("period", filters.period);
  if (filters?.startDate) params.append("startDate", filters.startDate);
  if (filters?.endDate) params.append("endDate", filters.endDate);
  if (filters?.status && filters.status !== "all") params.append("status", filters.status);

  const endpoint = `/reports/${reportType}/${format === "pdf" ? "pdf" : "excel"}`;
  let response;
  try {
    response = await apiClient.get(`${endpoint}?${params.toString()}`, { responseType: "blob", timeout: 120000 });
  } catch (err: any) {
    // Extract error message from blob error response
    if (err.response?.data instanceof Blob) {
      try {
        const text = await err.response.data.text();
        const parsed = JSON.parse(text);
        throw new Error(parsed.message || "فشل إنشاء التقرير");
      } catch (parseErr) {
        if (parseErr instanceof Error && parseErr.message !== "فشل إنشاء التقرير") {
          throw parseErr;
        }
      }
    }
    throw err;
  }

  // If server returned JSON error instead of file, parse and throw
  if (response.data instanceof Blob && response.data.type === "application/json") {
    const text = await response.data.text();
    const error = JSON.parse(text);
    throw new Error(error.message || "فشل إنشاء التقرير");
  }

  const blob = new Blob([response.data], {
    type: format === "pdf" ? "application/pdf" : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
  });
  const downloadUrl = window.URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = downloadUrl;
  link.download = `${reportType}_report_${new Date().toISOString().split("T")[0]}.${format === "pdf" ? "pdf" : "xlsx"}`;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  window.URL.revokeObjectURL(downloadUrl);
}

export const downloadAttendanceReport = (format: "excel" | "pdf", filters?: { period?: string; startDate?: string; endDate?: string }) =>
  downloadReport("attendance", format, filters);

export const downloadTasksReport = (format: "excel" | "pdf", filters?: { period?: string; startDate?: string; endDate?: string; status?: string }) =>
  downloadReport("tasks", format, filters);
