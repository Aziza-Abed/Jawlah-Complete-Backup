// tracking types

export type WorkerLocation = {
  userId: number;
  fullName: string;
  username?: string;
  latitude: number;
  longitude: number;
  speed?: number;
  accuracy?: number;
  timestamp: string;
  isOnline: boolean;
  status: "Online" | "Offline";
  zoneName?: string;
};

// Backend GET /tracking/history/{userId} returns LocationUpdateDto[]
export type LocationHistoryPoint = {
  latitude: number;
  longitude: number;
  speed?: number;
  accuracy?: number;
  heading?: number;
  timestamp: string;
  zoneName?: string;
};
