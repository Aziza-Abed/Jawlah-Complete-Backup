// User types matching backend DTOs

export type UserRole = "Admin" | "Supervisor" | "Worker";
export type UserStatus = "Active" | "Inactive" | "Suspended";
export type WorkerType = "Sanitation" | "PublicWorks" | "Agriculture" | "Maintenance";

export type UserResponse = {
  userId: number;
  username: string;
  fullName: string;
  email?: string;
  phoneNumber: string;
  role: UserRole;
  workerType?: WorkerType;
  departmentId?: number;
  department?: string;
  // Team assignment for team-based workers
  teamId?: number;
  teamName?: string;
  status: UserStatus;
  createdAt: string;
  lastLoginAt?: string;
  deviceId?: string;
  lastBatteryLevel?: number;
  isLowBattery?: boolean;
  // Supervisor assignment for workers
  supervisorId?: number;
  supervisorName?: string;
  warningCount?: number;
  // Employee ID alias for username (used in UI)
  employeeId?: string;
};

// Alias for backward compatibility
export type User = UserResponse;

export type UsersListResponse = {
  items: UserResponse[];
  totalCount: number;
  page: number;
  pageSize: number;
};

// Request types for creating/updating users
export type CreateUserRequest = {
  username: string;
  password: string;
  fullName: string;
  email?: string;
  phoneNumber: string;
  role: UserRole;
  workerType?: WorkerType;
  departmentId?: number;
  supervisorId?: number;
  zoneIds?: number[];
};

export type RegisterRequest = {
  username: string;
  password: string;
  fullName: string;
  email?: string;
  phoneNumber: string;
  role?: UserRole;
  workerType?: WorkerType;
  departmentId?: number;
  supervisorId?: number;
  municipalityId?: number;
  zoneIds?: number[];
};
