import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { usePageTitle } from "../hooks/usePageTitle";
import { getMyWorkers, assignUserZones, getUserZones } from "../api/users";
import { getZones } from "../api/zones";
import { getTasks } from "../api/tasks";
import type { UserResponse } from "../types/user";
import type { ZoneResponse } from "../types/zone";
import { Users, MapPin, Phone, RefreshCw, X, MapPinned, Check } from "lucide-react";
import { useConfirm } from "../components/common/ConfirmDialog";
import MiniStat from "../components/common/MiniStat";
import FilterChip from "../components/common/FilterChip";

type WorkerWithStats = UserResponse & {
  activeTasks: number;
  completedToday: number;
  isOnline: boolean;
  lastSeen?: string;
  teamId?: number;
};

export default function MyWorkers() {
  usePageTitle("العمال");
  const [, ConfirmDialog] = useConfirm();
  const navigate = useNavigate();

  const [workers, setWorkers] = useState<WorkerWithStats[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [search, setSearch] = useState("");
  const [filter, setFilter] = useState<"all" | "online" | "offline">("all");

  // Zone assignment state
  const [zones, setZones] = useState<ZoneResponse[]>([]);
  const [zoneModal, setZoneModal] = useState<{
    open: boolean;
    worker: WorkerWithStats | null;
    selectedZones: number[];
  }>({ open: false, worker: null, selectedZones: [] });
  const [savingZones, setSavingZones] = useState(false);

  useEffect(() => {
    fetchData();
  }, []);

  const fetchData = async () => {
    try {
      setLoading(true);
      setError("");

      const [workersData, tasksData, zonesData] = await Promise.all([
        getMyWorkers(),
        getTasks(),
        getZones(),
      ]);

      setZones(zonesData);

      const today = new Date().toISOString().split("T")[0];
      const workersWithStats: WorkerWithStats[] = workersData.map((worker) => {
        const workerTasks = tasksData.filter(
          (t) => t.assignedToUserId === worker.userId ||
            (t.isTeamTask && t.teamId && worker.teamId === t.teamId)
        );
        const activeTasks = workerTasks.filter(
          (t) => t.status === "Pending" || t.status === "InProgress"
        ).length;
        const completedToday = workerTasks.filter(
          (t) =>
            t.status === "Completed" &&
            t.completedAt?.startsWith(today)
        ).length;

        const lastLogin = worker.lastLoginAt ? new Date(worker.lastLoginAt) : null;
        const isOnline = lastLogin
          ? Date.now() - lastLogin.getTime() < 15 * 60 * 1000
          : false;

        return {
          ...worker,
          activeTasks,
          completedToday,
          isOnline,
          lastSeen: worker.lastLoginAt,
        };
      });

      setWorkers(workersWithStats);
    } catch (err) {
      console.error("Failed to fetch workers:", err);
      setError("فشل تحميل بيانات العمال");
    } finally {
      setLoading(false);
    }
  };

  const filtered = useMemo(() => {
    let result = workers;
    if (filter === "online") result = result.filter((w) => w.isOnline);
    else if (filter === "offline") result = result.filter((w) => !w.isOnline);

    const q = search.trim().toLowerCase();
    if (q) {
      result = result.filter(
        (w) =>
          (w.fullName || "").toLowerCase().includes(q) ||
          (w.username || "").toLowerCase().includes(q) ||
          (w.phoneNumber || "").includes(q)
      );
    }
    return result;
  }, [workers, filter, search]);

  const stats = useMemo(() => {
    const total = workers.length;
    const online = workers.filter((w) => w.isOnline).length;
    const offline = total - online;
    const totalActive = workers.reduce((sum, w) => sum + w.activeTasks, 0);
    return { total, online, offline, totalActive };
  }, [workers]);

  const openZoneModal = async (worker: WorkerWithStats) => {
    try {
      const workerZones = await getUserZones(worker.userId);
      setZoneModal({
        open: true,
        worker,
        selectedZones: workerZones,
      });
    } catch (err) {
      console.error("Failed to fetch worker zones:", err);
      setZoneModal({
        open: true,
        worker,
        selectedZones: [],
      });
    }
  };

  const handleSaveZones = async () => {
    if (!zoneModal.worker) return;

    try {
      setSavingZones(true);
      await assignUserZones(zoneModal.worker.userId, zoneModal.selectedZones);
      setZoneModal({ open: false, worker: null, selectedZones: [] });
      fetchData();
    } catch (err) {
      console.error("Failed to save zones:", err);
    } finally {
      setSavingZones(false);
    }
  };

  const toggleZone = (zoneId: number) => {
    setZoneModal(prev => ({
      ...prev,
      selectedZones: prev.selectedZones.includes(zoneId)
        ? prev.selectedZones.filter(id => id !== zoneId)
        : [...prev.selectedZones, zoneId],
    }));
  };

  if (loading) {
    return (
      <div className="h-full w-full bg-[#F3F1ED] grid place-items-center">
        <div className="text-[#2F2F2F] font-sans font-semibold">جاري التحميل...</div>
      </div>
    );
  }

  return (
    <div className="h-full w-full bg-[#F3F1ED] overflow-auto">
      <div className="p-4 sm:p-6 md:p-8">
        <div className="max-w-[1100px] mx-auto">
          {/* Unified Header */}
          <div className="flex items-center justify-between gap-3">
            <h1 className="text-right font-black text-[28px] text-[#2F2F2F] tracking-tight">
              إدارة عمالي
            </h1>
            <button
              type="button"
              onClick={fetchData}
              className="h-[38px] px-4 rounded-[10px] bg-white border border-black/10 text-[#2F2F2F] font-sans font-semibold text-[13px] hover:opacity-95 flex items-center gap-2"
            >
              <RefreshCw size={14} className={loading ? "animate-spin" : ""} />
              تحديث
            </button>
          </div>

          {error && (
            <div className="mt-4 p-3 bg-red-100 text-red-700 rounded-[10px] text-right">
              {error}
            </div>
          )}

          {/* Mini Stats Grid */}
          <div className="mt-4 grid grid-cols-1 sm:grid-cols-4 gap-3">
            <MiniStat title="إجمالي العمال" value={String(stats.total)} />
            <MiniStat title="متصل الآن" value={String(stats.online)} />
            <MiniStat title="غير متصل" value={String(stats.offline)} />
            <MiniStat title="مهام نشطة" value={String(stats.totalActive)} />
          </div>

          {/* Filter Bar */}
          <div className="mt-4 bg-[#F3F1ED] rounded-[14px] border border-black/10 shadow-[0_2px_0_rgba(0,0,0,0.08)] p-3 sm:p-4">
            <div className="flex flex-col md:flex-row md:items-center gap-3">
              <div className="flex-1">
                <input
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                  placeholder="بحث باسم العامل، الهاتف، أو اسم المستخدم..."
                  className="w-full h-[44px] rounded-[12px] bg-white border border-black/10 px-4 text-right outline-none focus:ring-2 focus:ring-black/10 text-[13px]"
                />
              </div>

              <div className="flex items-center gap-2 flex-wrap justify-end">
                <FilterChip
                  active={filter === "all"}
                  onClick={() => setFilter("all")}
                  label="الكل"
                  count={stats.total}
                />
                <FilterChip
                  active={filter === "online"}
                  onClick={() => setFilter("online")}
                  label="متصل"
                  count={stats.online}
                />
                <FilterChip
                  active={filter === "offline"}
                  onClick={() => setFilter("offline")}
                  label="غير متصل"
                  count={stats.offline}
                />
              </div>
            </div>
          </div>

          {/* Workers List */}
          <div className="mt-4 space-y-3">
            {filtered.length === 0 ? (
              <div className="bg-white rounded-[14px] border border-black/10 p-5 text-right text-[#6B7280]">
                لا يوجد عمال يطابقون البحث.
              </div>
            ) : (
              filtered.map((w) => (
                <WorkerCard
                  key={w.userId}
                  worker={w}
                  onViewHistory={() => navigate(`/location-history/${w.userId}`)}
                  onAssignZones={() => openZoneModal(w)}
                />
              ))
            )}
          </div>
        </div>
      </div>

      {/* Zone Assignment Modal */}
      {zoneModal.open && zoneModal.worker && (
        <CenterModal onClose={() => setZoneModal({ open: false, worker: null, selectedZones: [] })}>
          <div className="text-right">
            <div className="flex items-center justify-between mb-4">
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 rounded-xl bg-[#7895B2]/10 flex items-center justify-center">
                  <MapPinned size={20} className="text-[#7895B2]" />
                </div>
                <div>
                  <div className="text-[16px] font-sans font-bold text-[#2F2F2F]">
                    تعيين المناطق
                  </div>
                  <div className="text-[12px] text-[#6B7280]">
                    {zoneModal.worker.fullName}
                  </div>
                </div>
              </div>
              <div>{/* spacer for X button */}</div>
            </div>

            <div className="mt-4 grid grid-cols-2 gap-2 max-h-[300px] overflow-auto p-1">
              {zones.map((zone) => (
                <button
                  key={zone.zoneId}
                  onClick={() => toggleZone(zone.zoneId)}
                  className={`p-3 rounded-xl border text-right transition-all flex items-center justify-between gap-2 ${
                    zoneModal.selectedZones.includes(zone.zoneId)
                      ? "bg-[#7895B2]/10 border-[#7895B2] text-[#2F2F2F]"
                      : "bg-white border-black/10 text-[#6B7280] hover:border-[#7895B2]/50"
                  }`}
                >
                  <div className={`w-5 h-5 rounded-md flex items-center justify-center ${
                    zoneModal.selectedZones.includes(zone.zoneId)
                      ? "bg-[#7895B2] text-white"
                      : "bg-black/5"
                  }`}>
                    {zoneModal.selectedZones.includes(zone.zoneId) && <Check size={14} />}
                  </div>
                  <span className="text-[13px] font-semibold flex-1 text-right">{zone.zoneName}</span>
                </button>
              ))}
            </div>

            <div className="mt-5 flex gap-3">
              <button
                onClick={() => setZoneModal({ open: false, worker: null, selectedZones: [] })}
                className="flex-1 h-11 rounded-xl border border-black/10 text-[#2F2F2F] font-bold text-[13px] hover:bg-black/5 transition"
              >
                إلغاء
              </button>
              <button
                onClick={handleSaveZones}
                disabled={savingZones}
                className="flex-1 h-11 rounded-xl bg-[#7895B2] text-white font-bold text-[13px] hover:bg-[#6a85a0] transition disabled:opacity-50"
              >
                {savingZones ? "جاري الحفظ..." : `حفظ (${zoneModal.selectedZones.length} منطقة)`}
              </button>
            </div>
          </div>
        </CenterModal>
      )}

      {ConfirmDialog}
    </div>
  );
}

function WorkerCard({ worker, onViewHistory, onAssignZones }: { worker: WorkerWithStats; onViewHistory: () => void; onAssignZones: () => void }) {
  return (
    <div className="bg-[#F3F1ED] rounded-[14px] border border-black/10 shadow-[0_2px_0_rgba(0,0,0,0.06)] overflow-hidden">
      <div className="p-4 sm:p-5 flex items-center justify-between gap-4">

        {/* Worker Info & Avatar */}
        <div className="flex items-center gap-4 text-right">
           <div className="relative shrink-0">
              <div className="w-14 h-14 rounded-[16px] bg-white border border-black/10 flex items-center justify-center shadow-sm">
                <Users size={26} className={worker.isOnline ? "text-[#7895B2]" : "text-[#9CA3AF]"} />
              </div>
              <span className={`absolute -bottom-1 -right-1 w-4.5 h-4.5 rounded-full border-[3.5px] border-white ${worker.isOnline ? "bg-green-500 shadow-sm" : "bg-gray-400 shadow-sm"}`} />
           </div>
           <div className="text-right">
              <div className="text-[16px] font-sans font-black text-[#2F2F2F]">{worker.fullName}</div>
              <div className="text-[12px] text-[#6B7280] flex items-center justify-end gap-1.5 mt-0.5">
                <span dir="ltr" className="font-sans font-bold">{worker.phoneNumber}</span>
                <Phone size={12} className="text-[#AFAFAF]" />
              </div>
           </div>
        </div>

        {/* Quick Stats */}
        <div className="flex-1 flex items-center justify-center gap-3">
          <MetaSmallPill label="منجز اليوم" value={worker.completedToday} />
          <MetaSmallPill label="المهام نشطة" value={worker.activeTasks} />
        </div>

        {/* Action Buttons */}
        <div className="flex items-center gap-2">
          <button
            onClick={onAssignZones}
            className="h-[40px] px-4 rounded-[12px] bg-white border border-black/10 text-[#7895B2] text-[13px] font-bold hover:bg-[#7895B2]/5 transition-all flex items-center gap-2 whitespace-nowrap"
          >
            <MapPinned size={16} />
            المناطق
          </button>
          <button
            onClick={onViewHistory}
            className="h-[40px] px-4 rounded-[12px] bg-[#7895B2] text-white text-[13px] font-bold shadow-[0_2px_0_rgba(0,0,0,0.1)] hover:opacity-95 transition-all flex items-center gap-2 whitespace-nowrap"
          >
            <MapPin size={16} />
            السجل
          </button>
        </div>
      </div>
    </div>
  );
}

function MetaSmallPill({ label, value }: { label: string; value: number }) {
  return (
    <div className="bg-white/80 rounded-[10px] border border-black/5 px-3 py-1.5 min-w-[90px] text-right">
      <div className="text-[10px] text-[#6B7280] font-semibold">{label}</div>
      <div className="text-[13px] text-[#2F2F2F] font-bold">{value}</div>
    </div>
  );
}

function CenterModal({ children, onClose }: { children: React.ReactNode; onClose: () => void }) {
  return (
    <div className="fixed inset-0 z-[60] flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-black/40" onClick={onClose} />
      <div className="relative w-full max-w-[480px] bg-[#F3F1ED] rounded-[16px] shadow-2xl border border-black/10 p-5">
        <button onClick={onClose} className="absolute top-4 left-4 text-[#6B7280] hover:text-[#2F2F2F]">
          <X size={20} />
        </button>
        {children}
      </div>
    </div>
  );
}
