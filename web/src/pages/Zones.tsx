import React, { useEffect, useRef, useState } from "react";
import { MapContainer, TileLayer, Marker, Popup, useMap } from "react-leaflet";
import L from "leaflet";
import { getWorkerLocations } from "../api/tracking";
import type { WorkerLocation as ApiWorkerLocation } from "../types/tracking";

type WorkerLocation = {
  id: string;
  name: string;
  lat: number;
  lng: number;
  status: "online" | "offline";
  timestamp?: string;
};

// Fix Leaflet default marker icons (Vite build)
const markerIcon = new L.Icon({
  iconUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png",
  iconRetinaUrl:
    "https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png",
  shadowUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png",
  iconSize: [25, 41],
  iconAnchor: [12, 41],
});

function FlyAndOpenPopup({
  worker,
  markerRefs,
}: {
  worker: WorkerLocation | null;
  markerRefs: React.MutableRefObject<Record<string, L.Marker | null>>;
}) {
  const map = useMap();

  useEffect(() => {
    if (!worker) return;

    map.flyTo([worker.lat, worker.lng], 16, { duration: 0.8 });

    // Open popup after the map starts moving (small delay is more reliable)
    window.setTimeout(() => {
      const marker = markerRefs.current[worker.id];
      marker?.openPopup();
    }, 250);
  }, [worker, map, markerRefs]);

  return null;
}

export default function Zones() {
  const [workers, setWorkers] = useState<WorkerLocation[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const center: [number, number] = [31.907, 35.215];

  const [selectedId, setSelectedId] = useState<string>("");

  const selectedWorker = workers.find((w) => w.id === selectedId) || null;

  const markerRefs = useRef<Record<string, L.Marker | null>>({});

  // Fetch worker locations
  const fetchLocations = async () => {
    try {
      const data = await getWorkerLocations();
      const mapped: WorkerLocation[] = data.map((w: ApiWorkerLocation) => ({
        id: w.userId.toString(),
        name: w.fullName,
        lat: w.latitude,
        lng: w.longitude,
        status: w.isOnline ? "online" : "offline",
        timestamp: w.timestamp,
      }));
      setWorkers(mapped);
      setError("");
    } catch (err) {
      setError("فشل في تحميل مواقع العمال");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchLocations();

    // Poll every 30 seconds for updates
    const interval = setInterval(fetchLocations, 30000);
    return () => clearInterval(interval);
  }, []);

  if (loading) {
    return (
      <div className="h-full w-full bg-[#D9D9D9] flex items-center justify-center">
        <div className="text-[#2F2F2F]">جاري التحميل...</div>
      </div>
    );
  }

  return (
    <div className="h-full w-full bg-[#D9D9D9] overflow-auto">
      <div className="p-4 sm:p-6 md:p-8">
        <div className="max-w-[980px] mx-auto">
          <div className="flex items-center justify-between mb-4">
            <h1 className="text-right font-sans font-semibold text-[20px] sm:text-[22px] text-[#2F2F2F]">
              الخريطة الحية
            </h1>
          </div>

          {error && (
            <div className="mb-4 p-3 bg-red-100 text-red-700 rounded-[10px] text-right">
              {error}
            </div>
          )}

          <div className="bg-[#F3F1ED] rounded-[16px] border border-[#0EA5E9] shadow-[0_2px_0_rgba(0,0,0,0.08)] p-4 sm:p-5">
            <div className="flex flex-col md:flex-row items-stretch md:items-center gap-3 md:gap-4 mb-4">
              <div className="text-right font-sans font-semibold text-[#2F2F2F]">
                مواقع العمال على الخريطة ({workers.length})
              </div>

              <div className="flex-1" />

              <div className="w-full md:w-[320px]">
                <select
                  value={selectedId}
                  onChange={(e) => setSelectedId(e.target.value)}
                  className="w-full h-[44px] rounded-[10px] bg-white border border-black/10 px-4 text-right outline-none focus:ring-2 focus:ring-black/10"
                >
                  <option value="">اختر اسم العامل...</option>
                  {workers.map((w) => (
                    <option key={w.id} value={w.id}>
                      {w.name} ({w.status === "online" ? "متصل" : "غير متصل"})
                    </option>
                  ))}
                </select>
              </div>
            </div>

            <div className="rounded-[12px] overflow-hidden border border-[#0EA5E9] bg-white">
              <MapContainer
                center={center}
                zoom={13}
                className="w-full h-[260px] sm:h-[340px] md:h-[420px]"
              >
                <TileLayer
                  attribution='&copy; OpenStreetMap contributors'
                  url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
                />

                <FlyAndOpenPopup worker={selectedWorker} markerRefs={markerRefs} />

                {workers.map((w) => (
                  <Marker
                    key={w.id}
                    position={[w.lat, w.lng]}
                    icon={markerIcon}
                    ref={(ref) => {
                      markerRefs.current[w.id] = (ref as unknown as L.Marker) || null;
                    }}
                  >
                    <Popup>
                      <div dir="rtl" className="text-right font-sans">
                        <div className="font-semibold text-[#2F2F2F]">
                          {w.name}
                        </div>
                        <div className="text-[12px] text-[#6B7280] mt-1">
                          الحالة: {w.status === "online" ? "متصل" : "غير متصل"}
                        </div>
                        <div className="text-[12px] text-[#6B7280]">
                          {w.lat.toFixed(4)}, {w.lng.toFixed(4)}
                        </div>
                        {w.timestamp && (
                          <div className="text-[12px] text-[#6B7280]">
                            آخر تحديث: {new Date(w.timestamp).toLocaleTimeString("ar-EG")}
                          </div>
                        )}
                      </div>
                    </Popup>
                  </Marker>
                ))}
              </MapContainer>
            </div>

            <div className="mt-3 text-right text-[12px] text-[#6B7280]">
              يتم تحديث المواقع تلقائياً كل 30 ثانية
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
