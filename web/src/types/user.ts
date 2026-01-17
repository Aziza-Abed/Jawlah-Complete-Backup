// User types matching backend DTOs

export type UserRole = "Admin" | "Supervisor" | "Worker";
export type UserStatus = "Active" | "Inactive" | "Suspended";
export type WorkerType = "Sweeper" | "Collector" | "Driver" | "Supervisor" | "Maintenance";

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
};

export type UsersListResponse = {
  items: UserResponse[];
  totalCount: number;
  page: number;
  pageSize: number;
};
