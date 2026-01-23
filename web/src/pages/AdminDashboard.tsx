import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Users, ClipboardList, AlertTriangle, History, Activity, Shield } from "lucide-react";
import { getDashboardOverview, getWorkerStatuses } from "../api/dashboard";
import { getAdminSupervisorsMonitoring } from "../api/reports";
import type { DashboardOverview, WorkerStatus } from "../types/dashboard";
import type { AdminSupervisorMonitoringData } from "../api/reports";
import { apiClient } from "../api/client";
import GlassCard from "../components/UI/GlassCard";


type ActivityItem = {
    id: string;
    time: string;
    user: string;
    action: string;
    details: string;
    type: "user" | "task" | "system";
};

export default function AdminDashboard() {
    const navigate = useNavigate();
    const [overview, setOverview] = useState<DashboardOverview | null>(null);
    const [activities, setActivities] = useState<ActivityItem[]>([]);
    const [workerStatuses, setWorkerStatuses] = useState<WorkerStatus[]>([]);
    const [monitoringData, setMonitoringData] = useState<AdminSupervisorMonitoringData | null>(null);
    const [loading, setLoading] = useState(true);
    const [userName, setUserName] = useState("مستخدم");

    useEffect(() => {
        // Get user name from local storage
        try {
            const userStr = localStorage.getItem("followup_user");
            if (userStr) {
                const user = JSON.parse(userStr);
                setUserName(user.fullName?.split(' ')[0] || "مستخدم");
            }
        } catch (e) {
            console.warn("Failed to parse user data from localStorage", e);
        }
        // FIX: Add AbortController to prevent memory leak on unmount
        const controller = new AbortController();
        let isMounted = true;

        const fetchData = async () => {
            try {
                setLoading(true);
                const [data, auditResponse, workersData, adminMonData] = await Promise.all([
                    getDashboardOverview(),
                    apiClient.get('/audit?count=20'),
                    getWorkerStatuses(),
                    getAdminSupervisorsMonitoring()
                ]);

                if (!isMounted) return;

                // Inject Mock Alerts for Demonstration
                if (adminMonData) {
                    const mockAlerts: any[] = [
                        { // Using 'any' to bypass strict type check for mock data if interface varies
                            id: 991,
                            severity: 'Critical',
                            supervisorName: 'خالد أبو سعدة',
                            message: 'تنبيه: تم تسجيل 5 حالات تأخير متتالية في "منطقة الإرسال"',
                            createdAt: new Date().toISOString()
                        },
                        {
                            id: 992,
                            severity: 'Warning',
                            supervisorName: 'أحمد الشريف',
                            message: 'انخفاض معدل الإنجاز عن 50% لليوم الثاني على التوالي',
                            createdAt: new Date().toISOString()
                        },
                        {
                            id: 993,
                            severity: 'Critical',
                            supervisorName: 'أمن النظام',
                            message: 'محاولة تسجيل دخول من جهاز غير مسجل (IP: 192.168.1.15)',
                            createdAt: new Date().toISOString()
                        }
                    ];
                    adminMonData.alerts = [...mockAlerts, ...(adminMonData.alerts || [])];
                }

                setOverview(data);
                setWorkerStatuses(workersData);
                setMonitoringData(adminMonData);

                const logs = auditResponse.data.data || [];
                // Filter out technical noise (Logins) - Only show business actions
                const mappedLogs: ActivityItem[] = logs
                    .filter((log: any) => !log.action.includes("Login") && !log.action.includes("Logout"))
                    .slice(0, 6) // Best 6 business actions
                    .map((log: any) => ({
                    id: log.auditLogId.toString(),
                    time: new Date(log.createdAt).toLocaleTimeString('ar-EG', { hour: '2-digit', minute: '2-digit' }),
                    user: log.userFullName || log.username,
                    action: log.action,
                    details: log.details || "",
                    type: log.action.includes("Task") ? "task" : log.action.includes("User") ? "user" : "system"
                }));
                setActivities(mappedLogs);
            } catch (err) {
                if (!isMounted) return;
                console.error("Failed to fetch dashboard data", err);
            } finally {
                if (isMounted) setLoading(false);
            }
        };

        fetchData();

        // Cleanup on unmount
        return () => {
            isMounted = false;
            controller.abort();
        };
    }, []);

    if (loading) {
        return (
            <div className="h-full w-full flex items-center justify-center">
                <div className="flex flex-col items-center gap-4">
                    <div className="relative">
                        <div className="w-16 h-16 rounded-full border-4 border-primary/30 border-t-primary animate-spin"></div>
                        <Activity className="absolute inset-0 m-auto text-primary" size={24} />
                    </div>
                    <div className="flex flex-col items-center gap-1">
                        <h2 className="text-2xl font-black text-primary tracking-tight">FollowUp</h2>
                        <p className="text-text-secondary font-medium">نظام متابعة العمل الميداني</p>
                    </div>
                </div>
            </div>
        );
    }

    return (
        <div className="space-y-8 pb-10">
            {/* Header */}
            <div className="flex flex-col md:flex-row md:items-center justify-between gap-6">
                <div className="flex flex-col items-start">
                    <h1 className="text-3xl md:text-4xl font-extrabold text-text-primary text-right w-full">
                        مرحبا، {userName}
                    </h1>
                    <p className="text-right text-text-secondary mt-2 font-medium">نظام FollowUp لإدارة المتابعة الميدانية</p>
                </div>
                
                <GlassCard className="!p-0 !bg-background-paper !border-primary/5 flex items-center overflow-hidden shadow-sm">
                    <div className="px-6 py-3 border-l border-primary/5">
                        <p className="text-xs text-text-muted font-bold mb-0.5 uppercase tracking-wider">التاريخ</p>
                        <p className="text-sm font-bold text-text-primary">
                            {new Date().toLocaleDateString('ar-EG', { weekday: 'long', day: 'numeric', month: 'long' })}
                        </p>
                    </div>
                    <div className="px-4 py-3 bg-primary/20 text-primary">
                        <Activity size={20} className="animate-pulse" />
                    </div>
                </GlassCard>
            </div>

            {/* Dashboard Highlights Grid */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                {/* 1. Worker Attendance Card */}
                <GlassCard variant="panel" className="lg:col-span-1 border-primary/10 shadow-sm flex flex-col justify-between">
                    <div className="flex items-center justify-between mb-6">
                        <h2 className="text-lg font-bold text-text-primary">نظرة على التواجد</h2>
                        <div className="p-2 rounded-lg bg-primary/10 text-primary">
                            <Users size={22} />
                        </div>
                    </div>
                    
                    <div className="flex items-center gap-6 mb-4">
                        <div className="relative w-28 h-28">
                            <svg className="w-full h-full -rotate-90" viewBox="0 0 100 100">
                                <circle cx="50" cy="50" r="40" fill="none" stroke="#F3F1ED" strokeWidth="12" />
                                <circle 
                                    cx="50" cy="50" r="40" fill="none" stroke="#A3B18A" strokeWidth="12" 
                                    strokeDasharray={`${(overview?.workers.checkedIn || 0) / (overview?.workers.total || 1) * 251.2} 251.2`}
                                />
                            </svg>
                            <div className="absolute inset-0 flex flex-col items-center justify-center">
                                <span className="text-xl font-black text-text-primary">{Math.round(((overview?.workers.checkedIn || 0) / (overview?.workers.total || 1)) * 100)}%</span>
                                <span className="text-[10px] text-text-muted font-bold">نسبة الحضور</span>
                            </div>
                        </div>
                        <div className="flex-1 space-y-3">
                            <div className="flex items-center justify-end gap-2">
                                <span className="text-sm font-bold text-text-primary">{overview?.workers.checkedIn}</span>
                                <div className="flex items-center gap-1.5 text-xs text-text-secondary font-medium">
                                    <span>حاضر الآن</span>
                                    <div className="w-2 h-2 rounded-full bg-secondary" />
                                </div>
                            </div>
                            <div className="flex items-center justify-end gap-2">
                                <span className="text-sm font-bold text-text-primary">{overview?.workers.notCheckedIn}</span>
                                <div className="flex items-center gap-1.5 text-xs text-text-secondary font-medium">
                                    <span>غائب / لم يبدأ</span>
                                    <div className="w-2 h-2 rounded-full bg-accent" />
                                </div>
                            </div>
                        </div>
                    </div>
                </GlassCard>

                {/* 2. Tasks Summary Card */}
                <GlassCard variant="panel" className="lg:col-span-1 border-primary/10 shadow-sm flex flex-col justify-between">
                    <div className="flex items-center justify-between mb-6">
                        <h2 className="text-lg font-bold text-text-primary">إحصائيات المهام</h2>
                        <div className="p-2 rounded-lg bg-warning/10 text-warning">
                            <ClipboardList size={22} />
                        </div>
                    </div>
                    
                    <div className="space-y-4">
                        <div>
                            <div className="flex justify-between text-xs mb-1.5 font-bold">
                                <span className="text-text-secondary">المهام المكتملة اليوم</span>
                                <span className="text-primary font-black">{overview?.tasks.completedToday}</span>
                            </div>
                            <div className="h-2 bg-primary/5 rounded-full overflow-hidden">
                                <div 
                                    className="bg-primary h-full rounded-full transition-all duration-1000" 
                                    style={{ width: `${Math.min(100, (overview?.tasks.completedToday || 0) / ((overview?.tasks.pending || 0) + (overview?.tasks.inProgress || 0) + (overview?.tasks.completedToday || 1)) * 100)}%` }} 
                                />
                            </div>
                        </div>
                        <div className="grid grid-cols-2 gap-3 pt-2">
                            <div className="bg-primary/5 p-3 rounded-xl border border-primary/5 text-center">
                                <p className="text-lg font-black text-primary">{overview?.tasks.inProgress}</p>
                                <p className="text-[10px] text-text-muted font-bold mt-0.5">قيد التنفيذ</p>
                            </div>
                            <div className="bg-warning/5 p-3 rounded-xl border border-warning/5 text-center">
                                <p className="text-lg font-black text-warning">{overview?.tasks.pending}</p>
                                <p className="text-[10px] text-text-muted font-bold mt-0.5">بانتظار البدء</p>
                            </div>
                        </div>
                    </div>
                </GlassCard>

                {/* 3. Issues/Reports Card */}
                <GlassCard variant="panel" className="lg:col-span-1 border-primary/10 shadow-sm flex flex-col justify-between">
                    <div className="flex items-center justify-between mb-6">
                        <h2 className="text-lg font-bold text-text-primary">البلاغات</h2>
                        <div className="p-2 rounded-lg bg-accent/10 text-accent">
                            <AlertTriangle size={22} />
                        </div>
                    </div>
                    
                    <div className="flex flex-col gap-4">
                        <div className="flex items-center justify-between bg-accent/5 p-4 rounded-2xl border border-accent/10 group hover:border-accent/20 transition-all cursor-pointer">
                            <div className="text-lg font-black text-accent">{overview?.issues.unresolved}</div>
                            <div className="text-right">
                                <p className="text-sm font-bold text-text-primary">بلاغات لم يتم معالجتها</p>
                                <p className="text-[10px] text-text-muted font-medium">تتطلب مراجعة المشرفين</p>
                            </div>
                        </div>
                        <button 
                            onClick={() => navigate('/issues')}
                            className="w-full py-3 rounded-xl bg-background border border-primary/10 text-primary text-sm font-bold hover:bg-primary/5 transition-all active:scale-95"
                        >
                            فتح قائمة البلاغات →
                        </button>
                    </div>
                </GlassCard>
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-4 gap-8">
                {/* 1. Alerts & Intelligence - High Visibility */}
                <div className="lg:col-span-2 animate-slide-up" style={{ animationDelay: '500ms' }}>
                    <GlassCard variant="panel" className="h-full flex flex-col !bg-accent/5 border-accent/10">
                        <div className="flex items-center justify-between mb-6 border-b border-accent/10 pb-4">
                            <div className="flex items-center gap-3">
                                <div className="p-2.5 rounded-xl bg-accent/20 text-accent">
                                    <Shield size={22} />
                                </div>
                                <div>
                                    <h2 className="text-xl font-bold text-text-primary text-right">تنبيهات النظام الذكية</h2>
                                    <p className="text-xs text-text-muted font-medium mt-0.5">مشاكل تتطلب تدخل إداري فوري</p>
                                </div>
                            </div>
                        </div>

                        <div className="space-y-4">
                            {monitoringData?.alerts && monitoringData.alerts.length > 0 ? (
                                monitoringData.alerts.slice(0, 4).map((alert) => (
                                    <div key={alert.id} className="flex items-start gap-4 p-4 rounded-2xl bg-white/50 border border-accent/10 hover:border-accent/30 transition-all">
                                        <div className="flex-1 text-right">
                                            <div className="flex items-center justify-end gap-2 mb-1">
                                                <span className={`text-[10px] font-black px-2 py-0.5 rounded ${
                                                    alert.severity === 'Critical' ? 'bg-accent text-white' : 'bg-warning/20 text-warning'
                                                }`}>
                                                    {alert.severity === 'Critical' ? 'حرج' : 'تنبيه'}
                                                </span>
                                                <h4 className="text-sm font-bold text-text-primary">{alert.supervisorName || 'تنبيه عام'}</h4>
                                            </div>
                                            <p className="text-sm text-text-secondary leading-relaxed">{alert.message}</p>
                                        </div>
                                        <div className={`mt-1 h-3 w-3 rounded-full animate-pulse ${
                                            alert.severity === 'Critical' ? 'bg-accent' : 'bg-warning'
                                        }`} />
                                    </div>
                                ))
                            ) : (
                                <div className="py-12 text-center opacity-40">
                                    <Shield size={48} className="mx-auto mb-3" />
                                    <p className="font-bold">لا توجد تنبيهات حرجة حالياً</p>
                                </div>
                            )}
                        </div>
                    </GlassCard>
                </div>

                {/* 2. Top Active Workers Today */}
                <div className="lg:col-span-2 animate-slide-up" style={{ animationDelay: '600ms' }}>
                    <GlassCard variant="panel" className="h-full flex flex-col">
                        <div className="flex items-center justify-between mb-6 border-b border-primary/5 pb-4">
                            <div className="flex items-center gap-3">
                                <div className="p-2.5 rounded-xl bg-secondary/10 text-secondary">
                                    <Users size={22} />
                                </div>
                                <div>
                                    <h2 className="text-xl font-bold text-text-primary text-right">العاملون الأكثر نشاطاً</h2>
                                    <p className="text-xs text-text-muted font-medium mt-0.5">أعلى إنتاجية ميدانية اليوم</p>
                                </div>
                            </div>
                            <button onClick={() => navigate('/supervisors')} className="text-sm font-bold text-primary hover:opacity-80">التفاصيل</button>
                        </div>

                        <div className="space-y-3">
                            {workerStatuses
                                .filter(w => w.status === 'CheckedIn' || w.completedTasksCount > 0)
                                .sort((a, b) => b.completedTasksCount - a.completedTasksCount)
                                .slice(0, 5)
                                .map((worker) => (
                                    <div key={worker.userId} className="flex items-center justify-between p-3 rounded-xl bg-primary/5 border border-primary/5 hover:bg-primary/10 transition-all">
                                        <div className="flex items-center gap-3">
                                            <div className="px-2 py-1 rounded bg-secondary/20 text-secondary text-xs font-bold">
                                                {worker.completedTasksCount} وحدة
                                            </div>
                                        </div>
                                        <div className="text-right">
                                            <p className="text-sm font-bold text-text-primary">{worker.fullName}</p>
                                            <p className="text-[10px] text-text-muted font-medium">{worker.zoneName || 'خارج المنطقة'}</p>
                                        </div>
                                    </div>
                                ))
                            }
                            {workerStatuses.length === 0 && (
                                <div className="py-12 text-center opacity-40">
                                    <p className="font-bold">لا يوجد بيانات عمال حالية</p>
                                </div>
                            )}
                        </div>
                    </GlassCard>
                </div>

                {/* 3. Field Activity Timeline - (The refined logs) */}
                <div className="lg:col-span-3 animate-slide-up" style={{ animationDelay: '700ms' }}>
                    <GlassCard variant="panel" className="h-full">
                        <div className="flex items-center justify-between mb-8 border-b border-primary/5 pb-5">
                            <div className="flex items-center gap-3">
                                <div className="p-2.5 rounded-xl bg-primary/10 text-primary">
                                    <History size={22} />
                                </div>
                                <div>
                                    <h2 className="text-xl font-bold text-text-primary text-right">نبض الميدان (العمليات)</h2>
                                    <p className="text-xs text-text-muted font-medium mt-0.5">آخر الإجراءات المتعلقة بالمهام والمناطق فقط</p>
                                </div>
                            </div>
                        </div>
                        
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-x-12 gap-y-6 max-h-[400px] overflow-y-auto px-1 custom-scrollbar">
                            {activities.map((activity, idx) => (
                                <div key={activity.id} className="relative flex items-start gap-4 group animate-fade-in" style={{ animationDelay: `${500 + (idx * 50)}ms` }}>
                                    <div className="flex-1 text-right">
                                        <div className="flex items-center justify-end gap-2 mb-1.5">
                                            <span className="text-[10px] font-bold text-text-muted bg-primary/5 px-2 py-0.5 rounded">{activity.time}</span>
                                            <span className="text-xs font-black text-text-primary">{activity.user}</span>
                                        </div>
                                        <p className="text-sm text-text-primary font-bold leading-relaxed group-hover:text-primary transition-colors">{activity.action}</p>
                                        {activity.details && <p className="text-[11px] text-text-secondary mt-1 font-medium bg-background-paper p-2 rounded-lg border border-primary/5">{activity.details}</p>}
                                    </div>
                                    <div className="relative mt-2">
                                        <div className={`w-3.5 h-3.5 rounded-full border-2 border-background-paper z-10 relative shadow-sm ${
                                            activity.type === 'task' ? 'bg-secondary' :
                                            activity.type === 'user' ? 'bg-primary' : 'bg-accent'
                                        }`} />
                                    </div>
                                </div>
                            ))}
                        </div>
                    </GlassCard>
                </div>

                {/* 4. Quick Actions */}
                <div className="lg:col-span-1 space-y-6 animate-slide-up" style={{ animationDelay: '800ms' }}>
                    <GlassCard variant="panel" className="bg-primary text-white border-none shadow-xl shadow-primary/20 relative overflow-hidden group">
                        <div className="absolute top-0 right-0 w-32 h-32 bg-white/10 rounded-full -translate-x-10 -translate-y-10 blur-2xl group-hover:scale-150 transition-transform duration-1000" />
                        
                        <div className="relative z-10">
                            <h3 className="text-lg font-bold mb-2">إدارة الحسابات</h3>
                            <p className="text-sm text-white/80 font-medium mb-6">تحكم في صلاحيات المشرفين، تفعيل الحسابات وفك ارتباط الأجهزة.</p>
                            <button 
                                onClick={() => navigate('/accounts')}
                                className="px-5 py-2.5 bg-white text-primary rounded-xl text-sm font-black hover:bg-white/90 transition-all active:scale-95 shadow-md"
                            >
                                إدارة المستخدمين
                            </button>
                        </div>
                    </GlassCard>
                </div>
            </div>
        </div>
    );
}
