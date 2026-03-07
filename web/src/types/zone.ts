// zone types

export type ZoneResponse = {
  zoneId: number;
  zoneName: string;
  zoneCode: string;
  municipalityId?: number;
  description?: string;
  centerLatitude: number;
  centerLongitude: number;
  areaSquareMeters: number;
  district?: string;
  version: number;
  isActive: boolean;
  // GeoJSON boundary for map display
  boundaryGeoJson?: string;
  // GIS source type: "Quarters", "Borders", "Blocks", or undefined for manual zones
  zoneType?: string;
  createdAt: string;
};
