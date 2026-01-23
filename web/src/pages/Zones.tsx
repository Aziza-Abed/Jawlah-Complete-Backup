import React, { useEffect, useRef, useState } from "react";
import { MapContainer, TileLayer, Marker, Popup, useMap, GeoJSON } from "react-leaflet";
import L from "leaflet";
import "leaflet/dist/leaflet.css";
import { getWorkerLocations } from "../api/tracking";
import { getZonesMapData } from "../api/zones";
import { getMyWorkers } from "../api/users";
import type { WorkerLocation as ApiWorkerLocation } from "../types/tracking";
import type { UserResponse } from "../types/user";
import { Battery, BatteryLow, BatteryMedium, BatteryFull, Wifi, WifiOff, RefreshCcw, MapPin, Clock, Navigation } from "lucide-react";
import { useMunicipality } from "../contexts/MunicipalityContext";

type WorkerLocation = {
  id: string;
  name: string;
  lat: number;
  lng: number;
  status: "online" | "offline";
  timestamp?: string;
  zoneName?: string;
  batteryLevel?: number;
  isLowBattery?: boolean;
  accuracy?: number;
  speed?: number;
};

// Custom marker icons
const OnlineIcon = L.divIcon({
  className: "custom-marker",
  html: `<div class="relative flex items-center justify-center w-8 h-8">
    <div class="absolute w-full h-full bg-green-500 rounded-full animate-ping opacity-50"></div>
    <div class="relative w-5 h-5 bg-green-500 rounded-full border-2 border-white shadow-lg"></div>
  </div>`,
  iconSize: [32, 32],
  iconAnchor: [16, 16]
});

const OfflineIcon = L.divIcon({
  className: "custom-marker",
  html: `<div class="w-5 h-5 bg-gray-400 rounded-full border-2 border-white shadow-lg"></div>`,
  iconSize: [20, 20],
  iconAnchor: [10, 10]
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

// Battery icon component
function BatteryIcon({ level }: { level?: number }) {
  if (!level || level <= 0) return <Battery size={14} className="text-gray-400" />;
  if (level <= 20) return <BatteryLow size={14} className="text-red-500" />;
  if (level <= 50) return <BatteryMedium size={14} className="text-yellow-500" />;
  return <BatteryFull size={14} className="text-green-500" />;
}

export default function Zones() {
  const [workers, setWorkers] = useState<WorkerLocation[]>([]);
  const [zonesData, setZonesData] = useState<any>(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState("");
  const [lastUpdate, setLastUpdate] = useState<Date>(new Date());

  // Get map center from municipality settings
  const { mapCenter: center } = useMunicipality();

  const [selectedId, setSelectedId] = useState<string>("");
  const [showPanel, setShowPanel] = useState(true);

  const selectedWorker = workers.find((w) => w.id === selectedId) || null;

  const markerRefs = useRef<Record<string, L.Marker | null>>({});

  // Fetch map data
  const fetchMapData = async (isRefresh = false) => {
    if (isRefresh) setRefreshing(true);

    try {
      const [locations, zones, myWorkers] = await Promise.all([
        getWorkerLocations(),
        getZonesMapData(),
        getMyWorkers().catch(() => []) // Fallback if not supervisor
      ]);

      // Build user details map for battery info enrichment
      const userMap: Record<string, UserResponse> = {};
      myWorkers.forEach((u: UserResponse) => {
        userMap[u.userId.toString()] = u;
      });

      const mappedWorkers: WorkerLocation[] = locations.map((w: ApiWorkerLocation) => {
        const userInfo = userMap[w.userId.toString()];
        return {
          id: w.userId.toString(),
          name: w.fullName,
          lat: w.latitude,
          lng: w.longitude,
          status: w.isOnline ? "online" : "offline",
          timestamp: w.timestamp,
          zoneName: (w as any).zoneName || "غير محدد",
          batteryLevel: userInfo?.lastBatteryLevel,
          isLowBattery: userInfo?.isLowBattery,
          accuracy: w.accuracy,
          speed: w.speed,
        };
      });

      setWorkers(mappedWorkers);
      setZonesData(zones);
      setError("");
      setLastUpdate(new Date());
    } catch (err) {
      console.error("Map fetch error:", err);
      setError("فشل في تحميل بيانات الخريطة");
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  useEffect(() => {
    fetchMapData();

    // Poll every 10 seconds for updates (Live feel)
    const interval = setInterval(() => fetchMapData(true), 10000);
    return () => clearInterval(interval);
  }, []);

  const onlineCount = workers.filter(w => w.status === "online").length;

  if (loading) {
    return (
      <div className="h-full w-full bg-background flex items-center justify-center">
        <div className="flex flex-col items-center gap-4">
          <div className="w-12 h-12 border-4 border-[#60778E]/30 border-t-[#60778E] rounded-full animate-spin"></div>
          <p className="text-[#2F2F2F] font-medium">جاري تحميل الخريطة الحية...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="h-full w-full bg-background flex flex-col lg:flex-row overflow-hidden">
      {/* Workers Side Panel */}
      <div className={`${showPanel ? 'w-full lg:w-80 xl:w-96' : 'w-0'} bg-[#F3F1ED] border-l border-[#60778E]/20 flex flex-col transition-all duration-300 overflow-hidden order-2 lg:order-1`}>
        {/* Panel Header */}
        <div className="p-4 border-b border-[#60778E]/10 bg-[#60778E]/5">
          <div className="flex items-center justify-between mb-2">
            <button
              onClick={() => fetchMapData(true)}
              disabled={refreshing}
              className={`p-2 rounded-lg hover:bg-[#60778E]/10 transition-colors text-[#6B7280] ${refreshing ? 'animate-spin' : ''}`}
              title="تحديث البيانات"
            >
              <RefreshCcw size={18} />
            </button>
            <div className="text-right">
              <h2 className="font-bold text-[#2F2F2F] text-lg">الخريطة الحية</h2>
              <p className="text-[10px] text-[#6B7280] font-medium flex items-center justify-end gap-1 mt-1">
                <span className="w-1.5 h-1.5 rounded-full bg-green-500 animate-pulse"></span>
                آخر تحديث: {lastUpdate.toLocaleTimeString('ar-EG')}
              </p>
            </div>
          </div>

          {/* Stats */}
          <div className="grid grid-cols-2 gap-2 mt-3">
            <div className="text-center p-2 bg-white rounded-lg border border-[#60778E]/10">
              <p className="text-xl font-bold text-green-600">{onlineCount}</p>
              <p className="text-[9px] font-bold text-[#6B7280]">متصل الآن</p>
            </div>
            <div className="text-center p-2 bg-white rounded-lg border border-[#60778E]/10">
              <p className="text-xl font-bold text-[#2F2F2F]">{workers.length}</p>
              <p className="text-[9px] font-bold text-[#6B7280]">إجمالي العمال</p>
            </div>
          </div>
        </div>

        {/* Workers List */}
        <div className="flex-1 overflow-y-auto p-3 space-y-2">
          {workers.length === 0 ? (
            <div className="p-8 text-center text-[#6B7280] font-sans flex flex-col items-center justify-center opacity-50">
              <MapPin size={32} className="mb-2" />
              لا يوجد عمال متصلين حالياً
            </div>
          ) : (
            workers.map(w => (
              <div
                key={w.id}
                onClick={() => setSelectedId(w.id)}
                className={`bg-white hover:bg-[#60778E]/5 p-3 rounded-xl border transition-all cursor-pointer ${selectedId === w.id ? 'border-[#60778E] shadow-md' : 'border-[#60778E]/10'}`}
              >
                <div className="flex items-start justify-between mb-2">
                  <div className="flex flex-col items-start gap-1">
                    <span className={`flex items-center gap-1 text-[10px] font-bold ${w.status === 'online' ? 'text-green-600' : 'text-[#6B7280]'}`}>
                      {w.status === 'online' ? <Wifi size={12} /> : <WifiOff size={12} />}
                      {w.status === 'online' ? 'متصل' : 'غير متصل'}
                    </span>
                    {w.batteryLevel !== undefined && (
                      <div className="flex items-center gap-1.5 bg-[#F3F1ED] px-2 py-0.5 rounded-lg border border-[#60778E]/10 mt-1">
                        <span className="text-[10px] font-bold text-[#6B7280] font-mono">{w.batteryLevel}%</span>
                        <BatteryIcon level={w.batteryLevel} />
                      </div>
                    )}
                  </div>
                  <div className="text-right">
                    <h3 className="font-bold text-[#2F2F2F] text-sm">{w.name}</h3>
                    <p className="text-[10px] text-[#6B7280] font-medium mt-0.5">{w.zoneName || 'خارج المناطق'}</p>
                  </div>
                </div>

                {w.isLowBattery && (
                  <div className="mt-2 flex items-center justify-end gap-1.5 text-[9px] font-bold text-red-600 bg-red-50 py-1.5 px-2 rounded-lg border border-red-200">
                    <span>تحذير: طاقة منخفضة</span>
                    <BatteryLow size={10} />
                  </div>
                )}

                <div className="mt-2 flex items-center justify-between border-t border-[#60778E]/5 pt-2">
                  {w.accuracy && (
                    <div className="flex items-center gap-1 text-[9px] text-[#6B7280]">
                      <Navigation size={10} className="opacity-50" />
                      <span>الدقة: {Math.round(w.accuracy)}م</span>
                    </div>
                  )}
                  {w.timestamp && (
                    <div className="flex items-center gap-1 text-[9px] text-[#6B7280] font-mono">
                      <span>{new Date(w.timestamp).toLocaleTimeString('ar-EG', { hour: '2-digit', minute: '2-digit' })}</span>
                      <Clock size={10} className="opacity-50" />
                    </div>
                  )}
                </div>
              </div>
            ))
          )}
        </div>
      </div>

      {/* Map Area */}
      <div className="flex-1 relative order-1 lg:order-2 min-h-[500px]">
        {error && (
          <div className="absolute top-4 right-4 left-4 z-[1000] p-3 bg-red-100 text-red-700 rounded-lg text-right shadow-lg">
            {error}
          </div>
        )}

        <MapContainer
          center={center}
          zoom={13}
          className="absolute inset-0"
          style={{ height: '100%', width: '100%' }}
          zoomControl={false}
        >
          <TileLayer
            attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
            url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
          />

          {zonesData && (
            <GeoJSON
              data={zonesData}
              style={{
                fillColor: "#60778E",
                weight: 2,
                opacity: 0.8,
                color: "#60778E",
                fillOpacity: 0.1,
              }}
              onEachFeature={(feature, layer) => {
                if (feature.properties && feature.properties.zoneName) {
                  layer.bindPopup(`<div class="text-right font-sans p-2"><b>${feature.properties.zoneName}</b></div>`);
                }
              }}
            />
          )}

          <FlyAndOpenPopup worker={selectedWorker} markerRefs={markerRefs} />

          {workers.map((w) => (
            <Marker
              key={w.id}
              position={[w.lat, w.lng]}
              icon={w.status === 'online' ? OnlineIcon : OfflineIcon}
              ref={(ref) => {
                markerRefs.current[w.id] = (ref as unknown as L.Marker) || null;
              }}
            >
              <Popup>
                <div dir="rtl" className="text-right font-sans min-w-[180px] p-1">
                  <div className="font-bold text-[#2F2F2F] text-sm border-b border-gray-200 pb-2 mb-2">
                    {w.name}
                  </div>
                  <div className="space-y-1.5">
                    <div className="flex items-center justify-end gap-2">
                      <span className={`text-[11px] font-bold ${w.status === 'online' ? 'text-green-600' : 'text-gray-500'}`}>
                        {w.status === 'online' ? 'متصل' : 'غير متصل'}
                      </span>
                      <div className={`w-2 h-2 rounded-full ${w.status === 'online' ? 'bg-green-500' : 'bg-gray-400'}`}></div>
                    </div>
                    <div className="text-[11px] text-[#6B7280]">
                      <span className="font-medium">المنطقة:</span> {w.zoneName || 'غير محدد'}
                    </div>
                    {w.batteryLevel !== undefined && (
                      <div className="text-[11px] text-[#6B7280] flex items-center justify-end gap-1">
                        <span>{w.batteryLevel}%</span>
                        <span className="font-medium">البطارية:</span>
                        <BatteryIcon level={w.batteryLevel} />
                      </div>
                    )}
                    {w.speed !== undefined && w.speed > 0 && (
                      <div className="text-[11px] text-[#6B7280]">
                        <span className="font-medium">السرعة:</span> {Math.round(w.speed)} كم/س
                      </div>
                    )}
                    <div className="text-[10px] text-[#9CA3AF] font-mono mt-2 pt-2 border-t border-gray-100">
                      {w.lat.toFixed(5)}, {w.lng.toFixed(5)}
                    </div>
                    {w.timestamp && (
                      <div className="text-[10px] text-[#9CA3AF]">
                        {new Date(w.timestamp).toLocaleTimeString('ar-EG')}
                      </div>
                    )}
                  </div>
                </div>
              </Popup>
            </Marker>
          ))}
        </MapContainer>

        {/* Map Overlay - Stats */}
        <div className="absolute top-4 right-4 z-[1000] hidden lg:block">
          <div className="bg-white/90 backdrop-blur-sm rounded-xl shadow-lg border border-[#60778E]/20 p-4 min-w-[200px]">
            <div className="flex items-center justify-between mb-3">
              <div className="flex items-center gap-2 bg-green-100 text-green-700 px-2 py-1 rounded-full border border-green-200">
                <span className="w-1.5 h-1.5 bg-green-500 rounded-full animate-pulse"></span>
                <span className="text-[10px] font-bold">مباشر</span>
              </div>
              <span className="text-sm font-bold text-[#2F2F2F]">ملخص التغطية</span>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div className="text-center p-2 bg-[#F3F1ED] rounded-lg">
                <p className="text-xl font-bold text-green-600">{onlineCount}</p>
                <p className="text-[9px] font-bold text-[#6B7280]">نشط الآن</p>
              </div>
              <div className="text-center p-2 bg-[#F3F1ED] rounded-lg">
                <p className="text-xl font-bold text-[#2F2F2F]">{workers.length}</p>
                <p className="text-[9px] font-bold text-[#6B7280]">الإجمالي</p>
              </div>
            </div>
          </div>
        </div>

        {/* Toggle Panel Button (Mobile) */}
        <button
          onClick={() => setShowPanel(!showPanel)}
          className="absolute bottom-4 right-4 z-[1000] lg:hidden bg-white rounded-full p-3 shadow-lg border border-[#60778E]/20"
        >
          <MapPin size={20} className="text-[#60778E]" />
        </button>
      </div>
    </div>
  );
}
