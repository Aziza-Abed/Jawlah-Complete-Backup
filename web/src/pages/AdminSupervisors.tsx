import { useEffect, useState } from "react";
import { getAdminSupervisorsMonitoring } from "../api/reports";
import type { AdminSupervisorMonitoringData, SupervisorMonitoringItem } from "../api/reports";
import { getWorkers, updateUser } from "../api/users";
import type { UserResponse } from "../types/user";
import {
  Users,
  UserCheck,
  ChevronLeft,
  Search,
  X,
  UserMinus,
  ArrowLeftRight,
  CheckCircle2,
  Loader2,
  AlertTriangle,
  Clock,
  ClipboardList,
  Activity,
  Bell
} from "lucide-react";

type StatusFilter = "all" | "Active" | "Inactive";
type PerformanceFilter = "all" | "Good" | "Warning" | "Critical";

export default function AdminSupervisors() {
  const [monitoringData, setMonitoringData] = useState<AdminSupervisorMonitoringData | null>(null);
  const [allWorkers, setAllWorkers] = useState<UserResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState("");
  const [statusFilter, setStatusFilter] = useState<StatusFilter>("all");
  const [performanceFilter, setPerformanceFilter] = useState<PerformanceFilter>("all");

  const [selectedSupervisor, setSelectedSupervisor] = useState<SupervisorMonitoringItem | null>(null);
  const [supervisorWorkers, setSupervisorWorkers] = useState<UserResponse[]>([]);
  const [loadingWorkers, setLoadingWorkers] = useState(false);

  const [showTransferModal, setShowTransferModal] = useState(false);
  const [selectedWorkerIds, setSelectedWorkerIds] = useState<number[]>([]);
  const [targetSupervisorId, setTargetSupervisorId] = useState<number | "">("");
  const [transferring, setTransferring] = useState(false);

  useEffect(() => {
    fetchData();
  }, []);

  const fetchData = async () => {
    try {
      setLoading(true);
      const [monitoring, workerData] = await Promise.all([
        getAdminSupervisorsMonitoring(),
        getWorkers()
      ]);
      setMonitoringData(monitoring);
      setAllWorkers(workerData);
    } catch (err) {
      console.error("Failed to fetch data", err);
    } finally {
      setLoading(false);
    }
  };

  const getWorkersForSupervisor = (supervisorId: number) => {
    return allWorkers.filter(w => w.supervisorId === supervisorId);
  };

  const openSupervisorDetail = async (supervisor: SupervisorMonitoringItem) => {
    setSelectedSupervisor(supervisor);
    setLoadingWorkers(true);
    try {
      const workers = getWorkersForSupervisor(supervisor.userId);
      setSupervisorWorkers(workers);
    } finally {
      setLoadingWorkers(false);
    }
  };

  const closeSupervisorDetail = () => {
    setSelectedSupervisor(null);
    setSupervisorWorkers([]);
    setSelectedWorkerIds([]);
  };

  const toggleWorkerSelection = (workerId: number) => {
    setSelectedWorkerIds(prev =>
      prev.includes(workerId)
        ? prev.filter(id => id !== workerId)
        : [...prev, workerId]
    );
  };

  const handleTransferWorkers = async () => {
    if (selectedWorkerIds.length === 0 || targetSupervisorId === "") return;

    setTransferring(true);
    try {
      await Promise.all(
        selectedWorkerIds.map(workerId =>
          updateUser(workerId, { supervisorId: Number(targetSupervisorId) })
        )
      );

      await fetchData();
      setShowTransferModal(false);
      setSelectedWorkerIds([]);
      setTargetSupervisorId("");

      if (selectedSupervisor) {
        const workers = getWorkersForSupervisor(selectedSupervisor.userId);
        setSupervisorWorkers(workers);
      }
    } catch (err) {
      console.error("Failed to transfer workers", err);
    } finally {
      setTransferring(false);
    }
  };

  const filteredSupervisors = monitoringData?.supervisors.filter(s => {
    const search = searchTerm.toLowerCase();
    const matchesSearch = (s.fullName?.toLowerCase() || "").includes(search) ||
                         (s.username?.toLowerCase() || "").includes(search);
    const matchesStatus = statusFilter === "all" || s.status === statusFilter;
    const matchesPerformance = performanceFilter === "all" || s.performanceStatus === performanceFilter;
    return matchesSearch && matchesStatus && matchesPerformance;
  }) || [];

  if (loading) {
    return (
      <div className="h-full w-full bg-[#F3F1ED] flex items-center justify-center">
        <div className="flex flex-col items-center gap-4">
          <div className="w-12 h-12 border-4 border-[#7895B2]/30 border-t-[#7895B2] rounded-full animate-spin"></div>
          <p className="text-[#6B7280] font-medium">جاري تحليل بيانات المشرفين...</p>
        </div>
      </div>
    );
  }

  const summary = monitoringData?.summary;


  return (
    <div className="h-full w-full bg-[#F3F1ED] overflow-auto">
      <div className="p-4 sm:p-6 md:p-8">
        <div className="max-w-[1400px] mx-auto space-y-6">
          {/* Header */}
          <div className="flex items-center gap-3">
            <div className="p-2.5 rounded-[12px] bg-[#7895B2]/20">
              <Users size={22} className="text-[#7895B2]" />
            </div>
            <div>
              <h1 className="font-sans font-bold text-[22px] sm:text-[24px] text-[#2F2F2F]">
                مراقبة المشرفين
              </h1>
              <p className="text-[13px] text-[#6B7280]">
                متابعة أداء المشرفين والعمال • {summary?.totalSupervisors || 0} مشرف
              </p>
            </div>
          </div>

          {/* Summary Cards */}
          <div className="grid grid-cols-2 md:grid-cols-4 gap-6">
            <SummaryCard icon={Users} label="إجمالي المشرفين" value={summary?.totalSupervisors} subtext={`${summary?.activeSupervisors} نشطين الآن`} color="#7895B2" />
            <SummaryCard icon={UserCheck} label="عمال الميدان" value={summary?.totalWorkers} subtext={`${summary?.activeWorkersToday} في الميدان اليوم`} color="#8FA36A" />
            <SummaryCard icon={ClipboardList} label="إجمالي المهام" value={summary?.totalTasksThisMonth} subtext={`${summary?.completedTasksThisMonth} مكتملة`} color="#7895B2" />
            <SummaryCard icon={Activity} label="معدل الإنجاز" value={`${summary?.overallCompletionRate}%`} subtext="أداء مستقر" color="#8FA36A" />
          </div>



          {/* Search and Filters */}
          <div className="bg-white rounded-[16px] p-4 shadow-[0_4px_20px_rgba(0,0,0,0.04)] flex flex-col sm:flex-row gap-4">
            <div className="flex-1 relative">
              <input
                type="text"
                placeholder="بحث عن مشرف..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="w-full h-[46px] pr-11 pl-4 bg-[#F3F1ED] rounded-[12px] text-right text-[14px] text-[#2F2F2F] placeholder:text-[#6B7280] border-0 outline-none focus:ring-2 focus:ring-[#7895B2]/30"
              />
              <Search className="absolute right-4 top-1/2 -translate-y-1/2 text-[#6B7280]" size={18} />
            </div>

            <select
              value={statusFilter}
              onChange={(e) => setStatusFilter(e.target.value as StatusFilter)}
              className="h-[46px] w-full sm:w-[150px] px-4 bg-[#F3F1ED] rounded-[12px] text-right text-[14px] text-[#2F2F2F] border-0 outline-none focus:ring-2 focus:ring-[#7895B2]/30 cursor-pointer"
            >
              <option value="all">جميع الحالات</option>
              <option value="Active">نشط</option>
              <option value="Inactive">غير نشط</option>
            </select>

            <select
              value={performanceFilter}
              onChange={(e) => setPerformanceFilter(e.target.value as PerformanceFilter)}
              className="h-[46px] w-full sm:w-[150px] px-4 bg-[#F3F1ED] rounded-[12px] text-right text-[14px] text-[#2F2F2F] border-0 outline-none focus:ring-2 focus:ring-[#7895B2]/30 cursor-pointer"
            >
              <option value="all">جميع الأداء</option>
              <option value="Good">جيد</option>
              <option value="Warning">تحذير</option>
              <option value="Critical">حرج</option>
            </select>
          </div>

          {/* Supervisors Grid */}
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
            {filteredSupervisors.map((supervisor) => (
              <div
                key={supervisor.userId}
                className="bg-white rounded-[24px] p-6 shadow-[0_4px_25px_rgba(0,0,0,0.03)] flex flex-col gap-5 cursor-pointer hover:shadow-[0_12px_40px_rgba(0,0,0,0.06)] hover:-translate-y-1 transition-all border border-black/5"
                onClick={() => openSupervisorDetail(supervisor)}
              >
                {/* Header */}
                <div className="flex items-center gap-4">
                  <div className={`w-14 h-14 rounded-[20px] shrink-0 flex items-center justify-center text-white shadow-sm font-black text-xl ${supervisor.status === 'Active' ? 'bg-[#7895B2]' : 'bg-[#AFAFAF]'}`}>
                    {supervisor.fullName.charAt(0)}
                  </div>

                  <div className="flex-1 min-w-0 text-right">
                    <h3 className="text-[17px] font-black text-[#2F2F2F] truncate">{supervisor.fullName}</h3>
                    <div className="flex items-center gap-2 justify-end mt-1.5">
                      <span className="text-[12px] font-bold text-[#7895B2] bg-[#7895B2]/5 px-2 py-0.5 rounded-lg border border-[#7895B2]/10">
                        {supervisor.workersCount} عامل تابع
                      </span>
                    </div>
                  </div>
                </div>

                {/* Simplified Stats Summary */}
                <div className="flex items-center justify-between gap-4 py-4 px-2 border border-black/5 bg-[#F9F8F6]/50 rounded-2xl">
                  <div className="text-center flex-1">
                    <p className="text-[10px] text-[#AFAFAF] uppercase font-black mb-1">المهام</p>
                    <p className="text-[15px] font-black text-[#2F2F2F]">{supervisor.tasksAssignedThisMonth}</p>
                  </div>
                  <div className="w-[1px] h-6 bg-black/5"></div>
                  <div className="text-center flex-1">
                    <p className="text-[10px] text-[#AFAFAF] uppercase font-black mb-1">المكتملة</p>
                    <p className="text-[15px] font-black text-[#8FA36A]">{supervisor.tasksCompletedThisMonth}</p>
                  </div>
                  <div className="w-[1px] h-6 bg-black/5"></div>
                  <div className="text-center flex-1">
                    <p className="text-[10px] text-[#AFAFAF] uppercase font-black mb-1">المتأخرة</p>
                    <p className="text-[15px] font-black text-[#C86E5D]">{supervisor.tasksDelayed}</p>
                  </div>
                </div>

                <div className="flex justify-end pt-2">
                  <button className="text-[14px] font-black text-[#7895B2] hover:text-[#647e99] transition-colors flex items-center gap-2 group">
                    عرض لوحة المتابعة الكاملة
                    <ChevronLeft size={18} className="transition-transform group-hover:-translate-x-1" />
                  </button>
                </div>
              </div>
            ))}
          </div>

          {filteredSupervisors.length === 0 && (
            <div className="bg-white rounded-[16px] p-8 shadow-[0_4px_20px_rgba(0,0,0,0.04)] text-center">
              <Users size={48} className="text-[#7895B2]/20 mx-auto mb-3" />
              <p className="text-[#6B7280] text-[15px] font-medium">لا يوجد مشرفين لعرضهم</p>
            </div>
          )}
        </div>
      </div>

      {/* Detail Slide Panel */}
      {selectedSupervisor && (
        <div className="fixed inset-0 z-50 flex">
          <div
            className="absolute inset-0 bg-black/40"
            onClick={closeSupervisorDetail}
          />

          <div className="absolute left-0 top-0 h-full w-full max-w-xl bg-white border-r border-[#E5E7EB] shadow-2xl overflow-y-auto animate-slide-in-left">
            <div className="sticky top-0 bg-white/80 backdrop-blur-md border-b border-black/5 p-6 flex items-center justify-between z-10">
              <h2 className="text-[18px] font-black text-[#2F2F2F]">تفاصيل المشرف الميداني</h2>
              <button
                onClick={closeSupervisorDetail}
                className="w-10 h-10 rounded-xl bg-[#F3F1ED] flex items-center justify-center hover:bg-white hover:shadow-lg transition-all border border-black/5 group"
              >
                <X size={20} className="text-[#6B7280] group-hover:scale-110 transition-transform" />
              </button>
            </div>

            <div className="p-8 space-y-6">
              {/* Supervisor Info Header */}
              <div className="flex flex-col items-center text-center space-y-4">
                <div className="relative">
                  <div className="w-24 h-24 rounded-[32px] bg-[#7895B2] flex items-center justify-center text-3xl font-black text-white shadow-xl shadow-[#7895B2]/20 border-4 border-white">
                    {selectedSupervisor.fullName.charAt(0)}
                  </div>
                  <div className={`absolute -bottom-1 -right-1 w-6 h-6 rounded-full border-4 border-white ${selectedSupervisor.status === 'Active' ? 'bg-[#8FA36A]' : 'bg-[#C86E5D]'}`}></div>
                </div>
                
                <div>
                  <h3 className="text-2xl font-black text-[#2F2F2F] mb-1">{selectedSupervisor.fullName}</h3>
                  <div className="flex items-center gap-2 justify-center">
                    <span className="text-[13px] font-bold text-[#7895B2] bg-[#7895B2]/5 px-3 py-1 rounded-full border border-[#7895B2]/10 dir-ltr">
                      @{selectedSupervisor.username}
                    </span>
                    <span className={`text-[12px] font-black px-3 py-1 rounded-full ${
                      selectedSupervisor.status === 'Active' ? 'bg-[#8FA36A]/10 text-[#8FA36A]' : 'bg-[#C86E5D]/10 text-[#C86E5D]'
                    }`}>
                      {selectedSupervisor.status === 'Active' ? 'حساب نشط' : 'حساب معطل'}
                    </span>
                  </div>
                </div>
              </div>

              {/* Performance Stats Cards */}
              <div className="grid grid-cols-2 gap-4">
                <DetailStatCard 
                   icon={Activity} 
                   label="معدل الإنجاز" 
                   value={`${selectedSupervisor.completionRate}%`} 
                   color="#8FA36A" 
                   bgColor="bg-[#8FA36A]/5"
                />
                <DetailStatCard 
                   icon={Clock} 
                   label="متوسط الإنجاز" 
                   value={`${selectedSupervisor.avgResponseTimeHours}س`} 
                   color="#7895B2" 
                   bgColor="bg-[#7895B2]/5"
                />
                <DetailStatCard 
                   icon={AlertTriangle} 
                   label="مهام متأخرة" 
                   value={selectedSupervisor.tasksDelayed} 
                   color="#C86E5D" 
                   bgColor="bg-[#C86E5D]/5"
                />
                <DetailStatCard 
                   icon={Bell} 
                   label="بلاغات معلقة" 
                   value={selectedSupervisor.issuesPending} 
                   color="#F5B300" 
                   bgColor="bg-[#F5B300]/5"
                />
              </div>

              {/* Monthly Overview Table */}
              <div className="bg-[#F9F8F6] rounded-[24px] p-6 border border-black/5">
                <h4 className="text-[14px] font-black text-[#2F2F2F] mb-5 text-right flex items-center justify-end gap-2">
                  إحصائيات المهام الشهرية
                  <div className="w-1.5 h-1.5 rounded-full bg-[#7895B2]"></div>
                </h4>
                <div className="grid grid-cols-3 gap-4 text-center">
                  <div className="space-y-1">
                    <p className="text-[10px] text-[#AFAFAF] font-black uppercase">موكلة</p>
                    <p className="text-xl font-black text-[#2F2F2F]">{selectedSupervisor.tasksAssignedThisMonth}</p>
                  </div>
                  <div className="w-[1px] h-8 bg-black/5 mt-2"></div>
                  <div className="space-y-1">
                    <p className="text-[10px] text-[#AFAFAF] font-black uppercase">مراجعة</p>
                    <p className="text-xl font-black text-[#F5B300]">{selectedSupervisor.tasksPendingReview}</p>
                  </div>
                </div>
              </div>

              {/* Workers Management Section */}
              <div className="space-y-4">
                <div className="flex items-center justify-between flex-row-reverse pb-2 border-b border-black/5">
                  <h4 className="text-[16px] font-black text-[#2F2F2F] flex items-center gap-2">
                    العمال المسندين للمشرف ({supervisorWorkers.length})
                    <Users size={18} className="text-[#7895B2]" />
                  </h4>
                  {selectedWorkerIds.length > 0 && (
                    <button
                      onClick={() => setShowTransferModal(true)}
                      className="px-4 py-2 rounded-xl bg-[#7895B2] text-white hover:bg-[#647e99] transition-all text-[12px] font-black shadow-lg shadow-[#7895B2]/20 flex items-center gap-2"
                    >
                      <ArrowLeftRight size={14} />
                      نقل العمال ({selectedWorkerIds.length})
                    </button>
                  )}
                </div>

                {loadingWorkers ? (
                  <div className="flex justify-center py-12">
                    <Loader2 className="w-10 h-10 text-[#7895B2] animate-spin" />
                  </div>
                ) : supervisorWorkers.length === 0 ? (
                  <div className="text-center py-12 bg-[#F9F8F6] rounded-[24px] border border-dashed border-black/10">
                    <UserMinus size={40} className="mx-auto mb-3 text-[#AFAFAF]" />
                    <p className="text-[#6B7280] font-bold">لا يوجد عمال تحت إشراف هذا الحساب</p>
                  </div>
                ) : (
                  <div className="space-y-3">
                    {supervisorWorkers.map(worker => (
                      <div
                        key={worker.userId}
                        onClick={() => toggleWorkerSelection(worker.userId)}
                        className={`group flex items-center gap-4 flex-row-reverse p-4 rounded-[20px] transition-all cursor-pointer border ${
                          selectedWorkerIds.includes(worker.userId)
                            ? 'bg-[#7895B2]/10 border-[#7895B2]/30 shadow-sm'
                            : 'bg-white border-black/5 hover:bg-[#F9F8F6] hover:shadow-md'
                        }`}
                      >
                        <div className={`w-6 h-6 rounded-lg border-2 flex items-center justify-center transition-all ${
                          selectedWorkerIds.includes(worker.userId)
                            ? 'bg-[#7895B2] border-[#7895B2]'
                            : 'bg-[#F3F1ED] border-black/5'
                        }`}>
                          {selectedWorkerIds.includes(worker.userId) && (
                            <CheckCircle2 size={16} className="text-white" />
                          )}
                        </div>

                        <div className="flex-1 text-right">
                          <p className="font-black text-[#2F2F2F] text-[15px]">{worker.fullName}</p>
                          <div className="flex items-center gap-2 justify-end mt-1">
                             <span className="text-[11px] font-bold text-[#AFAFAF]">{worker.phoneNumber}</span>
                             <span className="w-1 h-1 rounded-full bg-black/10"></span>
                             <span className="text-[11px] font-black text-[#7895B2]">{worker.workerType === 'Sanitation' ? 'صحة ونظافة' : worker.workerType}</span>
                          </div>
                        </div>

                        <div className={`w-2.5 h-2.5 rounded-full ${worker.status === 'Active' ? 'bg-[#8FA36A]' : 'bg-[#C86E5D]'}`}></div>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Transfer Modal */}
      {showTransferModal && (
        <div className="fixed inset-0 z-[60] flex items-center justify-center p-4">
          <div
            className="absolute inset-0 bg-black/40"
            onClick={() => setShowTransferModal(false)}
          />

          <div className="relative bg-white rounded-[16px] p-6 w-full max-w-md shadow-xl">
            <h3 className="text-[18px] font-bold text-[#2F2F2F] text-right mb-4">نقل العمال</h3>

            <p className="text-[#6B7280] text-right mb-4 text-[14px]">
              سيتم نقل {selectedWorkerIds.length} عامل إلى المشرف المحدد
            </p>

            <div className="mb-6">
              <label className="block text-[12px] font-semibold text-[#6B7280] text-right mb-2">
                المشرف الجديد
              </label>
              <select
                value={targetSupervisorId}
                onChange={(e) => setTargetSupervisorId(e.target.value ? Number(e.target.value) : "")}
                className="w-full h-[46px] px-4 bg-[#F3F1ED] rounded-[12px] text-right text-[14px] text-[#2F2F2F] border-0 outline-none focus:ring-2 focus:ring-[#7895B2]/30"
                dir="rtl"
              >
                <option value="">اختر المشرف...</option>
                {monitoringData?.supervisors
                  .filter(s => s.userId !== selectedSupervisor?.userId && s.status === 'Active')
                  .map(s => (
                    <option key={s.userId} value={s.userId}>
                      {s.fullName} ({s.workersCount} عامل)
                    </option>
                  ))
                }
              </select>
            </div>

            <div className="flex gap-3 flex-row-reverse">
              <button
                onClick={handleTransferWorkers}
                disabled={transferring || targetSupervisorId === ""}
                className="flex-1 py-3 rounded-[12px] bg-[#7895B2] text-white font-semibold hover:bg-[#6B87A3] transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2"
              >
                {transferring ? (
                  <>
                    <Loader2 size={16} className="animate-spin" />
                    جاري النقل...
                  </>
                ) : (
                  <>
                    <ArrowLeftRight size={16} />
                    نقل العمال
                  </>
                )}
              </button>
              <button
                onClick={() => setShowTransferModal(false)}
                className="flex-1 py-3 rounded-[12px] border border-[#E5E7EB] text-[#2F2F2F] font-semibold hover:bg-[#F3F1ED] transition-colors"
              >
                إلغاء
              </button>
            </div>
          </div>
        </div>
      )}

      <style>{`
        @keyframes slide-in-left {
          from {
            transform: translateX(-100%);
          }
          to {
            transform: translateX(0);
          }
        }
        .animate-slide-in-left {
          animation: slide-in-left 0.3s ease-out;
        }
      `}</style>
    </div>
  );
}

function SummaryCard({ icon: Icon, label, value, subtext, color }: { icon: React.ComponentType<{ size?: number }>; label: string; value: number | string | undefined; subtext: string; color: string }) {
  return (
    <div className="bg-white rounded-[24px] p-6 shadow-[0_4px_25px_rgba(0,0,0,0.03)] border border-black/5 flex flex-col items-center text-center">
      <div className="w-12 h-12 rounded-[16px] flex items-center justify-center bg-[#F9F8F6] text-[#7895B2] mb-4 shadow-sm border border-black/5" style={{ color: color }}>
        <Icon size={24} />
      </div>
      <p className="text-[11px] font-black text-[#AFAFAF] uppercase tracking-wider mb-1">{label}</p>
      <p className="text-3xl font-black text-[#2F2F2F] mb-1">{value || 0}</p>
      <p className="text-[12px] font-bold text-[#6B7280]">{subtext}</p>
    </div>
  );
}

function DetailStatCard({ icon: Icon, label, value, color, bgColor }: { icon: React.ComponentType<{ size?: number }>; label: string; value: number | string | undefined; color: string; bgColor: string }) {
  return (
    <div className={`${bgColor} rounded-[24px] p-5 border border-black/5 flex flex-col items-center text-center`}>
      <div className="w-10 h-10 rounded-[14px] bg-white flex items-center justify-center mb-3 shadow-sm" style={{ color: color }}>
        <Icon size={20} />
      </div>
      <p className="text-[10px] text-[#AFAFAF] font-black uppercase mb-1">{label}</p>
      <p className="text-xl font-black text-[#2F2F2F]">{value || 0}</p>
    </div>
  );
}
