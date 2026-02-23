import { useEffect, useState, useMemo } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { MapContainer, TileLayer, Polyline, Marker, Popup, useMap } from "react-leaflet";
import L from "leaflet";
import { getWorkerLocationHistory } from "../api/tracking";
import { getMyWorkers } from "../api/users";
import { getTasks, reassignTask } from "../api/tasks";
import type { WorkerLocation } from "../types/tracking";
import type { UserResponse } from "../types/user";
import type { TaskResponse } from "../types/task";
import { ArrowRight, RefreshCw, AlertCircle, Phone, Users, X } from "lucide-react";
import { useConfirm } from "../components/common/ConfirmDialog";
import "leaflet/dist/leaflet.css";

// Fix Leaflet marker icon issue
import markerIcon2x from "leaflet/dist/images/marker-icon-2x.png";
import markerIcon from "leaflet/dist/images/marker-icon.png";
import markerShadow from "leaflet/dist/images/marker-shadow.png";

// @ts-ignore
delete L.Icon.Default.prototype._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl: markerIcon2x,
  iconUrl: markerIcon,
  shadowUrl: markerShadow,
});

// Custom icons for start/end points
const startIcon = new L.DivIcon({
  className: "custom-div-icon",
  html: `<div style="background-color: #8FA36A; width: 24px; height: 24px; border-radius: 50%; border: 3px solid white; box-shadow: 0 2px 5px rgba(0,0,0,0.3); display: flex; align-items: center; justify-content: center;">
    <span style="color: white; font-size: 10px; font-weight: bold;">ب</span>
  </div>`,
  iconSize: [24, 24],
  iconAnchor: [12, 12],
});

const endIcon = new L.DivIcon({
  className: "custom-div-icon",
  html: `<div style="background-color: #C86E5D; width: 24px; height: 24px; border-radius: 50%; border: 3px solid white; box-shadow: 0 2px 5px rgba(0,0,0,0.3); display: flex; align-items: center; justify-content: center;">
    <span style="color: white; font-size: 10px; font-weight: bold;">ن</span>
  </div>`,
  iconSize: [24, 24],
  iconAnchor: [12, 12],
});

// Auto-fit map bounds component
function FitBounds({ positions }: { positions: [number, number][] }) {
  const map = useMap();

  useEffect(() => {
    if (positions.length > 0) {
      const bounds = L.latLngBounds(positions);
      map.fitBounds(bounds, { padding: [50, 50] });
    }
  }, [positions, map]);

  return null;
}

export default function LocationHistory() {
  const [confirm, ConfirmDialog] = useConfirm();
  const { workerId } = useParams<{ workerId: string }>();
  const navigate = useNavigate();

  const [worker, setWorker] = useState<UserResponse | null>(null);
  const [allWorkers, setAllWorkers] = useState<UserResponse[]>([]);
  const [workerTasks, setWorkerTasks] = useState<TaskResponse[]>([]);
  const [history, setHistory] = useState<WorkerLocation[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [selectedDate, setSelectedDate] = useState(() => {
    const today = new Date();
    return today.toISOString().split("T")[0];
  });

  const [reassignModal, setReassignModal] = useState<{
    open: boolean;
    taskId: number | null;
    taskTitle: string;
  }>({ open: false, taskId: null, taskTitle: "" });
  const [reassigning, setReassigning] = useState(false);

  useEffect(() => {
    if (workerId) {
      fetchAllData();
    }
  }, [workerId, selectedDate]);

  const fetchAllData = async () => {
    try {
      setLoading(true);
      setError("");
      
      const [workersData, historyData, tasksData] = await Promise.all([
        getMyWorkers(),
        getWorkerLocationHistory(Number(workerId)),
        getTasks({ workerId: Number(workerId) }),
      ]);

      const found = workersData.find((w) => w.userId === Number(workerId));
      if (found) setWorker(found);
      setAllWorkers(workersData);
      setWorkerTasks(tasksData);

      // Filter history by selected date
      const filteredHistory = historyData.filter((loc) => {
        const d = new Date(loc.timestamp);
        const locDate = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
        return locDate === selectedDate;
      });

      filteredHistory.sort((a, b) => new Date(a.timestamp).getTime() - new Date(b.timestamp).getTime());
      setHistory(filteredHistory);

    } catch (err) {
      console.error("Failed to fetch data:", err);
      setError("فشل تحميل بيانات العامل");
    } finally {
      setLoading(false);
    }
  };

  const positions = useMemo((): [number, number][] => {
    return history.map((loc) => [loc.latitude, loc.longitude]);
  }, [history]);

  const stats = useMemo(() => {
    const activeTasks = workerTasks.filter(t => t.status === "Pending" || t.status === "InProgress").length;
    const completedTasks = workerTasks.filter(t => t.status === "Approved" || t.status === "Completed").length;

    if (history.length < 2) {
      return { distance: 0, duration: 0, activeTasks, completedTasks, points: history.length };
    }

    let totalDistance = 0;
    for (let i = 1; i < history.length; i++) {
      const lat1 = history[i - 1].latitude;
      const lon1 = history[i - 1].longitude;
      const lat2 = history[i].latitude;
      const lon2 = history[i].longitude;
      const R = 6371;
      const dLat = ((lat2 - lat1) * Math.PI) / 180;
      const dLon = ((lon2 - lon1) * Math.PI) / 180;
      const a = Math.sin(dLat / 2) * Math.sin(dLat / 2) + Math.cos((lat1 * Math.PI) / 180) * Math.cos((lat2 * Math.PI) / 180) * Math.sin(dLon / 2) * Math.sin(dLon / 2);
      const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
      totalDistance += R * c;
    }

    const durationMs = new Date(history[history.length - 1].timestamp).getTime() - new Date(history[0].timestamp).getTime();
    return { distance: totalDistance, duration: durationMs / (1000 * 60), activeTasks, completedTasks, points: history.length };
  }, [history, workerTasks]);

  const handleReassign = async (targetWorkerId: number) => {
    if (!reassignModal.taskId) return;
    const target = allWorkers.find(w => w.userId === targetWorkerId);
    if (!target) return;
    if (!await confirm(`هل أنت متأكد من إعادة تعيين المهمة "${reassignModal.taskTitle}" إلى ${target.fullName}؟`)) return;

    try {
      setReassigning(true);
      await reassignTask(reassignModal.taskId, {
        newAssignedToUserId: targetWorkerId,
        reassignmentReason: "تم إعادة التعيين بواسطة المشرف من سجل العامل",
      });
      setReassignModal({ open: false, taskId: null, taskTitle: "" });
      fetchAllData();
    } catch (err) {
      console.error("Failed to reassign task:", err);
    } finally {
      setReassigning(false);
    }
  };

  const formatTime = (dateStr: string) => {
    const date = new Date(dateStr);
    return date.toLocaleTimeString("ar-SA", { hour: "2-digit", minute: "2-digit" });
  };

  const formatDuration = (mins: number) => {
    const hours = Math.floor(mins / 60);
    const minutes = Math.round(mins % 60);
    return hours > 0 ? `${hours} ساعة ${minutes} دقيقة` : `${minutes} دقيقة`;
  };

  const defaultCenter: [number, number] = [31.7054, 35.2024];

  if (loading && !worker) {
    return (
      <div className="h-full w-full bg-[#F3F1ED] grid place-items-center">
        <div className="text-[#2F2F2F] font-sans font-semibold">جاري التحميل...</div>
      </div>
    );
  }

  return (
    <div className="h-full w-full bg-[#F3F1ED] overflow-auto">
      <div className="p-4 sm:p-6 md:p-8">
        <div className="max-w-[1100px] mx-auto space-y-6">

          {error && (
            <div className="bg-[#C86E5D]/10 border border-[#C86E5D]/30 rounded-[12px] p-4 text-[#C86E5D] text-center font-medium" dir="rtl">
              {error}
            </div>
          )}

          {/* Header & Worker Info */}
          <div className="flex flex-col md:flex-row items-center justify-between gap-6">
            <div className="flex items-start gap-5 text-right">
              {/* Avatar (Before Text in RTL = Right Side) */}
              <div className="w-20 h-20 rounded-[28px] bg-white border border-black/10 flex items-center justify-center shadow-sm shrink-0">
                <Users size={40} className="text-[#7895B2]" />
              </div>

              {/* Text Info */}
              <div className="flex flex-col flex-1">
                <div className="text-[14px] font-black text-[#6B7280] uppercase tracking-wider mb-1">
                  سجل العامل:
                </div>
                <h1 className="font-sans font-black text-[32px] text-[#2F2F2F] tracking-tight leading-tight">
                  {worker?.fullName}
                </h1>
                <div className="flex items-center justify-end gap-2.5 mt-1.5 text-[#6B7280]">
                  <span className="font-sans font-bold text-[18px]" dir="ltr">{worker?.phoneNumber}</span>
                  <Phone size={18} className="text-[#AFAFAF]" />
                </div>
                
                <div className="flex items-center justify-end gap-3 mt-4">
                  {worker?.teamName && (
                    <div className="bg-[#7895B2]/10 px-3 py-1 rounded-[8px] text-[12px] font-black text-[#7895B2]">
                       {worker.teamName} • الفريق
                    </div>
                  )}
                  {worker?.department && (
                    <div className="bg-[#8FA36A]/10 px-3 py-1 rounded-[8px] text-[12px] font-black text-[#8FA36A]">
                       {worker.department} • القسم
                    </div>
                  )}
                </div>
              </div>
            </div>

            <div className="flex items-center gap-3">
              <button
                onClick={() => navigate("/my-workers")}
                className="h-[44px] px-6 rounded-[12px] bg-white border border-black/10 text-[#2F2F2F] font-bold text-[13px] hover:bg-black/5 transition flex items-center gap-2"
              >
                <ArrowRight size={18} />
                الرجوع للعمال
              </button>
              <button
                onClick={fetchAllData}
                className="h-[44px] w-[44px] rounded-[12px] bg-[#7895B2] text-white flex items-center justify-center shadow-lg shadow-[#7895B2]/20 hover:bg-[#647e99] transition active:scale-95"
              >
                <RefreshCw size={20} className={loading ? "animate-spin" : ""} />
              </button>
            </div>
          </div>

          {/* Stat Cards */}
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
            <MiniStat title="مهام نشطة" value={String(stats.activeTasks)} color="#7895B2" />
            <MiniStat title="مهام مكتملة" value={String(stats.completedTasks)} color="#8FA36A" />
            <MiniStat title="مدة العمل" value={stats.duration > 0 ? formatDuration(stats.duration) : "—"} color="#2F2F2F" />
          </div>

          {/* Active Tasks Section */}
          <div className="bg-[#F3F1ED] rounded-[16px] border border-black/10 shadow-[0_2px_0_rgba(0,0,0,0.06)] overflow-hidden">
            <div className="p-4 border-b border-black/5 flex items-center justify-between">
              <div className="text-[12px] font-bold text-[#6B7280]">{stats.activeTasks} مهام</div>
              <h3 className="text-[16px] font-black text-[#2F2F2F]">المهام الجاري العمل عليها</h3>
            </div>
            <div className="p-4 space-y-3">
              {workerTasks.filter(t => t.status === "Pending" || t.status === "InProgress").length === 0 ? (
                <div className="text-center py-6 text-[#6B7280] text-[13px] font-semibold bg-white/50 rounded-[12px]">لا يوجد مهام نشطة حالياً للعامل.</div>
              ) : (
                workerTasks.filter(t => t.status === "Pending" || t.status === "InProgress").map((task) => (
                  <div key={task.taskId} className="bg-white rounded-[14px] border border-black/10 p-4 flex items-center justify-between shadow-sm">
                    <button
                      onClick={() => setReassignModal({ open: true, taskId: task.taskId, taskTitle: task.title })}
                      className="h-[36px] px-4 rounded-[10px] bg-[#F2D3CD] text-[#2F2F2F] text-[12px] font-bold hover:bg-[#ebbbbb] transition"
                    >
                      إعادة تعيين
                    </button>
                    <div className="text-right flex-1 ml-4 mr-4">
                      <div className="text-[14px] font-bold text-[#2F2F2F]">{task.title}</div>
                      <div className="text-[11px] text-[#6B7280] mt-1 flex items-center justify-end gap-2">
                         <span>{task.zoneName || "بدون منطقة"}</span>
                         <span className="w-1 h-1 rounded-full bg-black/10" />
                         <span className={task.status === "InProgress" ? "text-[#7895B2]" : "text-[#AFAFAF]"}>
                           {task.status === "InProgress" ? "قيد التنفيذ" : "قيد الانتظار"}
                         </span>
                      </div>
                    </div>
                    <div className="w-10 h-10 rounded-[10px] bg-[#F3F1ED] flex items-center justify-center shrink-0 font-bold text-[#7895B2] text-[13px]">
                      {task.taskId}
                    </div>
                  </div>
                ))
              )}
            </div>
          </div>

          {/* Map & History Controls */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            {/* Map Section */}
            <div className="md:col-span-2 space-y-4">
              <div className="bg-white rounded-[16px] border border-black/10 shadow-[0_2px_0_rgba(0,0,0,0.06)] overflow-hidden h-[450px] relative">
                {history.length === 0 ? (
                  <div className="absolute inset-0 flex flex-col items-center justify-center p-8 text-center bg-[#F9F8F6]">
                    <AlertCircle size={48} className="text-[#AFAFAF] mb-4" />
                    <div className="text-[16px] font-black text-[#2F2F2F]">لا يوجد سجل حركة</div>
                    <div className="text-[12px] text-[#AFAFAF] mt-1">لم يتم تسجيل أي حركة للعامل في الموعد المحدد.</div>
                  </div>
                ) : (
                  <MapContainer center={positions[0] || defaultCenter} zoom={15} className="h-full w-full">
                    <TileLayer attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors' url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png" />
                    <FitBounds positions={positions} />
                    <Polyline positions={positions} color="#7895B2" weight={4} opacity={0.8} />
                    {positions.length > 0 && (
                      <Marker position={positions[0]} icon={startIcon}>
                        <Popup><div className="text-right font-sans font-bold">نقطة البداية</div></Popup>
                      </Marker>
                    )}
                    {positions.length > 1 && (
                      <Marker position={positions[positions.length - 1]} icon={endIcon}>
                        <Popup><div className="text-right font-sans font-bold">آخر موقع</div></Popup>
                      </Marker>
                    )}
                  </MapContainer>
                )}
              </div>
            </div>

            {/* Controls & Date Filter */}
            <div className="space-y-4">
              <div className="bg-white rounded-[16px] border border-black/10 shadow-[0_2px_0_rgba(0,0,0,0.06)] p-5">
                <h3 className="text-[15px] font-black text-[#2F2F2F] text-right mb-4">فلترة التاريخ</h3>
                <input
                  type="date"
                  value={selectedDate}
                  onChange={(e) => setSelectedDate(e.target.value)}
                  className="w-full h-12 rounded-[12px] bg-[#F3F1ED] border-0 px-4 text-right font-bold text-[14px] outline-none"
                />
              </div>

              {/* Timeline */}
              {history.length > 0 && (
                <div className="bg-white rounded-[16px] border border-black/10 shadow-[0_2px_0_rgba(0,0,0,0.06)] p-5">
                  <h3 className="text-[15px] font-black text-[#2F2F2F] text-right mb-4">الجدول الزمني</h3>
                  <div className="space-y-3 max-h-[250px] overflow-auto pr-1">
                    {history.map((loc, idx) => (
                      <div key={idx} className="flex items-center justify-between py-2 border-b border-black/5 last:border-0 hover:bg-black/5 px-2 transition rounded-lg">
                        <div className="text-[11px] font-bold text-[#6B7280]">{formatTime(loc.timestamp)}</div>
                        <div className="flex items-center gap-2">
                           <span className="text-[12px] font-bold text-[#2F2F2F]">{loc.zoneName || "غير محدد"}</span>
                           <div className={`w-2 h-2 rounded-full ${idx === 0 ? "bg-[#8FA36A]" : idx === history.length - 1 ? "bg-[#C86E5D]" : "bg-[#7895B2]"}`} />
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* Reassign Modal */}
      {reassignModal.open && (
        <CenterModal onClose={() => setReassignModal({ open: false, taskId: null, taskTitle: "" })}>
           <div className="text-right">
            <h3 className="text-[18px] font-black text-[#2F2F2F]">إعادة تعيين المهمة</h3>
            <p className="text-[13px] text-[#6B7280] mt-1">تغيير العامل المسؤول عن: <span className="font-bold">{reassignModal.taskTitle}</span></p>
            
            <div className="mt-4 space-y-2 max-h-[300px] overflow-auto">
              {allWorkers.filter(w => w.userId !== Number(workerId)).map(w => (
                <button
                  key={w.userId}
                  onClick={() => handleReassign(w.userId)}
                  disabled={reassigning}
                  className="w-full p-4 rounded-[14px] bg-white border border-black/10 text-right hover:bg-[#7895B2]/5 transition-all flex items-center justify-between group shadow-sm"
                >
                  <ArrowRight size={16} className="text-[#AFAFAF] transition-transform group-hover:-translate-x-1" />
                  <div>
                    <div className="text-[14px] font-black text-[#2F2F2F]">{w.fullName}</div>
                    <div className="text-[11px] text-[#6B7280] font-sans">{w.phoneNumber}</div>
                  </div>
                </button>
              ))}
            </div>
           </div>
        </CenterModal>
      )}

      {ConfirmDialog}
    </div>
  );
}

/* ---------- UI Components ---------- */



function MiniStat({ title, value, color }: { title: string; value: string; color: string }) {
  return (
    <div className="bg-[#F3F1ED] rounded-[16px] border border-black/10 shadow-[0_2px_0_rgba(0,0,0,0.06)] p-4 flex flex-col items-end gap-1">
      <div className="text-[10px] font-black text-[#AFAFAF] uppercase tracking-widest">{title}</div>
      <div className="text-[18px] font-black" style={{ color }}>{value}</div>
    </div>
  );
}

function CenterModal({ children, onClose }: { children: React.ReactNode, onClose: () => void }) {
  return (
    <div className="fixed inset-0 z-[100] grid place-items-center p-4">
      <div className="absolute inset-0 bg-black/40 backdrop-blur-sm" onClick={onClose} />
      <div className="bg-[#F3F1ED] rounded-[24px] shadow-2xl border border-black/10 p-6 w-full max-w-[480px] relative">
        <button onClick={onClose} className="absolute left-6 top-6 text-[#AFAFAF] hover:text-[#2F2F2F] transition-colors">
          <X size={20} />
        </button>
        {children}
      </div>
    </div>
  );
}
