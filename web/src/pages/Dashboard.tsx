import React, { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Users, ClipboardList, MapPin, Activity, PlusCircle, ArrowRight } from "lucide-react";
import { getDashboardOverview, getWorkerStatuses } from "../api/dashboard";
import type { DashboardOverview, WorkerStatus } from "../types/dashboard";
import { apiClient } from "../api/client";
import GlassCard from "../components/UI/GlassCard";


type ActivityItem = {
  id: string;
  time: string;
  text: string;
  icon: "pin" | "user" | "map";
};

const Dashboard: React.FC = () => {
    const navigate = useNavigate();
    const [overview, setOverview] = useState<DashboardOverview | null>(null);
    const [activities, setActivities] = useState<ActivityItem[]>([]);
    const [workerStatuses, setWorkerStatuses] = useState<WorkerStatus[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");
    const [userName, setUserName] = useState("مشرف");

    useEffect(() => {
        try {
            const userStr = localStorage.getItem("followup_user");
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
                    apiClient.get('/audit?count=15')
                ]);

                setOverview(overviewData);
                setWorkerStatuses(workersData);

                // Map and clean up activities
                const logs = auditResponse.data.data || [];
                const mappedActivities: ActivityItem[] = logs
                    .filter((log: any) => !log.action.includes("Login") && !log.action.includes("Logout"))
                    .slice(0, 5)
                    .map((log: any) => {
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
    }, []);

  const tasksTotal = (overview?.tasks.pending ?? 0) + (overview?.tasks.inProgress ?? 0) + (overview?.tasks.completedToday ?? 0);

  // Activities now loaded from audit logs in useEffect above

  if (loading) {
    return (
      <div className="h-full w-full bg-background flex items-center justify-center">
        <div className="flex flex-col items-center gap-4">
            <div className="w-12 h-12 rounded-full border-4 border-primary/30 border-t-primary animate-spin"></div>
            <div className="flex flex-col items-center gap-1">
                <h2 className="text-xl font-black text-primary tracking-tight">FollowUp</h2>
                <p className="text-text-secondary text-sm font-medium">نظام متابعة العمل الميداني</p>
            </div>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="h-full w-full bg-background flex items-center justify-center">
        <div className="text-accent font-bold">{error}</div>
      </div>
    );
  }

    return (
        <div className="space-y-6 pb-10">
            {/* Simple Header */}
            <div className="text-right">
                <h1 className="text-2xl md:text-3xl font-bold text-text-primary">أهلاً بك، {userName}</h1>
                <p className="text-text-secondary mt-1">نظام متابعة العمل الميداني للبث الميداني</p>
            </div>

            {/* Basic Stats Grid */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
                <GlassCard className="flex items-center gap-4 bg-white/50 border border-primary/10">
                    <div className="p-3 rounded-full bg-primary/10 text-primary">
                        <Users size={24} />
                    </div>
                    <div className="text-right">
                        <p className="text-sm text-text-muted font-bold">إجمالي الفريق</p>
                        <h3 className="text-2xl font-black text-text-primary">{overview?.workers.total}</h3>
                    </div>
                </GlassCard>

                <GlassCard className="flex items-center gap-4 bg-white/50 border border-primary/10">
                    <div className="p-3 rounded-full bg-secondary/10 text-secondary">
                        <Activity size={24} />
                    </div>
                    <div className="text-right">
                        <p className="text-sm text-text-muted font-bold">المتواجدون الآن</p>
                        <h3 className="text-2xl font-black text-text-primary text-secondary">{overview?.workers.checkedIn}</h3>
                    </div>
                </GlassCard>

                <GlassCard className="flex items-center gap-4 bg-white/50 border border-primary/10">
                    <div className="p-3 rounded-full bg-accent/10 text-accent">
                        <ClipboardList size={24} />
                    </div>
                    <div className="text-right">
                        <p className="text-sm text-text-muted font-bold">مهام اليوم</p>
                        <h3 className="text-2xl font-black text-text-primary text-secondary">{tasksTotal}</h3>
                    </div>
                </GlassCard>

                <GlassCard className="flex items-center gap-4 bg-white/50 border border-primary/10">
                    <div className="p-3 rounded-full bg-secondary/10 text-secondary">
                        <ArrowRight size={24} className="-rotate-45" />
                    </div>
                    <div className="text-right">
                        <p className="text-sm text-text-muted font-bold">أنجزت اليوم</p>
                        <h3 className="text-2xl font-black text-text-primary text-secondary">{overview?.tasks.completedToday}</h3>
                    </div>
                </GlassCard>
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                {/* Team Status Table - Simpler */}
                {/* Team Status Table */}
                <GlassCard className="overflow-hidden min-h-[300px]">
                    <div className="flex items-center justify-between mb-4">
                        <h2 className="text-lg font-bold text-text-primary">حالة الفريق</h2>
                        {workerStatuses.length > 5 && (
                          <button onClick={() => navigate('/supervisors')} className="text-xs text-primary font-bold hover:underline">عرض الكل</button>
                        )}
                    </div>
                    {workerStatuses.length > 0 ? (
                        <div className="divide-y divide-primary/5">
                            {workerStatuses.slice(0, 8).map((worker) => (
                                <div key={worker.userId} className="py-3 flex items-center justify-between">
                                    <div className="flex items-center gap-2">
                                        <span className={`w-2 h-2 rounded-full ${worker.status === 'CheckedIn' ? 'bg-secondary' : 'bg-text-muted/30'}`} />
                                        <span className="text-xs font-bold text-text-muted">
                                            {worker.status === 'CheckedIn' ? 'نشط' : 'غائب'}
                                        </span>
                                    </div>
                                    <div className="text-right">
                                        <p className="font-bold text-sm text-text-primary">{worker.fullName}</p>
                                        <p className="text-[10px] text-text-muted">{worker.zoneName || 'لا توجد منطقة'}</p>
                                    </div>
                                </div>
                            ))}
                        </div>
                    ) : (
                        <div className="h-full flex flex-col items-center justify-center text-center opacity-50 pb-10">
                            <Users size={32} className="mb-2 text-text-muted" />
                            <p className="text-sm font-medium">لا يوجد عمال متاحين</p>
                        </div>
                    )}
                </GlassCard>

                {/* Quick Actions & Activity */}
                <div className="space-y-6">
                    <div className="grid grid-cols-2 gap-4">
                        <button 
                            onClick={() => navigate('/tasks/new')}
                            className="p-6 rounded-2xl bg-primary text-white text-right hover:shadow-lg transition-all active:scale-95 group"
                        >
                            <PlusCircle size={24} className="mb-4 opacity-70 group-hover:opacity-100" />
                            <h3 className="font-bold">إسناد مهمة</h3>
                            <p className="text-[10px] opacity-70">إضافة عمل جديد لعامل</p>
                        </button>
                        <button 
                            onClick={() => navigate('/zones')}
                            className="p-6 rounded-2xl bg-[#83c5be] text-white text-right hover:shadow-lg transition-all active:scale-95 group"
                        >
                            <MapPin size={24} className="mb-4 opacity-70 group-hover:opacity-100" />
                            <h3 className="font-bold">الخريطة</h3>
                            <p className="text-[10px] opacity-70">توزيع المهام مكانياً</p>
                        </button>
                    </div>

                    <GlassCard className="min-h-[200px]">
                        <h2 className="text-lg font-bold text-text-primary mb-4">آخر التحديثات</h2>
                        {activities.length > 0 ? (
                            <div className="space-y-4">
                                {activities.slice(0, 4).map((a) => (
                                    <div key={a.id} className="flex items-start gap-3 text-right">
                                        <div className="flex-1">
                                            <p className="text-sm text-text-primary font-medium">{a.text}</p>
                                            <p className="text-[10px] text-text-muted">{a.time}</p>
                                        </div>
                                        <div className="mt-1.5 w-1.5 h-1.5 rounded-full bg-primary/40" />
                                    </div>
                                ))}
                            </div>
                        ) : (
                            <div className="h-full flex flex-col items-center justify-center text-center opacity-50 pb-6">
                                <Activity size={24} className="mb-2 text-text-muted" />
                                <p className="text-sm font-medium">لا توجد تحديثات حديثة</p>
                            </div>
                        )}
                    </GlassCard>
                </div>
            </div>
        </div>
    );
};

export default Dashboard;
