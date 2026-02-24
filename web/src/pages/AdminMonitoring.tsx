import { useEffect, useRef, useState, useCallback } from "react";
import { getWorkerLocations } from "../api/tracking";
import { getUsers } from "../api/users";
import type { WorkerLocation } from "../types/tracking";
import type { UserResponse } from "../types/user";
import { MapContainer, TileLayer, Marker, Popup, useMap } from "react-leaflet";
import L from "leaflet";
import "leaflet/dist/leaflet.css";
import {
  BatteryLow,
  BatteryMedium,
  BatteryFull,
  Signal,
  Wifi,
  WifiOff,
  Map as MapIcon,
  RefreshCcw,
  AlertCircle,
  Navigation,
  Radio
} from "lucide-react";
import { useMunicipality } from "../contexts/MunicipalityContext";
import { useTrackingHub, type LiveLocationUpdate, type UserStatusUpdate } from "../hooks/useTrackingHub";

// Fix Leaflet icons
import markerIcon from "leaflet/dist/images/marker-icon.png";
import markerShadow from "leaflet/dist/images/marker-shadow.png";

const DefaultIcon = L.icon({
  iconUrl: markerIcon,
  shadowUrl: markerShadow,
  iconSize: [25, 41],
  iconAnchor: [12, 41],
});
L.Marker.prototype.options.icon = DefaultIcon;

const OnlineIcon = L.divIcon({
  className: "custom-marker",
  html: `<div class="relative flex items-center justify-center w-6 h-6">
    <div class="absolute w-full h-full bg-[#8FA36A] rounded-full animate-ping opacity-75"></div>
    <div class="relative w-4 h-4 bg-[#8FA36A] rounded-full border-2 border-white shadow-lg"></div>
  </div>`,
  iconSize: [24, 24],
  iconAnchor: [12, 12]
});

const OfflineIcon = L.divIcon({
  className: "custom-marker",
  html: `<div class="w-4 h-4 bg-[#6B7280] rounded-full border-2 border-white shadow-lg"></div>`,
  iconSize: [16, 16],
  iconAnchor: [8, 8]
});

function FlyToWorker({
  worker,
  markerRefs,
}: {
  worker: WorkerLocation | null;
  markerRefs: React.MutableRefObject<Record<number, L.Marker | null>>;
}) {
  const map = useMap();

  useEffect(() => {
    if (!worker) return;
    map.flyTo([worker.latitude, worker.longitude], 16, { duration: 0.8 });
    window.setTimeout(() => {
      markerRefs.current[worker.userId]?.openPopup();
    }, 250);
  }, [worker, map, markerRefs]);

  return null;
}

export default function AdminMonitoring() {
  const [locations, setLocations] = useState<WorkerLocation[]>([]);
  const [userDetails, setUserDetails] = useState<Record<number, UserResponse>>({});
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [lastUpdate, setLastUpdate] = useState(new Date());
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const markerRefs = useRef<Record<number, L.Marker | null>>({});

  // Get map center from municipality settings
  const { mapCenter } = useMunicipality();

  // SignalR: receive live location pushes from workers
  useTrackingHub({
    onLocationUpdate: useCallback((update: LiveLocationUpdate) => {
      setLocations((prev) => {
        const idx = prev.findIndex((l) => l.userId === update.userId);
        if (idx === -1) return prev;
        const copy = [...prev];
        copy[idx] = {
          ...copy[idx],
          latitude: update.latitude,
          longitude: update.longitude,
          timestamp: update.timestamp,
          isOnline: true,
          status: "Online",
        };
        return copy;
      });
      setLastUpdate(new Date());
    }, []),
    onUserStatus: useCallback((update: UserStatusUpdate) => {
      setLocations((prev) => {
        const idx = prev.findIndex((l) => l.userId === update.userId);
        if (idx === -1) return prev;
        const copy = [...prev];
        copy[idx] = {
          ...copy[idx],
          isOnline: update.status === "online",
          status: update.status === "online" ? "Online" : "Offline",
        };
        return copy;
      });
    }, []),
  });

  useEffect(() => {
    fetchData();
    // Fallback poll every 30s for battery info & new workers (SignalR handles locations)
    const interval = setInterval(() => fetchData(true), 30000);
    return () => clearInterval(interval);
  }, []);

  const fetchData = async (isRefresh = false) => {
    if (isRefresh) setRefreshing(true);
    else setLoading(true);

    try {
      const [locs, usersData] = await Promise.all([
        getWorkerLocations(),
        getUsers(1, 100) // Adjust count as needed
      ]);

      setLocations(locs);
      const userMap: Record<number, UserResponse> = {};
      usersData.items.forEach(u => {
          userMap[u.userId] = u;
      });
      setUserDetails(userMap);
      setLastUpdate(new Date());
    } catch (err) {
      console.error("Failed to fetch monitoring data", err);
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  const getBatteryIcon = (level: number = 0) => {
    // Battery thresholds: >50% green, 20-50% yellow/blue, <20% red
    if (level > 50) return <BatteryFull size={14} className="text-[#8FA36A]" />;
    if (level >= 20) return <BatteryMedium size={14} className="text-[#D4A843]" />;
    return <BatteryLow size={14} className="text-[#C86E5D]" />;
  };

  if (loading) {
    return (
      <div className="h-full w-full bg-[#F3F1ED] flex items-center justify-center">
        <div className="flex flex-col items-center gap-4">
          <div className="w-12 h-12 border-4 border-[#7895B2]/30 border-t-[#7895B2] rounded-full animate-spin"></div>
          <p className="text-[#6B7280] font-medium">جاري الاتصال بالنظام الميداني...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="h-full w-full flex flex-col md:flex-row overflow-hidden relative bg-[#F3F1ED]">
      {/* Side Panel */}
      <div className="w-full md:w-80 lg:w-96 bg-white/95 border-l border-[#E5E7EB] flex flex-col h-1/3 md:h-full order-2 md:order-1 z-10 shadow-[0_4px_20px_rgba(0,0,0,0.04)]">
        <div className="p-5 border-b border-[#E5E7EB] bg-[#7895B2]/5">
          <div className="flex items-center justify-between mb-2">
            <button
              onClick={() => fetchData(true)}
              disabled={refreshing}
              className={`p-2 rounded-[10px] hover:bg-[#7895B2]/10 transition-colors text-[#6B7280] ${refreshing ? 'animate-spin' : ''}`}
              title="تحديث البيانات"
            >
              <RefreshCcw size={18} />
            </button>
            <div className="text-right">
              <h2 className="font-bold text-[#2F2F2F] text-lg">مراقبة النظام</h2>
              <p className="text-[11px] text-[#6B7280] font-medium flex items-center justify-end gap-1 mt-1">
                <span className="w-1.5 h-1.5 rounded-full bg-[#8FA36A] animate-pulse"></span>
                آخر تحديث: {lastUpdate.toLocaleTimeString('ar-EG')}
              </p>
            </div>
          </div>
        </div>

        <div className="flex-1 overflow-y-auto p-4 space-y-3">
          {locations.map(loc => {
            const user = userDetails[loc.userId];
            return (
              <div key={loc.userId} onClick={() => setSelectedId(loc.userId)} className={`bg-[#F3F1ED] hover:bg-[#E8E6E2] p-4 rounded-[12px] border transition-all group cursor-pointer ${selectedId === loc.userId ? 'border-[#7895B2] shadow-md' : 'border-[#E5E7EB]'}`}>
                <div className="flex items-start justify-between mb-3">
                  <div className="flex flex-col items-start gap-1">
                    <span className={`flex items-center gap-1 text-[10px] font-bold ${loc.status === 'Online' ? 'text-[#8FA36A]' : 'text-[#6B7280]'}`}>
                      {loc.status === 'Online' ? <Wifi size={12} /> : <WifiOff size={12} />}
                      {loc.status === 'Online' ? 'نشط' : 'غير متصل'}
                    </span>
                    <div className="flex items-center gap-1.5 bg-white/50 px-2 py-0.5 rounded-[8px] border border-[#E5E7EB] mt-1">
                      <span className="text-[10px] font-bold text-[#6B7280] font-sans">{user?.lastBatteryLevel || 0}%</span>
                      {getBatteryIcon(user?.lastBatteryLevel)}
                    </div>
                  </div>
                  <div className="flex flex-col items-end">
                    <h3 className="font-bold text-[#2F2F2F] text-sm">{loc.fullName}</h3>
                    <p className="text-[10px] text-[#6B7280] font-medium mt-0.5">{loc.zoneName || 'خارج المناطق'}</p>
                  </div>
                </div>

                {user?.isLowBattery && (
                  <div className="mt-2 flex items-center justify-end gap-1.5 text-[9px] font-bold text-[#C86E5D] bg-[#C86E5D]/10 py-1.5 px-2 rounded-[8px] border border-[#C86E5D]/20">
                    <span>تحذير: طاقة منخفضة</span>
                    <AlertCircle size={10} />
                  </div>
                )}

                <div className="mt-3 flex items-center justify-between border-t border-[#E5E7EB] pt-2">
                  <div className="flex items-center gap-1 text-[9px] text-[#6B7280]">
                    <Radio size={10} className="text-[#7895B2] opacity-50" />
                    <span>الدقة: {Math.round(loc.accuracy || 0)}م</span>
                  </div>
                  <div className="flex items-center gap-1 text-[9px] text-[#6B7280] font-sans">
                    <span>{new Date(loc.timestamp).toLocaleTimeString('ar-EG', { hour: '2-digit', minute: '2-digit' })}</span>
                    <Navigation size={10} className="opacity-50" />
                  </div>
                </div>
              </div>
            );
          })}

          {locations.length === 0 && (
            <div className="p-8 text-center text-[#6B7280] flex flex-col items-center justify-center opacity-50 h-[200px]">
              <Signal size={32} className="mb-2" />
              لا يوجد عمال متصلين حالياً
            </div>
          )}
        </div>
      </div>

      {/* Map Content */}
      <div className="flex-1 relative order-1 md:order-2 h-2/3 md:h-full bg-white">
        <MapContainer center={mapCenter} zoom={12} zoomControl={false} className="h-full w-full z-0">
          <TileLayer
            attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
            url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
          />
          {locations.map(loc => (
            <Marker
              key={loc.userId}
              position={[loc.latitude, loc.longitude]}
              icon={loc.status === 'Online' ? OnlineIcon : OfflineIcon}
              ref={(ref) => {
                markerRefs.current[loc.userId] = (ref as unknown as L.Marker) || null;
              }}
            >
              <Popup className="glass-popup">
                <div className="text-right font-sans min-w-[150px]">
                  <h4 className="font-bold border-b border-gray-200 pb-2 mb-2 text-slate-800">{loc.fullName}</h4>
                  <div className="flex items-center justify-end gap-2 mb-1">
                    <span className={`text-xs font-bold ${loc.status === 'Online' ? 'text-[#8FA36A]' : 'text-[#6B7280]'}`}>{loc.status === 'Online' ? 'متصل' : 'أوفلاين'}</span>
                    <div className={`w-2 h-2 rounded-full ${loc.status === 'Online' ? 'bg-[#8FA36A]' : 'bg-[#6B7280]'}`}></div>
                  </div>
                  <p className="text-xs text-[#6B7280] mb-1">السرعة: <span className="font-sans text-[#2F2F2F]">{Math.round(loc.speed || 0)}</span> كم/س</p>
                  <p className="text-[10px] text-[#6B7280] mt-2">{new Date(loc.timestamp).toLocaleTimeString('ar-EG')}</p>
                </div>
              </Popup>
            </Marker>
          ))}
          <FlyToWorker worker={locations.find(l => l.userId === selectedId) || null} markerRefs={markerRefs} />
        </MapContainer>

        {/* Overlay Info */}
        <div className="absolute top-6 right-6 z-[1000] flex flex-col gap-3 pointer-events-none">
          <div className="p-4 w-64 pointer-events-auto bg-white/90 backdrop-blur-xl border border-[#E5E7EB] rounded-[16px] shadow-[0_4px_20px_rgba(0,0,0,0.08)]">
            <div className="flex items-center justify-between mb-4">
              <div className="flex items-center gap-2 bg-[#8FA36A]/20 text-[#8FA36A] px-2.5 py-1 rounded-full border border-[#8FA36A]/20">
                <span className="w-1.5 h-1.5 bg-[#8FA36A] rounded-full animate-pulse"></span>
                <span className="text-[10px] font-bold">مباشر</span>
              </div>
              <div className="flex flex-row-reverse items-center gap-2">
                <h3 className="text-sm font-bold text-[#2F2F2F]">ملخص التغطية</h3>
                <MapIcon size={16} className="text-[#7895B2]" />
              </div>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="text-center p-3 bg-[#7895B2]/10 rounded-[12px] border border-[#7895B2]/20">
                <p className="text-2xl font-bold text-[#7895B2]">{locations.filter(l => l.status === 'Online').length}</p>
                <p className="text-[9px] font-bold text-[#6B7280] uppercase tracking-wider mt-1">نشط الآن</p>
              </div>
              <div className="text-center p-3 bg-[#F3F1ED] rounded-[12px] border border-[#E5E7EB]">
                <p className="text-2xl font-bold text-[#2F2F2F]">{locations.length}</p>
                <p className="text-[9px] font-bold text-[#6B7280] uppercase tracking-wider mt-1">الإجمالي</p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
