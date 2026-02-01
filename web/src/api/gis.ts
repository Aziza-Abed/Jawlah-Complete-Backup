import { apiClient } from "./client";

// Types for GIS files
export interface GisFileDto {
  gisFileId: number;
  fileType: string;
  originalFileName: string;
  fileSize: number;
  fileSizeFormatted: string;
  isActive: boolean;
  featuresCount: number;
  uploadedByName: string | null;
  uploadedAt: string;
  lastImportedAt: string | null;
  notes: string | null;
}

export interface GisFilesStatus {
  quarters: GisFileDto | null;
  borders: GisFileDto | null;
  blocks: GisFileDto | null;
  hasAllRequiredFiles: boolean;
}

export interface ZonesSummary {
  totalZones: number;
  byType: { type: string; count: number }[];
}

export type GisFileType = "Quarters" | "Borders" | "Blocks";

// Get current status of all GIS files
export async function getGisFilesStatus(): Promise<GisFilesStatus> {
  const response = await apiClient.get<{ data: GisFilesStatus }>("/gis/status");
  return response.data.data;
}

// Get all uploaded GIS files (history)
export async function getAllGisFiles(): Promise<GisFileDto[]> {
  const response = await apiClient.get<{ data: GisFileDto[] }>("/gis/files");
  return response.data.data;
}

// Upload a new GIS file
export async function uploadGisFile(
  file: File,
  fileType: GisFileType,
  options?: {
    notes?: string;
    autoImport?: boolean;
  }
): Promise<{ success: boolean; message: string; data?: GisFileDto; warning?: string }> {
  const formData = new FormData();
  formData.append("file", file);
  formData.append("fileType", fileType);

  if (options?.notes) {
    formData.append("notes", options.notes);
  }

  if (options?.autoImport !== undefined) {
    formData.append("autoImport", options.autoImport.toString());
  }

  const response = await apiClient.post<{
    success: boolean;
    message: string;
    data?: GisFileDto;
    warning?: string;
  }>("/gis/upload", formData, {
    headers: {
      "Content-Type": "multipart/form-data",
    },
  });

  return response.data;
}

// Import a previously uploaded GIS file to zones
export async function importGisFile(fileId: number): Promise<{ success: boolean; message: string }> {
  const response = await apiClient.post<{ success: boolean; message: string }>(
    `/gis/import/${fileId}`
  );
  return response.data;
}

// Delete a GIS file
export async function deleteGisFile(fileId: number): Promise<{ success: boolean; message: string }> {
  const response = await apiClient.delete<{ success: boolean; message: string }>(
    `/gis/files/${fileId}`
  );
  return response.data;
}

// Get download URL for a GIS file
export function getGisFileDownloadUrl(fileId: number): string {
  return `/api/gis/files/${fileId}/download`;
}

// Get zones summary
export async function getZonesSummary(): Promise<ZonesSummary> {
  const response = await apiClient.get<{ data: ZonesSummary }>("/gis/zones-summary");
  return response.data.data;
}
