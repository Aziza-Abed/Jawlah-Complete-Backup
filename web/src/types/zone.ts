// Zone types matching backend DTOs

export type ZoneResponse = {
  zoneId: number;
  zoneName: string;
  zoneCode: string;
  description?: string;
  centerLatitude: number;
  centerLongitude: number;
  areaSquareMeters: number;
  district?: string;
  version: number;
  isActive: boolean;
  createdAt: string;
};
