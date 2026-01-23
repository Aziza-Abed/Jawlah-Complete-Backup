import { useEffect, useState } from "react";
import { getAdminSupervisorsMonitoring } from "../api/reports";
import type { AdminSupervisorMonitoringData, SupervisorMonitoringItem } from "../api/reports";
import { getWorkers, updateUser } from "../api/users";
import type { UserResponse } from "../types/user";
import {
  Users,
  AlertCircle,
  TrendingUp,
  UserCheck,
  ChevronLeft,
  Search,
  X,
  Filter,
  UserMinus,
  ArrowLeftRight,
  Phone,
  CheckCircle2,
  XCircle,
  Loader2,
  AlertTriangle,
  Clock,
  ClipboardList,
  Activity,
  Bell,
  ChevronDown,
  ChevronUp,
  BarChart3,
  UserX
} from "lucide-react";
import GlassCard from "../components/UI/GlassCard";

type StatusFilter = "all" | "Active" | "Inactive";
type PerformanceFilter = "all" | "Good" | "Warning" | "Critical";

export default function AdminSupervisors() {
  const [monitoringData, setMonitoringData] = useState<AdminSupervisorMonitoringData | null>(null);
  const [allWorkers, setAllWorkers] = useState<UserResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState("");
  const [statusFilter, setStatusFilter] = useState<StatusFilter>("all");
  const [performanceFilter, setPerformanceFilter] = useState<PerformanceFilter>("all");
  const [showAlerts, setShowAlerts] = useState(true);

  // Detail panel state
  const [selectedSupervisor, setSelectedSupervisor] = useState<SupervisorMonitoringItem | null>(null);
  const [supervisorWorkers, setSupervisorWorkers] = useState<UserResponse[]>([]);
  const [loadingWorkers, setLoadingWorkers] = useState(false);

  // Transfer modal state
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

  // Get workers for a specific supervisor
  const getWorkersForSupervisor = (supervisorId: number) => {
    return allWorkers.filter(w => w.supervisorId === supervisorId);
  };

  // Open detail panel
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

  // Close detail panel
  const closeSupervisorDetail = () => {
    setSelectedSupervisor(null);
    setSupervisorWorkers([]);
    setSelectedWorkerIds([]);
  };

  // Toggle worker selection for transfer
  const toggleWorkerSelection = (workerId: number) => {
    setSelectedWorkerIds(prev =>
      prev.includes(workerId)
        ? prev.filter(id => id !== workerId)
        : [...prev, workerId]
    );
  };

  // Transfer selected workers
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

  // Filter supervisors
  const filteredSupervisors = monitoringData?.supervisors.filter(s => {
    const matchesSearch = s.fullName.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         s.username.toLowerCase().includes(searchTerm.toLowerCase());
    const matchesStatus = statusFilter === "all" || s.status === statusFilter;
    const matchesPerformance = performanceFilter === "all" || s.performanceStatus === performanceFilter;
    return matchesSearch && matchesStatus && matchesPerformance;
  }) || [];

  const getPerformanceColor = (status: string) => {
    switch (status) {
      case "Good": return "text-secondary bg-secondary/10 border-secondary/20";
      case "Warning": return "text-warning bg-warning/10 border-warning/20";
      case "Critical": return "text-accent bg-accent/10 border-accent/20";
      default: return "text-text-muted bg-primary/5 border-primary/10";
    }
  };

  const getPerformanceIcon = (status: string) => {
    switch (status) {
      case "Good": return <CheckCircle2 size={14} />;
      case "Warning": return <AlertTriangle size={14} />;
      case "Critical": return <XCircle size={14} />;
      default: return <Activity size={14} />;
    }
  };

  const getAlertIcon = (type: string) => {
    switch (type) {
      case "TooManyWorkers": return <Users size={16} />;
      case "PerformanceDrop": return <TrendingUp size={16} />;
      case "HighDelayRate": return <Clock size={16} />;
      case "LowActivity": return <UserX size={16} />;
      default: return <AlertCircle size={16} />;
    }
  };

  if (loading) {
    return (
      <div className="h-full w-full flex items-center justify-center">
        <div className="flex flex-col items-center gap-4">
          <div className="w-12 h-12 border-4 border-primary/30 border-t-primary rounded-full animate-spin"></div>
          <p className="text-text-secondary font-medium">جاري تحليل بيانات المشرفين...</p>
        </div>
      </div>
    );
  }

  const summary = monitoringData?.summary;
  const alerts = monitoringData?.alerts || [];
  const criticalAlerts = alerts.filter(a => a.severity === "Critical");
  const warningAlerts = alerts.filter(a => a.severity === "Warning");

  return (
    <div className="space-y-6 pb-10">
      {/* Header */}
      <div className="flex flex-col items-start animate-fade-in">
        <h1 className="text-3xl font-extrabold text-text-primary">
          مراقبة المشرفين
        </h1>
        <p className="text-right text-text-secondary mt-2 font-medium">
          متابعة أداء المشرفين والعمال • {summary?.totalSupervisors || 0} مشرف
        </p>
      </div>

      {/* Summary Cards */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <GlassCard className="text-center">
          <div className="flex items-center justify-center gap-2 mb-2">
            <Users size={20} className="text-primary" />
            <span className="text-sm font-bold text-text-muted">المشرفين</span>
          </div>
          <div className="text-3xl font-extrabold text-text-primary">{summary?.activeSupervisors}</div>
          <div className="text-xs text-text-muted">من {summary?.totalSupervisors} نشط</div>
        </GlassCard>

        <GlassCard className="text-center">
          <div className="flex items-center justify-center gap-2 mb-2">
            <UserCheck size={20} className="text-secondary" />
            <span className="text-sm font-bold text-text-muted">العمال النشطين</span>
          </div>
          <div className="text-3xl font-extrabold text-secondary">{summary?.activeWorkersToday}</div>
          <div className="text-xs text-text-muted">من {summary?.totalWorkers} اليوم</div>
        </GlassCard>

        <GlassCard className="text-center">
          <div className="flex items-center justify-center gap-2 mb-2">
            <ClipboardList size={20} className="text-primary" />
            <span className="text-sm font-bold text-text-muted">المهام الشهرية</span>
          </div>
          <div className="text-3xl font-extrabold text-text-primary">{summary?.completedTasksThisMonth}</div>
          <div className="text-xs text-text-muted">من {summary?.totalTasksThisMonth} مكتملة</div>
        </GlassCard>

        <GlassCard className="text-center">
          <div className="flex items-center justify-center gap-2 mb-2">
            <BarChart3 size={20} className="text-secondary" />
            <span className="text-sm font-bold text-text-muted">معدل الإنجاز</span>
          </div>
          <div className="text-3xl font-extrabold text-secondary">{summary?.overallCompletionRate}%</div>
          <div className="text-xs text-text-muted">هذا الشهر</div>
        </GlassCard>
      </div>

      {/* Alerts Section */}
      {alerts.length > 0 && (
        <GlassCard className="border-l-4 border-l-warning">
          <div
            className="flex items-center justify-between cursor-pointer"
            onClick={() => setShowAlerts(!showAlerts)}
          >
            <button className="p-1 hover:bg-primary/5 rounded-lg transition-colors">
              {showAlerts ? <ChevronUp size={20} /> : <ChevronDown size={20} />}
            </button>
            <div className="flex items-center gap-3">
              <div className="flex items-center gap-2">
                {criticalAlerts.length > 0 && (
                  <span className="px-2 py-0.5 rounded-full text-xs font-bold bg-accent/20 text-accent">
                    {criticalAlerts.length} حرج
                  </span>
                )}
                {warningAlerts.length > 0 && (
                  <span className="px-2 py-0.5 rounded-full text-xs font-bold bg-warning/20 text-warning">
                    {warningAlerts.length} تحذير
                  </span>
                )}
              </div>
              <h3 className="text-lg font-bold text-text-primary flex items-center gap-2">
                <Bell size={20} className="text-warning" />
                التنبيهات ({alerts.length})
              </h3>
            </div>
          </div>

          {showAlerts && (
            <div className="mt-4 space-y-2">
              {alerts.map(alert => (
                <div
                  key={alert.id}
                  className={`flex items-center gap-3 p-3 rounded-lg border flex-row-reverse ${
                    alert.severity === "Critical"
                      ? "bg-accent/5 border-accent/20"
                      : "bg-warning/5 border-warning/20"
                  }`}
                >
                  <div className={`p-2 rounded-lg ${
                    alert.severity === "Critical" ? "bg-accent/20 text-accent" : "bg-warning/20 text-warning"
                  }`}>
                    {getAlertIcon(alert.type)}
                  </div>
                  <div className="flex-1 text-right">
                    <p className="text-sm font-medium text-text-primary">{alert.message}</p>
                    <p className="text-xs text-text-muted mt-1">
                      {new Date(alert.createdAt).toLocaleString('ar-EG')}
                    </p>
                  </div>
                  <span className={`px-2 py-0.5 rounded-full text-[10px] font-bold ${
                    alert.severity === "Critical" ? "bg-accent/20 text-accent" : "bg-warning/20 text-warning"
                  }`}>
                    {alert.severity === "Critical" ? "حرج" : "تحذير"}
                  </span>
                </div>
              ))}
            </div>
          )}
        </GlassCard>
      )}

      {/* Search and Filters */}
      <GlassCard className="flex flex-col sm:flex-row gap-4">
        <div className="flex-1 relative group">
          <input
            type="text"
            placeholder="بحث عن مشرف..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="glass-input w-full h-12 pr-11 text-right focus:bg-primary/5 text-text-primary"
          />
          <Search className="absolute right-4 top-1/2 -translate-y-1/2 text-text-muted group-focus-within:text-primary transition-colors" size={20} />
        </div>

        {/* Status Filter */}
        <div className="relative">
          <select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value as StatusFilter)}
            className="glass-input h-12 pl-10 pr-4 text-right appearance-none cursor-pointer min-w-[140px] text-text-primary focus:bg-primary/5 [&>option]:text-black"
          >
            <option value="all">جميع الحالات</option>
            <option value="Active">نشط</option>
            <option value="Inactive">غير نشط</option>
          </select>
          <Filter className="absolute left-3 top-1/2 -translate-y-1/2 text-text-muted pointer-events-none" size={18} />
        </div>

        {/* Performance Filter */}
        <div className="relative">
          <select
            value={performanceFilter}
            onChange={(e) => setPerformanceFilter(e.target.value as PerformanceFilter)}
            className="glass-input h-12 pl-10 pr-4 text-right appearance-none cursor-pointer min-w-[140px] text-text-primary focus:bg-primary/5 [&>option]:text-black"
          >
            <option value="all">جميع الأداء</option>
            <option value="Good">جيد</option>
            <option value="Warning">تحذير</option>
            <option value="Critical">حرج</option>
          </select>
          <Activity className="absolute left-3 top-1/2 -translate-y-1/2 text-text-muted pointer-events-none" size={18} />
        </div>
      </GlassCard>

      {/* Supervisors Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {filteredSupervisors.map((supervisor) => (
          <GlassCard
            key={supervisor.userId}
            variant="hover"
            className="flex flex-col gap-4 relative overflow-hidden group cursor-pointer"
            onClick={() => openSupervisorDetail(supervisor)}
          >
            {/* Header */}
            <div className="flex items-start gap-4">
              <div className={`p-3 rounded-xl shrink-0 ${supervisor.status === 'Active' ? 'bg-secondary/10 text-secondary' : 'bg-accent/10 text-accent'}`}>
                {supervisor.status === 'Active' ? <UserCheck size={24} /> : <AlertCircle size={24} />}
              </div>
              
              <div className="flex-1 min-w-0">
                <div className="flex items-start justify-between gap-2">
                    <h3 className="text-lg font-bold text-text-primary truncate text-right">{supervisor.fullName}</h3>
                    {/* Performance Badge - In flow to prevent overlap */}
                     <div className={`shrink-0 px-2 py-1 rounded-full text-[10px] font-bold flex items-center gap-1 border ${getPerformanceColor(supervisor.performanceStatus)}`}>
                        {getPerformanceIcon(supervisor.performanceStatus)}
                        {supervisor.performanceStatus === "Good" ? "جيد" : supervisor.performanceStatus === "Warning" ? "تحذير" : "حرج"}
                    </div>
                </div>
                
                <div className="flex items-center gap-2 text-text-secondary text-sm mt-1">
                  <span className="dir-ltr text-xs font-mono opacity-70">@{supervisor.username}</span>
                  <span className="w-1 h-1 rounded-full bg-primary/20"></span>
                  <span>{supervisor.phoneNumber}</span>
                </div>
              </div>
            </div>

            {/* Worker Count Badge */}
            <div className="flex justify-between items-center">
              <div className="text-xs text-text-muted">
                {supervisor.activeWorkersToday} نشط اليوم
              </div>
              <div className="inline-flex items-center gap-2 px-3 py-1.5 rounded-full bg-primary/10 border border-primary/20">
                <span className="text-sm font-bold text-primary">{supervisor.workersCount} عامل</span>
                <Users size={16} className="text-primary" />
              </div>
            </div>

            {/* Stats Grid */}
            <div className="grid grid-cols-3 gap-2">
              <div className="p-2 rounded-xl bg-primary/5 border border-primary/5 text-center">
                <span className="text-lg font-bold text-text-primary">{supervisor.tasksAssignedThisMonth}</span>
                <p className="text-[10px] text-text-muted">مهام الشهر</p>
              </div>
              <div className="p-2 rounded-xl bg-secondary/5 border border-secondary/5 text-center">
                <span className="text-lg font-bold text-secondary">{supervisor.tasksCompletedThisMonth}</span>
                <p className="text-[10px] text-text-muted">مكتملة</p>
              </div>
              <div className="p-2 rounded-xl bg-warning/5 border border-warning/5 text-center">
                <span className="text-lg font-bold text-warning">{supervisor.tasksDelayed}</span>
                <p className="text-[10px] text-text-muted">متأخرة</p>
              </div>
            </div>

            {/* Task Completion Progress */}
            <div className="space-y-2">
              <div className="flex justify-between text-xs font-bold">
                <span className="text-text-muted">{supervisor.completionRate}%</span>
                <span className="text-text-primary">معدل الإنجاز</span>
              </div>
              <div className="h-2 w-full bg-primary/10 rounded-full overflow-hidden">
                <div
                  className={`h-full rounded-full transition-all duration-1000 ${
                    supervisor.completionRate >= 80 ? "bg-secondary" :
                    supervisor.completionRate >= 50 ? "bg-warning" : "bg-accent"
                  }`}
                  style={{ width: `${supervisor.completionRate}%` }}
                />
              </div>
            </div>

            {/* Issues & Response Time */}
            <div className="flex justify-between items-center pt-2 border-t border-primary/5">
              <div className="flex items-center gap-1 text-xs text-text-muted">
                <Clock size={12} />
                <span>{supervisor.avgResponseTimeHours}س متوسط الإنجاز</span>
              </div>
              <div className="flex items-center gap-1 text-xs">
                <span className={supervisor.issuesPending > 0 ? "text-warning font-bold" : "text-text-muted"}>
                  {supervisor.issuesPending} بلاغ معلق
                </span>
                <AlertCircle size={12} className={supervisor.issuesPending > 0 ? "text-warning" : "text-text-muted"} />
              </div>
            </div>

            <div className="pt-2 flex justify-end">
              <button className="text-sm font-bold text-primary hover:text-primary-dark transition-colors flex items-center gap-1 group-hover:translate-x-[-4px] transition-transform">
                عرض التفاصيل والعمال
                <ChevronLeft size={16} />
              </button>
            </div>
          </GlassCard>
        ))}
      </div>

      {filteredSupervisors.length === 0 && (
        <GlassCard className="py-20 text-center flex flex-col items-center">
          <Users size={64} className="text-white/10 mb-4" />
          <p className="text-text-muted text-lg font-medium">لا يوجد مشرفين لعرضهم</p>
        </GlassCard>
      )}

      {/* Detail Slide Panel */}
      {selectedSupervisor && (
        <div className="fixed inset-0 z-50 flex">
          <div
            className="absolute inset-0 bg-black/40 backdrop-blur-sm"
            onClick={closeSupervisorDetail}
          />

          <div className="absolute left-0 top-0 h-full w-full max-w-xl bg-background-paper border-r border-primary/10 shadow-2xl overflow-y-auto animate-slide-in-left">
            <div className="sticky top-0 bg-background-paper/95 backdrop-blur-sm border-b border-primary/5 p-6 flex items-center justify-between z-10">
              <button
                onClick={closeSupervisorDetail}
                className="p-2 rounded-lg hover:bg-primary/5 transition-colors"
              >
                <X size={20} className="text-text-primary" />
              </button>
              <h2 className="text-xl font-bold text-text-primary">تفاصيل المشرف</h2>
            </div>

            <div className="p-6 space-y-6">
              {/* Supervisor Info */}
              <div className="flex items-start gap-4 flex-row-reverse">
                <div className="w-16 h-16 rounded-full bg-primary flex items-center justify-center text-2xl font-bold text-white shadow-lg shadow-primary/20">
                  {selectedSupervisor.fullName.charAt(0)}
                </div>
                <div className="flex-1 text-right">
                  <h3 className="text-2xl font-bold text-text-primary">{selectedSupervisor.fullName}</h3>
                  <p className="text-text-secondary text-sm">@{selectedSupervisor.username}</p>
                  <div className="flex items-center gap-2 mt-2 justify-end">
                    <span className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium ${
                      selectedSupervisor.status === 'Active'
                        ? 'bg-secondary/20 text-secondary'
                        : 'bg-accent/20 text-accent'
                    }`}>
                      {selectedSupervisor.status === 'Active' ? <CheckCircle2 size={12} /> : <XCircle size={12} />}
                      {selectedSupervisor.status === 'Active' ? 'نشط' : 'غير نشط'}
                    </span>
                    <span className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium border ${getPerformanceColor(selectedSupervisor.performanceStatus)}`}>
                      {getPerformanceIcon(selectedSupervisor.performanceStatus)}
                      {selectedSupervisor.performanceStatus === "Good" ? "أداء جيد" : selectedSupervisor.performanceStatus === "Warning" ? "يحتاج متابعة" : "أداء ضعيف"}
                    </span>
                  </div>
                </div>
              </div>

              {/* Contact Info */}
              <div className="grid grid-cols-1 gap-3">
                <div className="flex items-center gap-3 flex-row-reverse p-3 rounded-lg bg-primary/5">
                  <Phone size={18} className="text-primary" />
                  <span className="text-text-primary">{selectedSupervisor.phoneNumber}</span>
                </div>
                {selectedSupervisor.lastLoginAt && (
                  <div className="flex items-center gap-3 flex-row-reverse p-3 rounded-lg bg-primary/5">
                    <Clock size={18} className="text-primary" />
                    <span className="text-text-primary">آخر دخول: {new Date(selectedSupervisor.lastLoginAt).toLocaleString('ar-EG')}</span>
                  </div>
                )}
              </div>

              {/* Performance Stats */}
              <div className="grid grid-cols-2 gap-4">
                <div className="p-4 rounded-xl bg-secondary/10 border border-secondary/20 text-center">
                  <div className="text-3xl font-bold text-secondary">{selectedSupervisor.completionRate}%</div>
                  <div className="text-xs text-secondary/70 mt-1">معدل الإنجاز</div>
                </div>
                <div className="p-4 rounded-xl bg-primary/10 border border-primary/20 text-center">
                  <div className="text-3xl font-bold text-primary">{selectedSupervisor.avgResponseTimeHours}س</div>
                  <div className="text-xs text-primary/70 mt-1">متوسط وقت الإنجاز</div>
                </div>
                <div className="p-4 rounded-xl bg-warning/10 border border-warning/20 text-center">
                  <div className="text-3xl font-bold text-warning">{selectedSupervisor.tasksDelayed}</div>
                  <div className="text-xs text-warning/70 mt-1">مهام متأخرة</div>
                </div>
                <div className="p-4 rounded-xl bg-primary/5 border border-primary/10 text-center">
                  <div className="text-3xl font-bold text-text-primary">{selectedSupervisor.issuesPending}</div>
                  <div className="text-xs text-text-muted mt-1">بلاغات معلقة</div>
                </div>
              </div>

              {/* Monthly Stats */}
              <div className="p-4 rounded-xl bg-primary/5 border border-primary/10">
                <h4 className="text-sm font-bold text-text-primary mb-3 text-right">إحصائيات الشهر</h4>
                <div className="grid grid-cols-3 gap-3 text-center">
                  <div>
                    <div className="text-xl font-bold text-text-primary">{selectedSupervisor.tasksAssignedThisMonth}</div>
                    <div className="text-[10px] text-text-muted">مهام موكلة</div>
                  </div>
                  <div>
                    <div className="text-xl font-bold text-secondary">{selectedSupervisor.tasksCompletedThisMonth}</div>
                    <div className="text-[10px] text-text-muted">مكتملة</div>
                  </div>
                  <div>
                    <div className="text-xl font-bold text-warning">{selectedSupervisor.tasksPendingReview}</div>
                    <div className="text-[10px] text-text-muted">بانتظار المراجعة</div>
                  </div>
                </div>
              </div>

              {/* Workers Section */}
              <div className="border-t border-primary/10 pt-6">
                <div className="flex items-center justify-between mb-4 flex-row-reverse">
                  <h4 className="text-lg font-bold text-text-primary flex items-center gap-2">
                    <Users size={20} className="text-primary" />
                    العمال التابعين ({supervisorWorkers.length})
                  </h4>
                  {selectedWorkerIds.length > 0 && (
                    <button
                      onClick={() => setShowTransferModal(true)}
                      className="flex items-center gap-2 px-3 py-1.5 rounded-lg bg-primary/20 text-primary hover:bg-primary/30 transition-colors text-sm font-medium"
                    >
                      <ArrowLeftRight size={16} />
                      نقل المحددين ({selectedWorkerIds.length})
                    </button>
                  )}
                </div>

                {loadingWorkers ? (
                  <div className="flex justify-center py-8">
                    <Loader2 className="w-8 h-8 text-primary animate-spin" />
                  </div>
                ) : supervisorWorkers.length === 0 ? (
                  <div className="text-center py-8 text-text-muted">
                    <UserMinus size={40} className="mx-auto mb-2 opacity-50" />
                    <p>لا يوجد عمال تابعين لهذا المشرف</p>
                  </div>
                ) : (
                  <div className="space-y-2">
                    {supervisorWorkers.map(worker => (
                      <div
                        key={worker.userId}
                        className={`flex items-center gap-3 flex-row-reverse p-3 rounded-lg border transition-all cursor-pointer ${
                          selectedWorkerIds.includes(worker.userId)
                            ? 'bg-primary/20 border-primary/40'
                            : 'bg-background-paper border-primary/5 hover:bg-primary/5'
                        }`}
                        onClick={() => toggleWorkerSelection(worker.userId)}
                      >
                        <div className={`w-5 h-5 rounded border-2 flex items-center justify-center transition-colors ${
                          selectedWorkerIds.includes(worker.userId)
                            ? 'bg-primary border-primary'
                            : 'border-primary/20'
                        }`}>
                          {selectedWorkerIds.includes(worker.userId) && (
                            <CheckCircle2 size={14} className="text-white" />
                          )}
                        </div>

                        <div className="flex-1 text-right">
                          <div className="font-medium text-text-primary">{worker.fullName}</div>
                          <div className="text-xs text-text-muted flex items-center gap-2 justify-end">
                            <span>{worker.phoneNumber}</span>
                            <span className="w-1 h-1 rounded-full bg-primary/20"></span>
                            <span>{worker.workerType || 'عامل'}</span>
                          </div>
                        </div>

                        <div className={`px-2 py-0.5 rounded-full text-xs font-medium ${
                          worker.status === 'Active'
                            ? 'bg-secondary/20 text-secondary'
                            : 'bg-accent/20 text-accent'
                        }`}>
                          {worker.status === 'Active' ? 'نشط' : 'غير نشط'}
                        </div>
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
            className="absolute inset-0 bg-black/40 backdrop-blur-sm"
            onClick={() => setShowTransferModal(false)}
          />

          <div className="relative bg-background-paper border border-primary/10 rounded-2xl p-6 w-full max-w-md shadow-2xl">
            <h3 className="text-xl font-bold text-text-primary text-right mb-4">نقل العمال</h3>

            <p className="text-text-secondary text-right mb-4">
              سيتم نقل {selectedWorkerIds.length} عامل إلى المشرف المحدد
            </p>

            <div className="mb-6">
              <label className="block text-sm font-medium text-text-primary text-right mb-2">
                المشرف الجديد
              </label>
              <select
                value={targetSupervisorId}
                onChange={(e) => setTargetSupervisorId(e.target.value ? Number(e.target.value) : "")}
                className="glass-input w-full h-12 text-right text-text-primary focus:bg-primary/5 [&>option]:text-black"
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
                className="flex-1 py-3 rounded-xl bg-primary text-white font-bold hover:bg-primary-dark transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2"
              >
                {transferring ? (
                  <>
                    <Loader2 size={18} className="animate-spin" />
                    جاري النقل...
                  </>
                ) : (
                  <>
                    <ArrowLeftRight size={18} />
                    نقل العمال
                  </>
                )}
              </button>
              <button
                onClick={() => setShowTransferModal(false)}
                className="flex-1 py-3 rounded-xl border border-primary/10 text-text-primary font-bold hover:bg-primary/5 transition-colors"
              >
                إلغاء
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Animation styles */}
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
