import { apiClient } from "./client";
import type { ZoneResponse } from "../types/zone";

// Get all zones
export async function getZones(): Promise<ZoneResponse[]> {
  const response = await apiClient.get<{ data: ZoneResponse[] }>("/zones");
  return response.data.data;
}

// Get single zone by ID
export async function getZone(id: number): Promise<ZoneResponse> {
  const response = await apiClient.get<{ data: ZoneResponse }>(`/zones/${id}`);
  return response.data.data;
}

// Get zone by code
export async function getZoneByCode(code: string): Promise<ZoneResponse> {
  const response = await apiClient.get<{ data: ZoneResponse }>(`/zones/by-code/${code}`);
  return response.data.data;
}

// Get zones for map display (with GeoJSON boundaries)
export async function getZonesMapData(): Promise<unknown> {
  const response = await apiClient.get<{ data: unknown }>("/zones/map-data");
  return response.data.data;
}
