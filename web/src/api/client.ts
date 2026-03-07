import axios, { AxiosError, type InternalAxiosRequestConfig } from "axios";
import { STORAGE_KEYS } from "../constants/storageKeys";
import { HTTP_REQUEST_TIMEOUT_MS } from "../constants/appConstants";

const BASE_URL = import.meta.env.VITE_API_BASE_URL;

// get or create a unique device ID for this browser
function getDeviceId(): string {
  let deviceId = localStorage.getItem(STORAGE_KEYS.DEVICE_ID);

  if (!deviceId) {
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

// shared axios instance
export const apiClient = axios.create({
  baseURL: BASE_URL,
  timeout: HTTP_REQUEST_TIMEOUT_MS,
  headers: {
    'Accept': 'application/json; charset=utf-8',
  },
  responseType: 'json',
  responseEncoding: 'utf8',
});

// attach token and device ID to every request
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem(STORAGE_KEYS.TOKEN);
    config.headers = config.headers ?? {};

    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }

    config.headers["X-Device-Id"] = getDeviceId();

    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// handle 401 by trying to refresh the token
apiClient.interceptors.response.use(
  undefined,
  async (error: AxiosError) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };

    if (error.response) {
      const status = error.response.status;
      console.error("API Error:", status, error.response.data);

      // try refresh (skip auth endpoints to avoid loops)
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

              // retry with new token
              originalRequest.headers.Authorization = `Bearer ${newToken}`;
              return apiClient(originalRequest);
            }
          } catch {
            // refresh failed
          }
        }

        // no valid token, go to login
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
