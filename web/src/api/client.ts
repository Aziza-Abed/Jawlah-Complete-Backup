import axios, { AxiosError, type InternalAxiosRequestConfig } from "axios";
import { STORAGE_KEYS } from "../constants/storageKeys";

// 1) Read base URL from environment variables
const BASE_URL = import.meta.env.VITE_API_BASE_URL;

// Generate or retrieve a unique device ID for this browser
function getDeviceId(): string {
  let deviceId = localStorage.getItem(STORAGE_KEYS.DEVICE_ID);

  if (!deviceId) {
    // Generate a UUID v4
    deviceId = crypto.randomUUID();
    localStorage.setItem(STORAGE_KEYS.DEVICE_ID, deviceId);
  }

  return deviceId;
}

if (!BASE_URL) {
  console.warn(
    "VITE_API_BASE_URL is not defined. Please check your .env.local file."
  );
}

// 2) Create a single axios instance for the whole app
export const apiClient = axios.create({
  baseURL: BASE_URL,
  timeout: 10000, // 10 second timeout to prevent hanging
  headers: {
    'Accept': 'application/json; charset=utf-8',
    'Content-Type': 'application/json; charset=utf-8',
  },
  responseType: 'json',
  responseEncoding: 'utf8',
});

// 3) Request interceptor: inject JWT token and device ID automatically
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem(STORAGE_KEYS.TOKEN);

    // Ensure headers object exists
    config.headers = config.headers ?? {};

    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }

    // Always include device ID for OTP device binding
    config.headers["X-Device-Id"] = getDeviceId();

    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// 4) Response interceptor: handle 401 with token refresh
apiClient.interceptors.response.use(
  undefined,
  async (error: AxiosError) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };

    if (error.response) {
      const status = error.response.status;
      console.error("API Error:", status, error.response.data);

      // Try refresh token on 401 (but not for auth endpoints themselves)
      if (status === 401 && originalRequest && !originalRequest._retry) {
        const refreshToken = localStorage.getItem(STORAGE_KEYS.REFRESH_TOKEN);
        const isAuthEndpoint = originalRequest.url?.includes('/auth/login') ||
          originalRequest.url?.includes('/auth/refresh') ||
          originalRequest.url?.includes('/auth/forgot-password') ||
          originalRequest.url?.includes('/auth/reset-password');

        if (refreshToken && !isAuthEndpoint) {
          originalRequest._retry = true;

          try {
            const response = await axios.post(`${BASE_URL}/auth/refresh`, {
              refreshToken,
            });

            const data = response.data;
            if (data.success && data.data?.token) {
              const newToken = data.data.token;
              const newRefreshToken = data.data.refreshToken;

              localStorage.setItem(STORAGE_KEYS.TOKEN, newToken);
              if (newRefreshToken) {
                localStorage.setItem(STORAGE_KEYS.REFRESH_TOKEN, newRefreshToken);
              }

              // Retry the original request with new token
              originalRequest.headers.Authorization = `Bearer ${newToken}`;
              return apiClient(originalRequest);
            }
          } catch {
            // Refresh failed - fall through to redirect
          }
        }

        // Refresh failed or no refresh token - redirect to login
        localStorage.removeItem(STORAGE_KEYS.TOKEN);
        localStorage.removeItem(STORAGE_KEYS.USER);
        localStorage.removeItem(STORAGE_KEYS.REFRESH_TOKEN);
        if (window.location.pathname !== '/login') {
          window.location.href = '/login';
        }
      }
    } else {
      console.error("Network or CORS error:", error.message);
    }

    return Promise.reject(error);
  }
);
