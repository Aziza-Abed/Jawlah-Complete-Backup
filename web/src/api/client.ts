import axios, { AxiosError } from "axios";

// 1) Read base URL from environment variables
const BASE_URL = import.meta.env.VITE_API_BASE_URL;

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

// 3) Request interceptor: inject JWT token automatically (if present)
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem("jawlah_token");

    if (token) {
      // Ensure headers object exists
      config.headers = config.headers ?? {};
      config.headers.Authorization = `Bearer ${token}`;
    }

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
        localStorage.removeItem('jawlah_token');
        localStorage.removeItem('jawlah_user');
        window.location.href = '/login';
      }
    } else {
      console.error("Network or CORS error:", error.message);
    }

    return Promise.reject(error);
  }
);
