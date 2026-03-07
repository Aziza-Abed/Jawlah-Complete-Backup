// auth types

export type LoginRequest = {
  username: string;
  password: string;
};

export type LoginResponse = {
  success: boolean;
  token?: string;
  refreshToken?: string;
  expiresAt?: string;
  user?: UserDto;
  error?: string;
  // OTP (Two-Factor Authentication) fields
  requiresOtp?: boolean;
  sessionToken?: string;
  maskedPhone?: string;
  demoOtpCode?: string; // only set when MockSms is enabled (dev/demo mode)
};

export type VerifyOtpRequest = {
  sessionToken: string;
  otpCode: string;
  deviceId?: string;
};

export type VerifyOtpResponse = {
  success: boolean;
  token?: string;
  refreshToken?: string;
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
  sessionToken?: string;
  maskedPhone?: string;
  expiresAt?: string;
  message?: string;
  resendCooldownSeconds?: number;
  demoOtpCode?: string;
};

export type ForgotPasswordRequest = {
  username: string;
};

export type ForgotPasswordResponse = {
  success: boolean;
  sessionToken?: string;
  maskedPhone?: string;
  message?: string;
  expiresAt?: string;
  resendCooldownSeconds?: number;
  demoOtpCode?: string;
};

export type ResetPasswordRequest = {
  sessionToken: string;
  otpCode: string;
  newPassword: string;
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
