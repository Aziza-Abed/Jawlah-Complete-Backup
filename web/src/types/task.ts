// Task types matching backend DTOs

export type TaskStatus = "Pending" | "InProgress" | "Completed" | "Cancelled" | "Approved" | "Rejected";
export type TaskPriority = "Low" | "Medium" | "High" | "Urgent";
export type TaskType = "GarbageCollection" | "StreetSweeping" | "ContainerMaintenance" | "RepairMaintenance" | "PublicSpaceCleaning" | "Inspection" | "Other";

export type TaskResponse = {
  taskId: number;
  title: string;
  description: string;
  assignedToUserId: number;
  assignedToUserName: string;
  assignedByUserId?: number;
  assignedByUserName?: string;
  zoneId?: number;
  zoneName?: string;
  priority: TaskPriority;
  status: TaskStatus;
  taskType?: TaskType;
  requiresPhotoProof: boolean;
  estimatedDurationMinutes?: number;
  createdAt: string;
  dueDate?: string;
  startedAt?: string;
  completedAt?: string;
  locationDescription?: string;
  completionNotes?: string;
  photoUrl?: string;
  photos: string[];
  latitude?: number;
  longitude?: number;
  syncTime?: string;
  syncVersion: number;
  // Progress tracking for multi-day tasks
  progressPercentage: number;
  progressNotes?: string;
  extendedDeadline?: string;
  // Auto-rejection tracking
  isAutoRejected: boolean;
  rejectionReason?: string;
  rejectedAt?: string;
  rejectionDistanceMeters?: number;
  // Distance verification
  maxDistanceMeters: number;
  completionDistanceMeters?: number;
  isDistanceWarning: boolean;
};

export type CreateTaskRequest = {
  title: string;
  description: string;
  assignedToUserId: number;
  zoneId?: number;
  priority: TaskPriority;
  taskType?: TaskType;
  requiresPhotoProof?: boolean;
  estimatedDurationMinutes?: number;
  dueDate?: string;
  latitude?: number;
  longitude?: number;
  locationDescription?: string;
};

export type UpdateTaskRequest = {
  title?: string;
  description?: string;
  assignedToUserId?: number;
  zoneId?: number;
  priority?: TaskPriority;
  taskType?: TaskType;
  requiresPhotoProof?: boolean;
  estimatedDurationMinutes?: number;
  dueDate?: string;
  latitude?: number;
  longitude?: number;
  locationDescription?: string;
};
