import { useEffect, useState } from "react";
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
import GlassCard from "../components/UI/GlassCard";
import { useMunicipality } from "../contexts/MunicipalityContext";

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
    <div class="absolute w-full h-full bg-secondary rounded-full animate-ping opacity-75"></div>
    <div class="relative w-4 h-4 bg-secondary rounded-full border-2 border-white shadow-lg"></div>
  </div>`,
  iconSize: [24, 24],
  iconAnchor: [12, 12]
});

const OfflineIcon = L.divIcon({
  className: "custom-marker",
  html: `<div class="w-4 h-4 bg-text-muted rounded-full border-2 border-white shadow-lg"></div>`,
  iconSize: [16, 16],
  iconAnchor: [8, 8]
});

function ChangeView({ center }: { center: [number, number] }) {
  const map = useMap();
  map.setView(center, map.getZoom());
  return null;
}

export default function AdminMonitoring() {
  const [locations, setLocations] = useState<WorkerLocation[]>([]);
  const [userDetails, setUserDetails] = useState<Record<number, UserResponse>>({});
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [lastUpdate, setLastUpdate] = useState(new Date());

  // Get map center from municipality settings
  const { mapCenter } = useMunicipality();

  useEffect(() => {
    fetchData();
    const interval = setInterval(() => fetchData(true), 30000); // Auto refresh every 30s
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
    if (level > 80) return <BatteryFull size={14} className="text-secondary" style={{ color: "#2A9D8F" }} />;
    if (level > 40) return <BatteryMedium size={14} className="text-primary" />;
    return <BatteryLow size={14} className="text-accent" />;
  };

  if (loading) {
    return (
      <div className="h-full w-full flex items-center justify-center">
         <div className="flex flex-col items-center gap-4">
              <div className="w-12 h-12 border-4 border-primary/30 border-t-primary rounded-full animate-spin"></div>
              <p className="text-text-secondary font-medium">جاري الاتصال بالنظام الميداني...</p>
         </div>
      </div>
    );
  }

  return (
    <div className="h-full w-full flex flex-col md:flex-row overflow-hidden relative">
      {/* Side Panel */}
      <GlassCard 
        noPadding 
        className="w-full md:w-80 lg:w-96 !bg-background-paper/95 border-r border-primary/10 flex flex-col h-1/3 md:h-full order-2 md:order-1 z-10 !rounded-none md:!rounded-tr-none md:!rounded-br-none"
      >
        <div className="p-6 border-b border-primary/5 bg-primary/5">
           <div className="flex items-center justify-between mb-2">
                <button 
                  onClick={() => fetchData(true)} 
                  disabled={refreshing}
                  className={`p-2 rounded-lg hover:bg-primary/5 transition-colors text-text-secondary ${refreshing ? 'animate-spin' : ''}`}
                  title="تحديث البيانات"
                >
                    <RefreshCcw size={18} />
                </button>
                <div className="text-right">
                    <h2 className="font-extrabold text-text-primary text-xl">مراقبة النظام</h2>
                    <p className="text-[10px] text-text-muted font-bold flex items-center justify-end gap-1 mt-1">
                        <span className="w-1.5 h-1.5 rounded-full bg-secondary animate-pulse"></span>
                        آخر تحديث: {lastUpdate.toLocaleTimeString('ar-EG')}
                    </p>
                </div>
           </div>
        </div>

        <div className="flex-1 overflow-y-auto p-4 space-y-3 custom-scrollbar">
            {locations.map(loc => {
                const user = userDetails[loc.userId];
                return (
                    <div key={loc.userId} className="bg-primary/5 hover:bg-primary/10 p-4 rounded-2xl border border-primary/5 transition-all group cursor-pointer">
                        <div className="flex items-start justify-between mb-3">
                            <div className="flex flex-col items-start gap-1">
                                <span className={`flex items-center gap-1 text-[10px] font-extrabold ${loc.status === 'Online' ? 'text-secondary' : 'text-text-muted'}`}>
                                    {loc.status === 'Online' ? <Wifi size={12} /> : <WifiOff size={12} />}
                                    {loc.status === 'Online' ? 'نشط' : 'غير متصل'}
                                </span>
                                <div className="flex items-center gap-1.5 bg-background-paper/50 px-2 py-0.5 rounded-lg border border-primary/5 mt-1">
                                    <span className="text-[10px] font-bold text-text-secondary font-mono">{user?.lastBatteryLevel || 0}%</span>
                                    {getBatteryIcon(user?.lastBatteryLevel)}
                                </div>
                            </div>
                            <div className="flex flex-col items-end">
                                <h3 className="font-bold text-text-primary text-sm">{loc.fullName}</h3>
                                <p className="text-[10px] text-text-muted font-medium mt-0.5">{loc.zoneName || 'خارج المناطق'}</p>
                            </div>
                        </div>

                        {user?.isLowBattery && (
                            <div className="mt-2 flex items-center justify-end gap-1.5 text-[9px] font-bold text-accent bg-accent/10 py-1.5 px-2 rounded-lg border border-accent/20">
                                <span>تحذير: طاقة منخفضة</span>
                                <AlertCircle size={10} />
                            </div>
                        )}
                        
                        <div className="mt-3 flex items-center justify-between border-t border-primary/5 pt-2">
                             <div className="flex items-center gap-1 text-[9px] text-text-muted">
                                <Radio size={10} className="text-primary opacity-50" />
                                <span>الدقة: {Math.round(loc.accuracy || 0)}م</span>
                             </div>
                             <div className="flex items-center gap-1 text-[9px] text-text-muted font-mono">
                                <span>{new Date(loc.timestamp).toLocaleTimeString('ar-EG', { hour: '2-digit', minute: '2-digit' })}</span>
                                <Navigation size={10} className="opacity-50" />
                             </div>
                        </div>
                    </div>
                );
            })}
            
            {locations.length === 0 && (
                <div className="p-8 text-center text-text-muted font-sans flex flex-col items-center justify-center opacity-50 h-[200px]">
                    <Signal size={32} className="mb-2" />
                    لا يوجد عمال متصلين حالياً
                </div>
            )}
        </div>
      </GlassCard>

      {/* Map Content */}
      <div className="flex-1 relative order-1 md:order-2 h-2/3 md:h-full bg-background-paper">
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
                >
                    <Popup className="glass-popup">
                        <div className="text-right font-sans min-w-[150px]">
                            <h4 className="font-bold border-b border-gray-200 pb-2 mb-2 text-slate-800">{loc.fullName}</h4>
                            <div className="flex items-center justify-end gap-2 mb-1">
                                <span className={`text-xs font-bold ${loc.status === 'Online' ? 'text-green-600' : 'text-gray-500'}`}>{loc.status === 'Online' ? 'متصل' : 'أوفلاين'}</span>
                                <div className={`w-2 h-2 rounded-full ${loc.status === 'Online' ? 'bg-green-500' : 'bg-gray-400'}`}></div>
                            </div>
                            <p className="text-xs text-gray-500 mb-1">السرعة: <span className="font-mono text-slate-700">{Math.round(loc.speed || 0)}</span> كم/س</p>
                            <p className="text-[10px] text-gray-400 mt-2">{new Date(loc.timestamp).toLocaleTimeString('ar-EG')}</p>
                        </div>
                    </Popup>
                </Marker>
            ))}
            {locations.length > 0 && <ChangeView center={[locations[0].latitude, locations[0].longitude]} />}
         </MapContainer>

         {/* Overlay Info */}
         <div className="absolute top-6 right-6 z-[1000] flex flex-col gap-3 pointer-events-none">
            <GlassCard className="p-4 w-64 pointer-events-auto backdrop-blur-xl border-primary/10 !bg-background-paper/80 shadow-2xl">
                <div className="flex items-center justify-between mb-4">
                    <div className="flex items-center gap-2 bg-secondary/20 text-secondary px-2.5 py-1 rounded-full border border-secondary/20">
                        <span className="w-1.5 h-1.5 bg-secondary rounded-full animate-pulse"></span>
                        <span className="text-[10px] font-bold">مباشر</span>
                    </div>
                    <div className="flex flex-row-reverse items-center gap-2">
                        <h3 className="text-sm font-bold text-text-primary">ملخص التغطية</h3>
                        <MapIcon size={16} className="text-primary" />
                    </div>
                </div>
                <div className="grid grid-cols-2 gap-4">
                    <div className="text-center p-3 bg-primary/5 rounded-xl border border-primary/5">
                        <p className="text-2xl font-extrabold text-primary">{locations.filter(l => l.status === 'Online').length}</p>
                        <p className="text-[9px] font-bold text-text-muted uppercase tracking-wider mt-1">نشط الآن</p>
                    </div>
                    <div className="text-center p-3 bg-primary/5 rounded-xl border border-primary/5">
                        <p className="text-2xl font-extrabold text-text-primary">{locations.length}</p>
                        <p className="text-[9px] font-bold text-text-muted uppercase tracking-wider mt-1">الإجمالي</p>
                    </div>
                </div>
            </GlassCard>
         </div>
      </div>
    </div>
  );
}
