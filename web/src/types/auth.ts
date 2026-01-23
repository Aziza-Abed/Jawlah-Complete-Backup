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
  // OTP (Two-Factor Authentication) fields
  requiresOtp?: boolean;
  sessionToken?: string;
  maskedPhone?: string;
};

export type VerifyOtpRequest = {
  sessionToken: string;
  otpCode: string;
  deviceId?: string;
};

export type VerifyOtpResponse = {
  success: boolean;
  token?: string;
  expiresAt?: string;
  user?: UserDto;
  error?: string;
  remainingAttempts?: number;
};

export type ResendOtpRequest = {
  sessionToken: string;
  deviceId?: string;
};

export type ResendOtpResponse = {
  success: boolean;
  maskedPhone?: string;
  expiresAt?: string;
  message?: string;
  resendCooldownSeconds?: number;
};

export type UserDto = {
  userId: number;
  username: string;
  fullName: string;
  email?: string;
  role: string;
  workerType?: string;
  employeeId?: string;  // Optional - not always returned from backend
  phoneNumber: string;
  createdAt: string;
};
