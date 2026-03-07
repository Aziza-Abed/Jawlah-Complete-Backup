import { useEffect, useRef, useCallback } from "react";
import * as signalR from "@microsoft/signalr";
import { STORAGE_KEYS } from "../constants/storageKeys";

const API_BASE = import.meta.env.VITE_API_BASE_URL || "";
// API base is like http://localhost:5000/api — hub is at http://localhost:5000/hubs/tracking
const HUB_URL = API_BASE.replace(/\/api\/?$/, "") + "/hubs/tracking";

export type LiveLocationUpdate = {
  userId: number;
  userName: string;
  latitude: number;
  longitude: number;
  timestamp: string;
};

export type UserStatusUpdate = {
  userId: number;
  userName: string;
  status: "online" | "offline";
};

// Hook that connects to the SignalR TrackingHub and calls callbacks when live data arrives
export function useTrackingHub(callbacks: {
  onLocationUpdate?: (update: LiveLocationUpdate) => void;
  onUserStatus?: (update: UserStatusUpdate) => void;
}) {
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const callbacksRef = useRef(callbacks);
  callbacksRef.current = callbacks;

  const connect = useCallback(() => {
    const token = localStorage.getItem(STORAGE_KEYS.TOKEN);
    if (!token) return;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL, {
        accessTokenFactory: () => localStorage.getItem(STORAGE_KEYS.TOKEN) || "",
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    // Listen for real-time location updates from workers
    connection.on(
      "ReceiveLocationUpdate",
      (userId: number, userName: string, latitude: number, longitude: number, timestamp: string) => {
        callbacksRef.current.onLocationUpdate?.({
          userId,
          userName,
          latitude,
          longitude,
          timestamp: typeof timestamp === "string" ? timestamp : new Date(timestamp).toISOString(),
        });
      }
    );

    // Listen for worker online/offline status
    connection.on(
      "ReceiveUserStatus",
      (userId: number, userName: string, status: string) => {
        const safeStatus = status === "online" ? "online" : "offline";
        callbacksRef.current.onUserStatus?.({
          userId,
          userName,
          status: safeStatus,
        });
      }
    );

    connection
      .start()
      .then(() => {
        // Join supervisors group to receive worker updates
        connection.invoke("JoinSupervisorsGroup").catch(() => {});
      })
      .catch(() => {});

    connectionRef.current = connection;
  }, []);

  useEffect(() => {
    connect();

    return () => {
      connectionRef.current?.stop();
      connectionRef.current = null;
    };
  }, [connect]);
}
