// Tracking types matching backend response

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
