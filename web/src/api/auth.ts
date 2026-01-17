import { apiClient } from "./client";
import type { LoginRequest, LoginResponse } from "../types/auth";

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

export async function logout(): Promise<void> {
  await apiClient.post("/auth/logout");
}

export async function refreshToken(token: string): Promise<LoginResponse> {
  const response = await apiClient.post<ApiResponse<LoginResponse>>("/auth/refresh", { refreshToken: token });

  if (response.data.success && response.data.data) {
    return response.data.data;
  }

  return {
    success: false,
    error: response.data.errors?.join(", ") || response.data.message || "فشل تحديث الرمز"
  };
}
