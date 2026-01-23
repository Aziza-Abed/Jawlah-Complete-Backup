import axios, { AxiosError } from "axios";

// 1) Read base URL from environment variables
const BASE_URL = import.meta.env.VITE_API_BASE_URL;

// Generate or retrieve a unique device ID for this browser
function getDeviceId(): string {
  const DEVICE_ID_KEY = "followup_device_id";
  let deviceId = localStorage.getItem(DEVICE_ID_KEY);

  if (!deviceId) {
    // Generate a UUID v4
    deviceId = crypto.randomUUID();
    localStorage.setItem(DEVICE_ID_KEY, deviceId);
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
    const token = localStorage.getItem("followup_token");

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

// 4) Response interceptor: unified error logging / handling
apiClient.interceptors.response.use(
  undefined,
  (error: AxiosError) => {
    if (error.response) {
      const status = error.response.status;

      console.error("API Error:", status, error.response.data);

      if (status === 401) {
        // Token expired or invalid - redirect to login
        localStorage.removeItem('followup_token');
        localStorage.removeItem('followup_user');
        window.location.href = '/login';
      }
    } else {
      console.error("Network or CORS error:", error.message);
    }

    return Promise.reject(error);
  }
);
