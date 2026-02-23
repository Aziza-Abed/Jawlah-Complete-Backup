import { apiClient } from "./client";
import type { ZoneResponse } from "../types/zone";

// Get all zones
export async function getZones(): Promise<ZoneResponse[]> {
  const response = await apiClient.get<{ data: ZoneResponse[] }>("/zones");
  return response.data.data;
}

// Get zones for map display (with GeoJSON boundaries)
export async function getZonesMapData(): Promise<unknown> {
  const response = await apiClient.get<{ data: unknown }>("/zones/map-data");
  return response.data.data;
}

export async function createZone(data: Partial<ZoneResponse>): Promise<ZoneResponse> {
  const response = await apiClient.post<{ data: ZoneResponse }>("/zones", data);
  return response.data.data;
}

export async function updateZone(id: number, data: Partial<ZoneResponse>): Promise<ZoneResponse> {
  const response = await apiClient.put<{ data: ZoneResponse }>(`/zones/${id}`, data);
  return response.data.data;
}

export async function deleteZone(id: number): Promise<void> {
  await apiClient.delete(`/zones/${id}`);
}
