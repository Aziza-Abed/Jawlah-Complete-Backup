// Auth types matching backend DTOs

export type LoginRequest = {
  username: string;
  password: string;
};

export type LoginResponse = {
  success: boolean;
  token?: string;
  expiresAt?: string;
  user?: UserDto;
  error?: string;
};

export type UserDto = {
  userId: number;
  username: string;
  fullName: string;
  email?: string;
  role: string;
  workerType?: string;
  employeeId: string;
  phoneNumber: string;
  createdAt: string;
};
