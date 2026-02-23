// Shared API response wrapper type - matches backend ApiResponse<T>
export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  message?: string;
  errors?: string[];
}
