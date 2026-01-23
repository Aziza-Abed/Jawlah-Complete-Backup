import { apiClient } from "./client";

// Attendance status enum
export type AttendanceStatus = "CheckedIn" | "CheckedOut" | "Absent" | "OnLeave";

// Attendance record from backend
export interface AttendanceRecord {
  attendanceId: number;
  userId: number;
  userName: string;
  zoneId?: number;
  zoneName?: string;
  checkInEventTime: string;
  checkOutEventTime?: string;
  workDuration?: string;
  status: AttendanceStatus;
  lateMinutes: number;
  overtimeMinutes: number;
  isValidated: boolean;
  validationMessage?: string;
  isManualEntry: boolean;
  manualEntryReason?: string;
  approvedByUserId?: number;
  approvedByUserName?: string;
}

// Manual attendance request (pending approval)
export interface ManualAttendanceRequest {
  attendanceId: number;
  userId: number;
  userName: string;
  reason: string;
  requestedAt: string;
  status: "Pending" | "Approved" | "Rejected";
}

// Get today's attendance for all workers (supervisor view)
export async function getTodayAttendance(): Promise<AttendanceRecord[]> {
  const response = await apiClient.get<{ data: AttendanceRecord[] }>("/attendance/today");
  return response.data.data;
}

// Get attendance history with optional filters
export async function getAttendanceHistory(params?: {
  userId?: number;
  zoneId?: number;
  startDate?: string;
  endDate?: string;
}): Promise<AttendanceRecord[]> {
  const queryParams = new URLSearchParams();
  if (params?.userId) queryParams.append("userId", params.userId.toString());
  if (params?.zoneId) queryParams.append("zoneId", params.zoneId.toString());
  if (params?.startDate) queryParams.append("startDate", params.startDate);
  if (params?.endDate) queryParams.append("endDate", params.endDate);

  const url = `/attendance/history${queryParams.toString() ? `?${queryParams.toString()}` : ""}`;
  const response = await apiClient.get<{ data: AttendanceRecord[] }>(url);
  return response.data.data;
}

// Get pending manual attendance requests (supervisor must approve)
export async function getPendingManualAttendance(): Promise<AttendanceRecord[]> {
  const response = await apiClient.get<{ data: AttendanceRecord[] }>("/attendance/pending-manual");
  return response.data.data;
}

// Approve or reject a manual attendance request
export async function approveManualAttendance(
  attendanceId: number,
  approved: boolean,
  notes?: string
): Promise<void> {
  await apiClient.post(`/attendance/${attendanceId}/approve`, {
    approved,
    notes,
  });
}
