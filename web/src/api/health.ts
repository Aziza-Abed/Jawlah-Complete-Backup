import { apiClient } from "./client";

export async function pingApi() {
  // TODO: update path according to backend health endpoint
  const response = await apiClient.get("/health");
  return response.data;
}
