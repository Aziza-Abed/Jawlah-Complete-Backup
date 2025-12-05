import axios from "axios";

const BASE_URL = import.meta.env.VITE_API_BASE_URL;

if (!BASE_URL) {
  console.warn(
    "VITE_API_BASE_URL is not defined. Please check your .env.local file."
  );
}

export const apiClient = axios.create({
  baseURL: BASE_URL,
});

apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem("jawlah_token");

    if (token) {
      if (!config.headers) {
        config.headers = {};
      }

      config.headers.Authorization = `Bearer ${token}`;
    }

    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

apiClient.interceptors.response.use(
  (response) => {
    return response;
  },
  (error) => {
    if (error.response) {
      const status = error.response.status;

      console.error("API Error:", status, error.response.data);

      if (status === 401) {
        // window.location.href = "/login";
      }
    } else {
      console.error("Network or CORS error:", error.message);
    }

    return Promise.reject(error);
  }
);
