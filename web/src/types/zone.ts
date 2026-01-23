// Zone types matching backend DTOs

export type ZoneResponse = {
  zoneId: number;
  zoneName: string;
  zoneCode: string;
  municipalityId: number;
  description?: string;
  centerLatitude: number;
  centerLongitude: number;
  areaSquareMeters: number;
  district?: string;
  version: number;
  isActive: boolean;
  geometryJson?: string;
  createdAt: string;
};
