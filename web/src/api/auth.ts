import { apiClient } from "./client";
import type {
  LoginRequest,
  LoginResponse,
  VerifyOtpRequest,
  VerifyOtpResponse,
  ResendOtpRequest,
  ResendOtpResponse
} from "../types/auth";
import type { RegisterRequest, UserResponse } from "../types/user";

// Backend returns ApiResponse<LoginResponse> wrapper
interface ApiResponse<T> {
  success: boolean;
  data?: T;
  message?: string;
  errors?: string[];
}

export async function login(request: LoginRequest): Promise<LoginResponse> {
  const response = await apiClient.post<ApiResponse<LoginResponse>>("/auth/login", request);

  // Extract nested data from ApiResponse wrapper
  if (response.data.success && response.data.data) {
    return response.data.data;
  }

  // Return error response
  return {
    success: false,
    error: response.data.errors?.join(", ") || response.data.message || "فشل تسجيل الدخول"
  };
}

export async function verifyOtp(request: VerifyOtpRequest): Promise<VerifyOtpResponse> {
  const response = await apiClient.post<ApiResponse<VerifyOtpResponse>>("/auth/verify-otp", request);

  if (response.data.success && response.data.data) {
    return response.data.data;
  }

  // Extract remaining attempts from response if available
  const data = response.data.data as VerifyOtpResponse | undefined;
  return {
    success: false,
    error: response.data.errors?.join(", ") || response.data.message || "رمز التحقق غير صحيح",
    remainingAttempts: data?.remainingAttempts
  };
}

export async function resendOtp(request: ResendOtpRequest): Promise<ResendOtpResponse> {
  const response = await apiClient.post<ApiResponse<ResendOtpResponse>>("/auth/resend-otp", request);

  if (response.data.success && response.data.data) {
    return {
      ...response.data.data,
      message: response.data.message || "تم إرسال رمز التحقق"
    };
  }

  return {
    success: false,
    message: response.data.errors?.join(", ") || response.data.message || "فشل إعادة إرسال الرمز"
  };
}

export async function register(data: RegisterRequest): Promise<ApiResponse<UserResponse>> {
  const response = await apiClient.post<ApiResponse<UserResponse>>("/auth/register", data);
  return response.data;
}

export async function logout(): Promise<void> {
  await apiClient.post("/auth/logout");
}

// Note: Refresh token not implemented in backend yet
// export async function refreshToken(token: string): Promise<LoginResponse> {
//   const response = await apiClient.post<ApiResponse<LoginResponse>>("/auth/refresh", { refreshToken: token });
//   if (response.data.success && response.data.data) {
//     return response.data.data;
//   }
//   return {
//     success: false,
//     error: response.data.errors?.join(", ") || response.data.message || "فشل تحديث الرمز"
//   };
// }
