// Task types matching backend DTOs

export type TaskStatus = "Pending" | "InProgress" | "UnderReview" | "Completed" | "Rejected" | "Created" | "Assigned" | "Accepted" | "Submitted" | "Synced" | "Cancelled" | "FailedSync";
export type TaskPriority = "Low" | "Medium" | "High" | "Urgent";
export type TaskType = "GarbageCollection" | "StreetSweeping" | "ContainerMaintenance" | "RepairMaintenance" | "PublicSpaceCleaning" | "Inspection" | "Other";

export type TaskResponse = {
  taskId: number;
  sourceIssueId?: number;  // set if this task was created from an issue
  title: string;
  description: string;
  assignedToUserId: number;
  assignedToUserName: string;
  assignedByUserId?: number;
  assignedByUserName?: string;
  zoneId?: number;
  zoneName?: string;
  // Team task support
  teamId?: number;
  teamName?: string;
  isTeamTask: boolean;
  priority: TaskPriority;
  status: TaskStatus;
  taskType?: TaskType;
  requiresPhotoProof: boolean;
  estimatedDurationMinutes?: number;
  createdAt: string;
  scheduledAt?: string;
  dueDate?: string;
  startedAt?: string;
  completedAt?: string;   // server time (tamper-proof)
  eventTime?: string;     // device time when worker actually completed it
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
  // For individual tasks - either assignedToUserId or teamId is required
  assignedToUserId?: number;
  // For team tasks - all team members can work on this task
  teamId?: number;
  zoneId?: number;
  priority: TaskPriority;
  taskType?: TaskType;
  requiresPhotoProof?: boolean;
  estimatedDurationMinutes?: number;
  scheduledAt?: string;
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
  scheduledAt?: string;
  dueDate?: string;
  latitude?: number;
  longitude?: number;
  locationDescription?: string;
};
