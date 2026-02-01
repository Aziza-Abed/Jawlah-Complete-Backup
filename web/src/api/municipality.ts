import { apiClient } from "./client";

// ============== Municipality Settings (for app configuration) ==============
// Types for municipality data

export interface MunicipalitySettings {
  municipalityId: number;
  code: string;
  name: string;
  nameEnglish?: string;
  logoUrl?: string;
  centerLatitude: number;
  centerLongitude: number;
  bounds: {
    minLatitude: number;
    maxLatitude: number;
    minLongitude: number;
    maxLongitude: number;
  };
  defaultStartTime: string;
  defaultEndTime: string;
  defaultGraceMinutes: number;
  maxAcceptableAccuracyMeters: number;
  defaultZoom: number;
}

export interface MunicipalityBasic {
  municipalityId?: number;
  code?: string;
  name: string;
  nameEnglish?: string;
  logoUrl?: string;
  centerLatitude: number;
  centerLongitude: number;
  defaultZoom: number;
}

// Default fallback settings (Palestine region center)
export const DEFAULT_MUNICIPALITY_SETTINGS: MunicipalityBasic = {
  name: "FollowUp",
  nameEnglish: "FollowUp System",
  centerLatitude: 31.9,
  centerLongitude: 35.2,
  defaultZoom: 10,
};

/**
 * Get current user's municipality settings (requires authentication)
 */
export async function getCurrentMunicipalitySettings(): Promise<MunicipalitySettings | null> {
  try {
    const response = await apiClient.get<{ success: boolean; data: MunicipalitySettings }>("/municipality/current");
    return response.data.data;
  } catch (error) {
    console.error("Failed to fetch municipality settings:", error);
    return null;
  }
}

/**
 * Get default municipality settings (for login screen, no auth required)
 */
export async function getDefaultMunicipalitySettings(): Promise<MunicipalityBasic> {
  try {
    const response = await apiClient.get<{ success: boolean; data: MunicipalityBasic }>("/municipality/default");
    return response.data.data;
  } catch (error) {
    console.error("Failed to fetch default municipality settings:", error);
    return DEFAULT_MUNICIPALITY_SETTINGS;
  }
}

// ============== Municipality Admin Management ==============

export interface Municipality {
  municipalityId: number;
  code: string;
  name: string;
  nameEnglish: string;
  country: string;
  region: string;
  contactEmail?: string;
  contactPhone?: string;
  address?: string;
  minLatitude: number;
  maxLatitude: number;
  minLongitude: number;
  maxLongitude: number;
  defaultStartTime: string;
  defaultEndTime: string;
  defaultGraceMinutes: number;
  maxAcceptableAccuracyMeters: number;
  licenseExpiresAt: string;
  isDeleted: boolean;
  userCount?: number;
  zoneCount?: number;
}

// Municipality APIs
export async function getMunicipalities(): Promise<Municipality[]> {
  const response = await apiClient.get<{ data: Municipality[] }>("/municipality");
  return response.data.data;
}

export async function getMunicipalityById(id: number): Promise<Municipality> {
  const response = await apiClient.get<{ data: Municipality }>(`/municipality/${id}`);
  return response.data.data;
}

export async function createMunicipality(data: Partial<Municipality>): Promise<Municipality> {
  const response = await apiClient.post<{ data: Municipality }>("/municipality", data);
  return response.data.data;
}

export async function updateMunicipality(id: number, data: Partial<Municipality>): Promise<Municipality> {
  const response = await apiClient.put<{ data: Municipality }>(`/municipality/${id}`, data);
  return response.data.data;
}

export async function deleteMunicipality(id: number): Promise<void> {
  await apiClient.delete(`/municipality/${id}`);
}
