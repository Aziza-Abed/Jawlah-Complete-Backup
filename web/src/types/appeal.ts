// Appeal types matching backend DTOs

export type AppealType = "TaskRejection" | "AttendanceFailure";
export type AppealStatus = "Pending" | "Approved" | "Rejected";

export type AppealResponse = {
  appealId: number;
  appealType: AppealType;
  appealTypeName: string;
  entityType: string; // "Task" or "Attendance"
  entityId: number;

  // Worker details
  userId: number;
  workerName: string;
  workerExplanation: string;

  // Location details
  workerLatitude?: number;
  workerLongitude?: number;
  expectedLatitude?: number;
  expectedLongitude?: number;
  distanceMeters?: number;

  // Status
  status: AppealStatus;
  statusName: string;

  // Review details
  reviewedByUserId?: number;
  reviewedByName?: string;
  reviewedAt?: string;
  reviewNotes?: string;

  // Timestamps
  submittedAt: string;

  // Evidence
  evidencePhotoUrl?: string;

  // Original rejection
  originalRejectionReason?: string;

  // Related entity
  entityTitle?: string;
};

export type ReviewAppealRequest = {
  approved: boolean;
  reviewNotes?: string;
};
