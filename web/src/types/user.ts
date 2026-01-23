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
  department?: string;
  status: UserStatus;
  createdAt: string;
  lastLoginAt?: string;
  deviceId?: string;
  lastBatteryLevel?: number;
  isLowBattery?: boolean;
  supervisorId?: number;
  warningCount?: number;
};

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
  department?: string;
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
};
