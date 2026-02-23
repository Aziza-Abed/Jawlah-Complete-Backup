import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { STORAGE_KEYS } from "../constants/storageKeys";
import {
  Users, ClipboardList, AlertTriangle, Activity, Shield,
  MapPin, FileText, BarChart3, History, UserCog, Building,
  Clock
} from "lucide-react";
import { getDashboardOverview, getWorkerStatuses } from "../api/dashboard";
import { getAdminSupervisorsMonitoring } from "../api/reports";
import type { DashboardOverview, WorkerStatus } from "../types/dashboard";
import type { AdminSupervisorMonitoringData } from "../api/reports";

export default function AdminDashboard() {
  const navigate = useNavigate();
  const [overview, setOverview] = useState<DashboardOverview | null>(null);
  const [workerStatuses, setWorkerStatuses] = useState<WorkerStatus[]>([]);
  const [monitoringData, setMonitoringData] = useState<AdminSupervisorMonitoringData | null>(null);
  const [loading, setLoading] = useState(true);
  const [userName, setUserName] = useState("مدير");

  useEffect(() => {
    try {
      const userStr = localStorage.getItem(STORAGE_KEYS.USER);
      if (userStr) {
        const user = JSON.parse(userStr);
        setUserName(user.fullName?.split(' ')[0] || "مدير");
      }
    } catch (e) {}

    const fetchData = async () => {
      try {
        setLoading(true);
        const [data, workersData, adminMonData] = await Promise.all([
          getDashboardOverview(),
          getWorkerStatuses(),
          getAdminSupervisorsMonitoring()
        ]);
        setOverview(data);
        setWorkerStatuses(workersData);
        setMonitoringData(adminMonData);
      } catch (err) {
        console.error("Failed to fetch dashboard data", err);
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, []);

  if (loading) {
    return (
      <div className="h-full w-full bg-[#F3F1ED] flex items-center justify-center">
        <div className="flex flex-col items-center gap-4">
          <div className="w-12 h-12 border-4 border-[#7895B2]/30 border-t-[#7895B2] rounded-full animate-spin"></div>
          <p className="text-[#6B7280] font-bold">جاري تحميل البيانات...</p>
        </div>
      </div>
    );
  }

  const attendancePercent = Math.round(
    ((overview?.workers.checkedIn || 0) / (overview?.workers.total || 1)) * 100
  );

  return (
    <div className="h-full w-full bg-[#F3F1ED] overflow-auto">
      <div className="p-4 sm:p-6 md:p-8">
        <div className="max-w-[1400px] mx-auto space-y-10">
          
          {/* Header with Title (Title Left, Date Right) */}
          <div className="flex flex-col lg:flex-row items-center justify-between gap-6">
              <div className="w-full lg:w-auto text-right">
                <h1 className="text-4xl font-black text-[#2F2F2F] tracking-tight">نظرة عامة</h1>
                <p className="text-[#6B7280] text-[15px] mt-1.5 font-medium">مرحباً بك، {userName}. إليك ملخص أداء البلدية اليوم.</p>
              </div>

              <div className="flex items-center gap-3">
                 <div className="bg-white px-5 py-3 rounded-2xl flex items-center justify-center text-[#6B7280] font-black text-[14px] border border-black/5 shadow-sm">
                    {new Date().toLocaleDateString('ar-EG', { weekday: 'long', day: 'numeric', month: 'long' })}
                 </div>
              </div>
          </div>

          {/* Quick Stats Grid (New Premium Style) */}
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
            <StatCard 
              label="إجمالي القوة العاملة" 
              value={monitoringData?.summary.totalWorkers || overview?.workers.total || 0} 
              icon={Users} 
              color="#7895B2" 
              trend="موظف وعامل"
            />
            <StatCard 
              label="عدد المشرفين" 
              value={monitoringData?.summary.totalSupervisors || 0} 
              icon={Shield} 
              color="#8FA36A" 
              trend="نشط حالياً"
            />
            <StatCard 
              label="مهام الشهر" 
              value={monitoringData?.summary.totalTasksThisMonth || 0} 
              icon={ClipboardList} 
              color="#F3C668" 
              trend="إجمالي التكليفات"
            />
            <StatCard 
              label="نسبة الإنجاز" 
              value={`${monitoringData?.summary.overallCompletionRate || 0}%`} 
              icon={BarChart3} 
              color="#C86E5D" 
              trend="معدل كفاءة القسم"
            />
          </div>

          {/* Main Content Grid */}
          <div className="grid grid-cols-1 xl:grid-cols-12 gap-8">
            
            {/* Left Column (8 units) */}
            <div className="xl:col-span-8 space-y-6">
                
                {/* Visual Monitoring Section */}
                <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
                  {/* Attendance Visualization */}
                  <div className="bg-white rounded-[24px] p-8 shadow-[0_4px_25px_rgba(0,0,0,0.03)] border border-black/5">
                    <div className="flex items-center justify-between mb-8">
                      <h2 className="text-[18px] font-black text-[#2F2F2F]">معدلات الحضور</h2>
                      <Activity size={20} className="text-[#8FA36A]" />
                    </div>
                    <div className="flex flex-col sm:flex-row items-center gap-10">
                       <div className="relative w-36 h-36">
                          <svg className="w-full h-full -rotate-90" viewBox="0 0 100 100">
                            <circle cx="50" cy="50" r="42" fill="none" stroke="#F3F1ED" strokeWidth="8" />
                            <circle
                              cx="50" cy="50" r="42" fill="none" stroke="#8FA36A" strokeWidth="8"
                              strokeDasharray={`${attendancePercent * 2.64} 264`}
                              strokeLinecap="round"
                            />
                          </svg>
                          <div className="absolute inset-0 flex flex-col items-center justify-center">
                            <span className="text-3xl font-black text-[#2F2F2F]">{attendancePercent}%</span>
                            <span className="text-[10px] text-[#6B7280] font-bold uppercase tracking-wider">الحضور</span>
                          </div>
                       </div>
                       <div className="flex-1 w-full space-y-4">
                          <DetailRow label="متواجدون" value={overview?.workers.checkedIn || 0} color="#8FA36A" />
                          <DetailRow label="غائبون" value={overview?.workers.notCheckedIn || 0} color="#C86E5D" />
                          <div className="pt-4 border-t border-black/5 mt-4">
                             <p className="text-[11px] text-[#6B7280] leading-relaxed">
                                يتم تحديث البيانات فورياً عند قيام العمال والمشرفين بتسجيل الحضور عبر التطبيق.
                             </p>
                          </div>
                       </div>
                    </div>
                  </div>

                  {/* Tasks Summary Card */}
                  <div className="bg-white rounded-[24px] p-8 shadow-[0_4px_25px_rgba(0,0,0,0.03)] border border-black/5">
                    <div className="flex items-center justify-between mb-8">
                      <h2 className="text-[18px] font-black text-[#2F2F2F]">ملخص العمليات</h2>
                      <ClipboardList size={20} className="text-[#7895B2]" />
                    </div>
                    <div className="space-y-4">
                        <TaskProgressRow label="قيد التنفيذ" value={overview?.tasks.inProgress || 0} total={overview?.tasks.createdToday || 1} color="#F3C668" />
                        <TaskProgressRow label="مكتملة اليوم" value={overview?.tasks.completedToday || 0} total={overview?.tasks.createdToday || 1} color="#8FA36A" />
                        <TaskProgressRow label="بانتظار العمل" value={overview?.tasks.pending || 0} total={overview?.tasks.createdToday || 1} color="#C86E5D" />
                        <button 
                          onClick={() => navigate('/tasks')}
                          className="w-full mt-4 h-12 rounded-xl bg-[#7895B2]/5 text-[#7895B2] font-black text-[13px] hover:bg-[#7895B2]/10 transition-colors"
                        >
                          إدارة كافة المهام
                        </button>
                    </div>
                  </div>
                </div>

                {/* Quick Management Section */}
                <div className="bg-white rounded-[24px] p-8 shadow-[0_4px_25px_rgba(0,0,0,0.03)] border border-black/5">
                  <h2 className="text-[18px] font-black text-[#2F2F2F] mb-8">إدارة النظام السريعة</h2>
                  <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-4">
                    <ManagerAction icon={Users} label="الحسابات" onClick={() => navigate('/accounts')} />
                    <ManagerAction icon={UserCog} label="المشرفين" onClick={() => navigate('/supervisors')} />
                    <ManagerAction icon={Building} label="الأقسام" onClick={() => navigate('/departments')} />
                    <ManagerAction icon={MapPin} label="المناطق" onClick={() => navigate('/zones-admin')} />
                    <ManagerAction icon={FileText} label="التقارير" onClick={() => navigate('/reports')} />
                    <ManagerAction icon={History} label="السجل" onClick={() => navigate('/audit-logs')} />
                  </div>
                </div>
            </div>

            {/* Right Column (4 units) */}
            <div className="xl:col-span-4 space-y-6">
                
                {/* Issues/Alerts Widget */}
                <div className="bg-[#C86E5D] rounded-[24px] p-8 shadow-xl shadow-[#C86E5D]/20 text-white group overflow-hidden relative">
                   <div className="absolute top-0 left-0 w-32 h-32 bg-white/10 rounded-full -translate-x-1/2 -translate-y-1/2 group-hover:scale-150 transition-transform duration-700"></div>
                   <div className="relative z-10">
                      <div className="flex items-center justify-between mb-6">
                        <AlertTriangle size={24} />
                        <span className="bg-white/20 px-3 py-1 rounded-full text-[11px] font-black uppercase tracking-widest">تنبيه حرج</span>
                      </div>
                      <h2 className="text-4xl font-black mb-2">{overview?.issues.unresolved || 0}</h2>
                      <p className="text-white/80 font-bold text-[15px] mb-8">بلاغات نشطة لم يتم إغلاقها حتى الآن.</p>
                      <button 
                        onClick={() => navigate('/issues')}
                        className="w-full h-14 bg-white text-[#C86E5D] rounded-2xl font-black text-[16px] shadow-lg hover:shadow-2xl transition-all hover:-translate-y-1"
                      >
                        معالجة البلاغات الآن
                      </button>
                   </div>
                </div>

                {/* Top Performers / Activity List */}
                <div className="bg-white rounded-[24px] p-8 shadow-[0_4px_25px_rgba(0,0,0,0.03)] border border-black/5 min-h-[400px]">
                  <div className="flex items-center justify-between mb-8">
                    <h2 className="text-[18px] font-black text-[#2F2F2F]">الأكثر تنفيذاً</h2>
                    <BarChart3 size={20} className="text-[#8FA36A]" />
                  </div>
                  <div className="space-y-4">
                    {workerStatuses
                      .filter(w => w.completedTasksCount > 0)
                      .sort((a, b) => b.completedTasksCount - a.completedTasksCount)
                      .slice(0, 6)
                      .map((worker) => (
                        <div key={worker.userId} className="flex items-center justify-between p-4 rounded-2xl bg-[#F3F1ED]/50 border border-black/5 hover:bg-[#F3F1ED] transition-colors group">
                          <div className="text-right">
                            <p className="font-black text-[14px] text-[#2F2F2F]">{worker.fullName}</p>
                            <p className="text-[11px] text-[#6B7280] font-bold">{worker.zoneName || 'المنطقة العامة'}</p>
                          </div>
                          <div className="flex flex-col items-center">
                            <span className="text-[16px] font-black text-[#8FA36A]">{worker.completedTasksCount}</span>
                            <span className="text-[9px] text-[#6B7280] font-black uppercase leading-none">مهمة</span>
                          </div>
                        </div>
                      ))}
                    {workerStatuses.length === 0 && (
                      <div className="text-center py-10 opacity-40">
                         <Clock size={32} className="mx-auto mb-2" />
                         <p className="text-sm font-bold">لا توجد بيانات حالياً</p>
                      </div>
                    )}
                  </div>
                </div>

            </div>

          </div>
        </div>
      </div>
    </div>
  );
}

/* -------- Helper UI Components -------- */

function StatCard({ label, value, icon: Icon, color, trend }: { label: string; value: number | string; icon: React.ComponentType<{ size?: number; style?: React.CSSProperties }>; color: string; trend: string }) {
  return (
    <div className="bg-white rounded-[24px] p-6 shadow-[0_4px_25px_rgba(0,0,0,0.03)] border border-black/5 flex items-center justify-between group hover:border-[#7895B2]/30 transition-all">
      <div className="text-right">
        <h3 className="text-3xl font-black text-[#2F2F2F] group-hover:scale-105 transition-transform origin-right">{value}</h3>
        <p className="text-[13px] text-[#6B7280] font-black mt-1 uppercase tracking-tight">{label}</p>
        <div className="mt-3 flex items-center gap-1.5">
           <div className="w-1.5 h-1.5 rounded-full" style={{ backgroundColor: color }}></div>
           <span className="text-[11px] font-bold text-[#AFAFAF]">{trend}</span>
        </div>
      </div>
      <div className="p-5 rounded-2xl" style={{ backgroundColor: `${color}15` }}>
        <Icon size={28} style={{ color: color }} />
      </div>
    </div>
  );
}

function DetailRow({ label, value, color }: { label: string; value: number | string; color: string }) {
  return (
    <div className="flex items-center justify-between">
       <div className="flex items-center gap-3">
          <div className="w-2.5 h-2.5 rounded-full" style={{ backgroundColor: color }}></div>
          <span className="text-[15px] font-bold text-[#6B7280]">{label}</span>
       </div>
       <span className="text-[16px] font-black text-[#2F2F2F]">{value}</span>
    </div>
  );
}

function TaskProgressRow({ label, value, total, color }: { label: string; value: number; total: number; color: string }) {
  const percent = Math.min(100, Math.round((value / total) * 100));
  return (
    <div className="space-y-3">
       <div className="flex justify-between items-end">
          <span className="text-[13px] font-bold text-[#6B7280]">{label}</span>
          <span className="text-[15px] font-black text-[#2F2F2F]">{value} <span className="text-[11px] text-[#AFAFAF] font-bold">/ {total}</span></span>
       </div>
       <div className="h-2 w-full bg-[#F3F1ED] rounded-full overflow-hidden">
          <div className="h-full rounded-full transition-all duration-1000" style={{ width: `${percent}%`, backgroundColor: color }}></div>
       </div>
    </div>
  );
}

function ManagerAction({ icon: Icon, label, onClick }: { icon: React.ComponentType<{ size?: number; className?: string }>; label: string; onClick: () => void }) {
  return (
    <button 
      onClick={onClick}
      className="flex flex-col items-center gap-3 p-5 rounded-[22px] bg-[#F3F1ED]/50 hover:bg-[#7895B2] group transition-all"
    >
      <div className="p-3 rounded-xl bg-white shadow-sm group-hover:bg-[#7895B2] transition-colors">
        <Icon size={22} className="text-[#7895B2] group-hover:text-white" />
      </div>
      <span className="text-[13px] font-black text-[#2F2F2F] group-hover:text-white">{label}</span>
    </button>
  );
}
