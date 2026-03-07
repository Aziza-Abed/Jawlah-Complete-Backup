import React, { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { STORAGE_KEYS } from "../constants/storageKeys";
import { Users, ClipboardList, MapPin, Activity, PlusCircle, ArrowRight, CircleHelp } from "lucide-react";
import { getDashboardOverview, getWorkerStatuses } from "../api/dashboard";
import type { DashboardOverview, WorkerStatus } from "../types/dashboard";
import { getAuditLogs, type AuditLog } from "../api/audit";
import WelcomeGuide from "../components/common/WelcomeGuide";
import AlertsWidget from "../components/dashboard/AlertsWidget";
import IssuesWidget from "../components/dashboard/IssuesWidget";


type ActivityItem = {
  id: string;
  time: string;
  text: string;
  icon: "pin" | "user" | "map";
};

const SupervisorDashboard: React.FC = () => {
    const navigate = useNavigate();
    const [overview, setOverview] = useState<DashboardOverview | null>(null);
    const [activities, setActivities] = useState<ActivityItem[]>([]);
    const [workerStatuses, setWorkerStatuses] = useState<WorkerStatus[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");
    const [userName, setUserName] = useState("مشرف");
    const [showWelcome, setShowWelcome] = useState(false);

    useEffect(() => {
        try {
            const userStr = localStorage.getItem(STORAGE_KEYS.USER);
            if (userStr) {
                const user = JSON.parse(userStr);
                setUserName(user.fullName?.split(' ')[0] || "مشرف");
            }
        } catch (e) {}

        const fetchData = async () => {
            try {
                setLoading(true);
                const [overviewData, workersData, auditResponse] = await Promise.all([
                    getDashboardOverview(),
                    getWorkerStatuses(),
                    getAuditLogs(1, 15)
                ]);

                setOverview(overviewData);
                setWorkerStatuses(workersData);

                // Map and clean up activities
                const logs = auditResponse.items || [];
                const mappedActivities: ActivityItem[] = logs
                    .filter((log: AuditLog) => !log.action.includes("Login") && !log.action.includes("Logout"))
                    .slice(0, 5)
                    .map((log: AuditLog) => {
                        const date = new Date(log.createdAt);
                        const time = date.toLocaleTimeString('ar-EG', { hour: '2-digit', minute: '2-digit' });

                        let icon: "pin" | "user" | "map" = "user";
                        if (log.action?.includes('Task')) icon = "pin";
                        else if (log.action?.includes('Zone')) icon = "map";

                        return {
                            id: log.auditLogId.toString(),
                            time,
                            text: log.details || log.action,
                            icon
                        };
                    });
                setActivities(mappedActivities);
            } catch (err) {
                console.error("Failed to fetch dashboard data", err);
                setError("فشل في تحميل بيانات المركز");
            } finally {
                setLoading(false);
            }
        };

        fetchData();

        // Check for first time visit
        const hasSeenWelcome = localStorage.getItem(STORAGE_KEYS.HAS_SEEN_WELCOME);
        if (!hasSeenWelcome) {
             // Delay slightly for smoother entrance
             setTimeout(() => setShowWelcome(true), 1000);
        }
    }, []);

    const handleCloseWelcome = () => {
        setShowWelcome(false);
        localStorage.setItem(STORAGE_KEYS.HAS_SEEN_WELCOME, "true");
    };

    const tasksTotal = (overview?.tasks.pending ?? 0) + (overview?.tasks.inProgress ?? 0) + (overview?.tasks.completedToday ?? 0);

  if (loading) {
    return (
      <div className="h-full w-full bg-[#F3F1ED] flex items-center justify-center">
        <div className="flex flex-col items-center gap-4">
          <div className="w-12 h-12 border-4 border-[#7895B2]/30 border-t-[#7895B2] rounded-full animate-spin"></div>
          <p className="text-[#6B7280] font-medium">جاري تحميل البيانات...</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="h-full w-full bg-[#F3F1ED] flex items-center justify-center">
        <div className="text-[#C86E5D] font-bold">{error}</div>
      </div>
    );
  }

    return (
      <div className="h-full w-full bg-[#F3F1ED] overflow-auto">
        <WelcomeGuide isOpen={showWelcome} onClose={handleCloseWelcome} />
        
        <div className="p-4 sm:p-6 md:p-8">
          <div className="max-w-[1200px] mx-auto space-y-6">
            {/* Header with Title (Title Left, Date Right) */}
            <div className="flex items-center justify-between">
                <div className="text-right">
                  <h1 className="font-black text-[28px] text-[#2F2F2F] tracking-tight">لوحة التحكم</h1>
                  <p className="text-[#6B7280] text-[15px] mt-1 font-medium italic">مرحباً بك مجدداً، {userName}</p>
                </div>

                <div className="flex items-center gap-3">
                    <div className="text-[14px] bg-white px-5 py-2.5 rounded-xl text-[#6B7280] font-black border border-black/5 shadow-sm">
                      {new Date().toLocaleDateString('ar-EG', { weekday: 'long', day: 'numeric', month: 'long' })}
                    </div>
                    <button
                      onClick={() => setShowWelcome(true)}
                      className="p-3 text-[#7895B2] hover:bg-[#7895B2]/10 rounded-xl transition-all bg-white shadow-sm border border-black/5 group"
                      title="المساعدة"
                    >
                      <CircleHelp size={22} className="group-hover:rotate-12 transition-transform" />
                    </button>
                </div>
            </div>

            {/* Stats Grid */}
            <div className="grid grid-cols-2 lg:grid-cols-4 gap-6">
              <DashboardStatItem icon={Users} label="إجمالي الفريق" value={overview?.workers.total} color="#7895B2" />
              <DashboardStatItem icon={Activity} label="المتواجدون الآن" value={overview?.workers.checkedIn} color="#8FA36A" />
              <DashboardStatItem icon={ClipboardList} label="مهام اليوم" value={tasksTotal} color="#C86E5D" />
              <DashboardStatItem icon={ArrowRight} label="أنجزت اليوم" value={overview?.tasks.completedToday} color="#8FA36A" />
            </div>

            {/* Alerts & Issues Section (Merged from old UI) */}
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
               <AlertsWidget 
                 pendingTasks={overview?.tasks.pending ?? 0}
                 absentWorkers={overview?.workers.notCheckedIn ?? 0}
                 unresolvedIssues={overview?.issues.unresolved ?? 0}
               />
               <IssuesWidget 
                 reportedToday={overview?.issues.reportedToday ?? 0}
                 unresolved={overview?.issues.unresolved ?? 0}
               />
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              {/* Team Status */}
              <div className="bg-white rounded-[16px] p-6 shadow-[0_4px_20px_rgba(0,0,0,0.04)] min-h-[300px]">
                <div className="flex items-center justify-between mb-5">
                  <h2 className="text-[18px] font-bold text-[#2F2F2F]">حالة الفريق</h2>
                  {workerStatuses.length > 5 && (
                    <button onClick={() => navigate('/my-workers')} className="text-sm text-[#7895B2] font-semibold hover:underline">عرض الكل</button>
                  )}
                </div>
                {workerStatuses.length > 0 ? (
                  <div className="divide-y divide-[#E5E7EB]">
                    {workerStatuses.slice(0, 8).map((worker) => (
                      <div key={worker.userId} className="py-3 flex items-center justify-between">
                        <div className="flex items-center gap-2">
                          <span className={`w-2 h-2 rounded-full ${worker.status === 'CheckedIn' ? 'bg-[#8FA36A]' : 'bg-[#6B7280]/30'}`} />
                          <span className="text-xs font-semibold text-[#6B7280]">
                            {worker.status === 'CheckedIn' ? 'نشط' : 'غائب'}
                          </span>
                        </div>
                        <div className="text-right">
                          <p className="font-semibold text-sm text-[#2F2F2F]">{worker.fullName}</p>
                          <p className="text-[10px] text-[#6B7280]">{worker.zoneName || 'لا توجد منطقة'}</p>
                        </div>
                      </div>
                    ))}
                  </div>
                ) : (
                  <div className="h-full flex flex-col items-center justify-center text-center opacity-50 pb-10">
                    <Users size={32} className="mb-2 text-[#6B7280]" />
                    <p className="text-sm font-medium text-[#6B7280]">لا يوجد عمال متاحين</p>
                  </div>
                )}
              </div>

              {/* Quick Actions & Activity */}
              <div className="space-y-6">
                <div className="grid grid-cols-2 gap-6">
                  <button
                    onClick={() => navigate('/tasks/new')}
                    className="p-5 rounded-[16px] bg-[#7895B2] text-white text-right hover:shadow-lg transition-all active:scale-95 group"
                  >
                    <PlusCircle size={22} className="mb-3 opacity-70 group-hover:opacity-100" />
                    <h3 className="font-bold text-[16px]">إسناد مهمة</h3>
                    <p className="text-[12px] opacity-70">إضافة عمل جديد لعامل</p>
                  </button>
                  <button
                    onClick={() => navigate('/zones')}
                    className="p-5 rounded-[16px] bg-[#8FA36A] text-white text-right hover:shadow-lg transition-all active:scale-95 group"
                  >
                    <MapPin size={22} className="mb-3 opacity-70 group-hover:opacity-100" />
                    <h3 className="font-bold text-[16px]">الخريطة</h3>
                    <p className="text-[12px] opacity-70">توزيع المهام مكانياً</p>
                  </button>
                </div>

                <div className="bg-white rounded-[16px] p-6 shadow-[0_4px_20px_rgba(0,0,0,0.04)] min-h-[200px]">
                  <h2 className="text-[18px] font-bold text-[#2F2F2F] mb-5">آخر التحديثات</h2>
                  {activities.length > 0 ? (
                    <div className="space-y-4">
                      {activities.slice(0, 4).map((a) => (
                        <div key={a.id} className="flex items-start gap-3 text-right">
                          <div className="flex-1">
                            <p className="text-sm text-[#2F2F2F] font-medium">{a.text}</p>
                            <p className="text-[10px] text-[#6B7280]">{a.time}</p>
                          </div>
                          <div className="mt-1.5 w-1.5 h-1.5 rounded-full bg-[#7895B2]/40" />
                        </div>
                      ))}
                    </div>
                  ) : (
                    <div className="h-full flex flex-col items-center justify-center text-center opacity-50 pb-6">
                      <Activity size={24} className="mb-2 text-[#6B7280]" />
                      <p className="text-sm font-medium text-[#6B7280]">لا توجد تحديثات حديثة</p>
                    </div>
                  )}
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    );
};

export default SupervisorDashboard;

function DashboardStatItem({ icon: Icon, label, value, color }: { icon: React.ComponentType<{ size?: number }>; label: string; value: number | string | undefined; color: string }) {
  return (
    <div className="bg-white rounded-[24px] p-6 shadow-[0_4px_25px_rgba(0,0,0,0.03)] flex items-center justify-between border border-black/5 hover:border-[#7895B2]/30 transition-all group">
      <div className="text-right">
        <p className="text-[13px] text-[#AFAFAF] font-black uppercase mb-1 tracking-tight">{label}</p>
        <h3 className="text-3xl font-black text-[#2F2F2F] group-hover:scale-105 transition-transform origin-right">{value || 0}</h3>
      </div>
      <div className="w-14 h-14 rounded-2xl flex items-center justify-center bg-[#F9F8F6] text-[#7895B2] shadow-sm border border-black/5 group-hover:bg-[#7895B2] group-hover:text-white transition-colors duration-300" style={{ color: color }}>
        <Icon size={24} />
      </div>
    </div>
  );
}


