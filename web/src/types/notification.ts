// notification types

export type NotificationType =
  | "TaskAssigned"
  | "TaskReminder"
  | "TaskUpdated"
  | "IssueReviewed"
  | "SystemAlert"
  | "BatteryLow"
  | "TaskStatusChanged"
  | "AttendanceReminder"
  | "ManualAttendanceApproved"
  | "ManualAttendanceRejected"
  | "IssueReported"
  | "IssueResolved"
  | "AppealSubmitted"
  | "IssueForwarded";

export type NotificationResponse = {
  notificationId: number;
  title: string;
  message: string;
  type: string;
  isRead: boolean;
  isSent: boolean;
  createdAt: string;
  sentAt?: string;
  readAt?: string;
  taskId?: number;
  issueId?: number;
};
