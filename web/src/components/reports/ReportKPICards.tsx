import { CheckCircle, Users, Activity, AlertCircle } from "lucide-react";
import type { KPI } from "./types";

type ReportKPICardsProps = {
  kpis: KPI[];
};

export default function ReportKPICards({ kpis }: ReportKPICardsProps) {
  return (
    <div className="grid grid-cols-2 gap-4">
      {kpis.map((k, idx) => (
        <KpiCard key={k.title} kpi={k} delay={idx * 50} />
      ))}
    </div>
  );
}

function KpiCard({ kpi, delay = 0 }: { kpi: KPI; delay?: number }) {
  const IconMap = {
      check: <CheckCircle size={22} />,
      users: <Users size={22} />,
      speed: <Activity size={22} />,
      alert: <AlertCircle size={22} />
  };

  return (
    <div
        className="bg-white rounded-[24px] p-6 shadow-[0_4px_25px_rgba(0,0,0,0.03)] border border-black/5 flex flex-col items-end gap-4"
        style={{ animationDelay: `${delay}ms` }}
    >
      <div className="w-12 h-12 rounded-[16px] flex items-center justify-center shadow-inner" style={{ backgroundColor: `${kpi.color}15`, color: kpi.color }}>
        {IconMap[kpi.icon]}
      </div>
      <div className="text-right">
        <span className="text-[10px] text-[#AFAFAF] font-black uppercase tracking-widest block mb-1">{kpi.title}</span>
        <div className="text-[28px] font-black text-[#2F2F2F] tracking-tight">{kpi.value}</div>
      </div>
    </div>
  );
}
