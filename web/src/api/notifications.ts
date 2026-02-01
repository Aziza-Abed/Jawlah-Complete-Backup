import { apiClient } from "./client";
import type { NotificationResponse } from "../types/notification";

// Backend returns ApiResponse wrapper
interface ApiResponse<T> {
  success: boolean;
  data?: T;
  message?: string;
  errors?: string[];
}

// Get all notifications
export async function getNotifications(): Promise<NotificationResponse[]> {
  const response = await apiClient.get<ApiResponse<NotificationResponse[]>>("/notifications");
  return response.data.data || [];
}

// Get unread notifications
export async function getUnreadNotifications(): Promise<NotificationResponse[]> {
  const response = await apiClient.get<ApiResponse<NotificationResponse[]>>("/notifications/unread");
  return response.data.data || [];
}

// Get unread notifications count
export async function getUnreadNotificationsCount(): Promise<number> {
  const response = await apiClient.get<ApiResponse<number>>("/notifications/unread-count");
  return response.data.data || 0;
}

// Mark notification as read
export async function markNotificationAsRead(id: number): Promise<void> {
  await apiClient.put(`/notifications/${id}/mark-read`);
}

// Mark all notifications as read
export async function markAllNotificationsAsRead(): Promise<void> {
  await apiClient.put("/notifications/mark-all-read");
}

// Delete notification
export async function deleteNotification(id: number): Promise<void> {
  await apiClient.delete(`/notifications/${id}`);
}
