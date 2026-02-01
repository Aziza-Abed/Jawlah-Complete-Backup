import { AlertCircle, Clock, Users, ShieldAlert } from "lucide-react";

interface AlertsWidgetProps {
  pendingTasks: number;
  absentWorkers: number;
  unresolvedIssues: number;
}

export default function AlertsWidget({ pendingTasks, absentWorkers, unresolvedIssues }: AlertsWidgetProps) {
  const alerts = [
    {
      id: "pending",
      title: `${pendingTasks} مهام بانتظار البدء`,
      icon: <Clock size={18} className="text-[#C86E5D]" />,
      active: pendingTasks > 0,
      color: "bg-[#F2D3CD]/30",
      borderColor: "border-[#C86E5D]/20",
    },
    {
      id: "absent",
      title: `${absentWorkers} عمال لم يسجلوا حضورهم`,
      icon: <Users size={18} className="text-[#F3C668]" />,
      active: absentWorkers > 0,
      color: "bg-[#F3E7C8]/40",
      borderColor: "border-[#F3C668]/20",
    },
    {
      id: "issues",
      title: `${unresolvedIssues} بلاغات تتطلب معالجة`,
      icon: <ShieldAlert size={18} className="text-[#C86E5D]" />,
      active: unresolvedIssues > 0,
      color: "bg-[#F2D3CD]/30",
      borderColor: "border-[#C86E5D]/20",
    },
  ];

  const activeAlerts = alerts.filter(a => a.active);

  return (
    <div className="bg-white rounded-[16px] p-6 shadow-[0_4px_20px_rgba(0,0,0,0.04)] h-full">
      <div className="flex items-center justify-between mb-6">
        <h2 className="text-[17px] font-bold text-[#2F2F2F] flex items-center gap-2">
          <AlertCircle size={20} className="text-[#C86E5D]" />
          تنبيهات اليوم
        </h2>
      </div>

      <div className="space-y-4">
        {activeAlerts.length > 0 ? (
          activeAlerts.map((alert) => (
            <div
              key={alert.id}
              className={`p-4 rounded-[12px] border ${alert.borderColor} ${alert.color} flex items-center gap-3 transition-all hover:scale-[1.01]`}
            >
              <div className="shrink-0">{alert.icon}</div>
              <div className="flex-1 text-right text-[14px] font-bold text-[#202020]">
                {alert.title}
              </div>
            </div>
          ))
        ) : (
          <div className="h-32 flex flex-col items-center justify-center text-[#6B7280] opacity-50">
            <ShieldAlert size={32} className="mb-2" />
            <p className="text-sm font-medium">لا توجد تنبيهات حالياً</p>
          </div>
        )}
      </div>
    </div>
  );
}
