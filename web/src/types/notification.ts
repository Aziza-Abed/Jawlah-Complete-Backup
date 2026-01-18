// Notification types matching backend DTOs

export type NotificationType = "TaskAssigned" | "TaskReminder" | "TaskUpdated" | "IssueReviewed" | "SystemAlert" | "BatteryLow";

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
};
