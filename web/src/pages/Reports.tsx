import React, { useEffect, useMemo, useState } from "react";
import { getTasksReport, getWorkersReport, getZonesReport, getAttendanceReportUrl, getTasksReportUrl } from "../api/reports";
import type {
  TasksReportData,
  WorkersReportData,
  ZonesReportData,
  ReportPeriod,
} from "../types/report";
import GlassCard from "../components/UI/GlassCard";
import {
    Download,
    Calendar,
    Filter,
    CheckCircle,
    AlertCircle,
    Users,
    Activity,
    BarChart3,
    PieChart,
    ChevronDown,
    Printer
} from "lucide-react";

type TabKey = "tasks" | "workers" | "zones";
type PeriodPreset = "daily" | "weekly" | "monthly" | "yearly" | "custom";
type TaskStatus = "all" | "open" | "in_progress" | "closed" | "pending";

type FiltersDraft = {
  period: PeriodPreset;
  status: TaskStatus;
  from: string;
  to: string;
};

type KPI = {
  title: string;
  value: string;
  icon: "check" | "users" | "speed" | "alert";
  color: string;
};

type SeriesPoint = { label: string; a: number; b: number; c?: number };
type LegendItem = { label: string; color: string };

type TaskRow = {
  id: string;
  title: string;
  worker: string;
  zone: string;
  status: string;
  priority: string;
  dueDate: string;
  time: string;
};

type WorkerRow = {
  id: string;
  name: string;
  presence: string;
  lastSeen: string;
  activeTasks: number;
  doneTasks: number;
};

type ZoneRow = {
  id: string;
  zone: string;
  total: number;
  done: number;
  inProgress: number;
  delayed: number;
  rate: string;
  updatedAt: string;
};

type DonutPart = { label: string; value: number; color: string };

type ViewModel =
  | {
      tab: "tasks";
      title: string;
      lastUpdated: string;
      filtersNote: string;
      kpis: KPI[];
      chart1: {
        title: string;
        legend: LegendItem[];
        points: SeriesPoint[];
      };
      chart2: {
        title: string;
        parts: DonutPart[];
      };
      table: {
        title: string;
        columns: string[];
        rows: TaskRow[];
      };
    }
  | {
      tab: "workers";
      title: string;
      lastUpdated: string;
      filtersNote: string;
      kpis: KPI[];
      chart1: {
        title: string;
        legend: LegendItem[];
        points: SeriesPoint[];
      };
      chart2: {
        title: string;
        items: { label: string; value: number; color: string }[];
      };
      table: {
        title: string;
        columns: string[];
        rows: WorkerRow[];
      };
    }
  | {
      tab: "zones";
      title: string;
      lastUpdated: string;
      filtersNote: string;
      kpis: KPI[];
      chart1: {
        title: string;
        items: { label: string; value: number; color: string }[];
      };
      chart2: {
        title: string;
        parts: DonutPart[];
      };
      table: {
        title: string;
        columns: string[];
        rows: ZoneRow[];
      };
    };

export default function Reports() {
  const [tab, setTab] = useState<TabKey>("tasks");

  const [draft, setDraft] = useState<FiltersDraft>({
    period: "monthly",
    status: "all",
    from: "",
    to: "",
  });

  const [applied, setApplied] = useState<FiltersDraft>(draft);

  // API data states
  const [tasksData, setTasksData] = useState<TasksReportData | null>(null);
  const [workersData, setWorkersData] = useState<WorkersReportData | null>(null);
  const [zonesData, setZonesData] = useState<ZonesReportData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const fetchReport = async (currentTab: TabKey, filters: FiltersDraft) => {
    setLoading(true);
    setError("");
    try {
      const apiFilters = {
        period: filters.period as ReportPeriod,
        status: filters.status !== "all" ? filters.status : undefined,
        startDate: filters.from || undefined,
        endDate: filters.to || undefined,
      };

      if (currentTab === "tasks") {
        const data = await getTasksReport(apiFilters);
        setTasksData(data);
      } else if (currentTab === "workers") {
        const data = await getWorkersReport(apiFilters);
        setWorkersData(data);
      } else {
        const data = await getZonesReport(apiFilters);
        setZonesData(data);
      }
    } catch (err) {
      setError("فشل في تحميل التقرير");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchReport(tab, applied);
  }, [tab, applied]);

  const onApply = () => {
    setApplied(draft);
  };

  const onReset = () => {
    const base: FiltersDraft = { period: "monthly", status: "all", from: "", to: "" };
    setDraft(base);
    setApplied(base);
  };

  const lastUpdated = useMemo(() => {
    const d = new Date();
    const hh = String(d.getHours()).padStart(2, "0");
    const mm = String(d.getMinutes()).padStart(2, "0");
    return `${hh}:${mm}`;
  }, [tab, applied]);

  const view = useMemo<ViewModel>(() => {
    // Use API data if available, otherwise fallback to mock
    if (tab === "tasks" && tasksData) {
      return buildViewFromTasksApi(tasksData, applied, lastUpdated);
    }
    if (tab === "workers" && workersData) {
      return buildViewFromWorkersApi(workersData, applied, lastUpdated);
    }
    if (tab === "zones" && zonesData) {
      return buildViewFromZonesApi(zonesData, applied, lastUpdated);
    }
    return buildMockView(tab, applied, lastUpdated);
  }, [tab, applied, lastUpdated, tasksData, workersData, zonesData]);

  const handleExport = (format: "excel" | "pdf") => {
    const filters = {
      period: applied.period,
      status: applied.status,
      startDate: applied.from || undefined,
      endDate: applied.to || undefined,
    };

    let url = "";
    if (tab === "tasks") {
      url = getTasksReportUrl(format, filters);
    } else {
      url = getAttendanceReportUrl(format, filters);
    }
    
    window.open(url, "_blank");
  };

  const exportPdf = () => {
    handleExport("pdf");
  };

  const exportExcel = () => {
    handleExport("excel");
  };

  const showStatusFilter = tab === "tasks";

  return (
    <div className="space-y-8 pb-10 motion-safe:scroll-smooth will-change-transform">
      <div className="max-w-[1400px] mx-auto">
        {/* Header */}
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-6 animate-fade-in mb-8">
            <div className="flex flex-col items-start">
                <h1 className="text-3xl font-extrabold text-text-primary text-right">
                    التقارير والإحصائيات
                </h1>
                <p className="text-right text-text-secondary mt-2 font-medium">تحليل شامل لأداء النظام والعمال والمناطق</p>
            </div>
            
            <GlassCard variant="panel" className="!p-0 !bg-background-paper !border-primary/5 flex items-center overflow-hidden shadow-sm !backdrop-blur-none">
                <div className="px-5 py-3 border-l border-primary/5 flex flex-col items-center">
                    <span className="text-[10px] text-text-muted font-bold uppercase tracking-wider mb-0.5">آخر تحديث</span>
                    <span className="text-text-primary font-mono font-bold">{view.lastUpdated}</span>
                </div>
                {loading && (
                    <div className="px-4 py-3 bg-primary/5 animate-pulse">
                        <Activity size={20} className="text-primary" />
                    </div>
                )}
            </GlassCard>
        </div>

        {error && (
            <GlassCard className="mb-6 !bg-red-500/10 !border-red-500/20 text-red-300 flex items-center gap-3">
                <AlertCircle size={20} />
                <span>{error}</span>
            </GlassCard>
        )}

        {/* Tabs */}
        <div className="mb-8">
          <Tabs
            value={tab}
            onChange={(v) => setTab(v)}
            items={[
              { key: "tasks", label: "تقرير المهام", icon: <CheckCircle size={16}/> },
              { key: "workers", label: "تقرير العمال", icon: <Users size={16}/> },
              { key: "zones", label: "تقرير المناطق", icon: <Activity size={16}/> },
            ]}
          />
        </div>

        {/* Filters */}
        <GlassCard className="mb-8 p-6">
          <div className="grid grid-cols-1 lg:grid-cols-[1fr_auto] gap-6 items-start">
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
              <Select
                label="الفترة الزمنية"
                value={draft.period}
                onChange={(v) => setDraft((p) => ({ ...p, period: v as PeriodPreset }))}
                options={[
                  { value: "daily", label: "يومي" },
                  { value: "weekly", label: "أسبوعي" },
                  { value: "monthly", label: "شهري" },
                  { value: "yearly", label: "سنوي" },
                  { value: "custom", label: "مخصص" },
                ]}
              />

              <Select
                label="نوع التقرير"
                value={tab}
                onChange={(v) => setTab(v as TabKey)}
                options={[
                  { value: "tasks", label: "تقرير المهام" },
                  { value: "workers", label: "تقرير العمال" },
                  { value: "zones", label: "تقرير المناطق" },
                ]}
              />

              <Select
                label="حالة المهمة"
                value={draft.status}
                onChange={(v) => setDraft((p) => ({ ...p, status: v as TaskStatus }))}
                options={[
                  { value: "all", label: "الكل" },
                  { value: "open", label: "مفتوحة" },
                  { value: "in_progress", label: "قيد التنفيذ" },
                  { value: "pending", label: "معلقة" },
                  { value: "closed", label: "مغلقة" },
                ]}
                disabled={!showStatusFilter}
                helper={!showStatusFilter ? "متاح فقط في تقرير المهام" : undefined}
              />
            </div>

            <div className="flex items-center gap-3 justify-end pt-2">
              <button
                type="button"
                onClick={onReset}
                className="h-12 px-6 rounded-xl border border-primary/10 text-text-secondary hover:bg-primary/5 transition-all font-bold"
              >
                إعادة تعيين
              </button>
              <button
                type="button"
                onClick={onApply}
                className="h-12 px-8 rounded-xl bg-primary hover:bg-primary-dark text-white font-bold shadow-lg shadow-primary/20 transition-all active:scale-95 flex items-center gap-2"
              >
                <Filter size={18} />
                تطبيق
              </button>
            </div>
          </div>

          {draft.period === "custom" && (
            <div className="mt-6 pt-6 border-t border-primary/10 grid grid-cols-1 sm:grid-cols-2 gap-6 animate-fade-in">
              <DateField
                label="من تاريخ"
                value={draft.from}
                onChange={(v) => setDraft((p) => ({ ...p, from: v }))}
              />
              <DateField
                label="إلى تاريخ"
                value={draft.to}
                onChange={(v) => setDraft((p) => ({ ...p, to: v }))}
              />
            </div>
          )}
        </GlassCard>

        {/* Layout */}
        <div className="grid grid-cols-1 xl:grid-cols-[1fr_360px] gap-8">
          {/* Left - Charts & Table */}
          <div className="space-y-8">
            {/* Chart 1 */}
            <GlassCard>
              <div className="flex items-center justify-between gap-3 mb-6">
                <div className="flex items-center gap-2">
                    <div className="p-2 bg-primary/10 rounded-lg text-primary">
                        <BarChart3 size={20} />
                    </div>
                    <h3 className="text-xl font-bold text-text-primary">
                    {view.tab === "tasks" || view.tab === "workers" ? view.chart1.title : view.chart1.title}
                    </h3>
                </div>

                {view.tab === "tasks" && <Legend items={view.chart1.legend} />}
                {view.tab === "workers" && <Legend items={view.chart1.legend} />}
              </div>

              <div className="mt-3">
                {view.tab === "tasks" && (
                  <BarChartGrouped
                    points={view.chart1.points}
                    colors={{
                      a: view.chart1.legend[0].color,
                      b: view.chart1.legend[1].color,
                      c: view.chart1.legend[2]?.color,
                    }}
                  />
                )}

                {view.tab === "workers" && (
                  <BarChartGrouped
                    points={view.chart1.points}
                    colors={{
                      a: view.chart1.legend[0].color,
                      b: view.chart1.legend[1].color,
                    }}
                  />
                )}

                {view.tab === "zones" && <HorizontalBars items={view.chart1.items} />}
              </div>
            </GlassCard>

            {/* Table + Export */}
            <GlassCard noPadding className="overflow-hidden !backdrop-blur-none bg-white/80">
                <div className="p-6 border-b border-primary/5 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
                    <div className="flex items-center gap-2">
                         <div className="p-2 bg-secondary/10 rounded-lg text-secondary">
                             <CheckCircle size={20} />
                         </div>
                         <h3 className="text-xl font-bold text-text-primary">
                             {view.table.title}
                         </h3>
                    </div>

                    <div className="flex items-center gap-3 justify-end">
                      <button
                        type="button"
                        onClick={exportExcel}
                        className="h-10 px-4 rounded-xl border border-primary/10 text-text-primary hover:bg-primary/5 transition-colors font-bold text-sm flex items-center gap-2"
                      >
                        Excel
                        <Download size={16} />
                      </button>
                      <button
                        type="button"
                        onClick={exportPdf}
                        className="h-10 px-4 rounded-xl border border-primary/10 text-text-primary hover:bg-primary/5 transition-colors font-bold text-sm flex items-center gap-2"
                      >
                        PDF
                        <Printer size={16} />
                      </button>
                    </div>
                </div>

              <div className="overflow-x-auto">
                {view.tab === "tasks" && (
                  <table className="min-w-[980px] w-full text-right">
                    <thead className="bg-primary/5 text-text-muted text-xs font-bold uppercase tracking-wider">
                      <tr>
                        {view.table.columns.map((c) => (
                          <th key={c} className="px-6 py-4 whitespace-nowrap">
                            {c}
                          </th>
                        ))}
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-primary/5">
                      {view.table.rows.map((r) => (
                        <tr key={r.id} className="hover:bg-primary/5 transition-colors text-sm text-text-secondary">
                          <td className="px-6 py-4 whitespace-nowrap font-mono">{r.id}</td>
                          <td className="px-6 py-4 whitespace-nowrap font-bold text-text-primary">{r.title}</td>
                          <td className="px-6 py-4 whitespace-nowrap">{r.worker}</td>
                          <td className="px-6 py-4 whitespace-nowrap">{r.zone}</td>
                          <td className="px-6 py-4 whitespace-nowrap">
                              <span className={`px-2 py-0.5 rounded-lg text-xs font-bold ${
                                  r.status === 'مكتملة' ? 'bg-secondary/20 text-secondary' :
                                  r.status === 'قيد التنفيذ' ? 'bg-primary/20 text-primary' :
                                  r.status === 'معلقة' ? 'bg-accent/20 text-accent' :
                                  r.status === 'مرفوضة' ? 'bg-red-500/20 text-red-600' : 'bg-primary/5 text-text-muted'
                              }`}>
                                {r.status}
                              </span>
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap">{r.priority}</td>
                          <td className="px-6 py-4 whitespace-nowrap font-mono">{r.dueDate}</td>
                          <td className="px-6 py-4 whitespace-nowrap font-mono">{r.time}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                )}

                {view.tab === "workers" && (
                  <table className="min-w-[820px] w-full text-right">
                    <thead className="bg-primary/5 text-text-muted text-xs font-bold uppercase tracking-wider">
                      <tr>
                        {view.table.columns.map((c) => (
                          <th key={c} className="px-6 py-4 whitespace-nowrap">
                            {c}
                          </th>
                        ))}
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-primary/5">
                      {view.table.rows.map((r) => (
                        <tr key={r.id} className="hover:bg-primary/5 transition-colors text-sm text-text-secondary">
                          <td className="px-6 py-4 whitespace-nowrap font-bold text-text-primary">{r.name}</td>
                          <td className="px-6 py-4 whitespace-nowrap">
                              <span className={`px-2 py-0.5 rounded-lg text-xs font-bold ${r.presence === 'حضور' ? 'bg-secondary/20 text-secondary' : 'bg-accent/20 text-accent'}`}>
                                  {r.presence}
                              </span>
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap font-mono">{r.lastSeen}</td>
                          <td className="px-6 py-4 whitespace-nowrap font-mono">{r.activeTasks}</td>
                          <td className="px-6 py-4 whitespace-nowrap font-mono">{r.doneTasks}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                )}

                {view.tab === "zones" && (
                  <table className="min-w-[900px] w-full text-right">
                    <thead className="bg-primary/5 text-text-muted text-xs font-bold uppercase tracking-wider">
                      <tr>
                        {view.table.columns.map((c) => (
                          <th key={c} className="px-6 py-4 whitespace-nowrap">
                            {c}
                          </th>
                        ))}
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-primary/5">
                      {view.table.rows.map((r) => (
                        <tr key={r.id} className="hover:bg-primary/5 transition-colors text-sm text-text-secondary">
                          <td className="px-6 py-4 whitespace-nowrap font-bold text-text-primary">{r.zone}</td>
                          <td className="px-6 py-4 whitespace-nowrap font-mono">{r.total}</td>
                          <td className="px-6 py-4 whitespace-nowrap font-mono text-secondary">{r.done}</td>
                          <td className="px-6 py-4 whitespace-nowrap font-mono text-primary">{r.inProgress}</td>
                          <td className="px-6 py-4 whitespace-nowrap font-mono text-accent">{r.delayed}</td>
                          <td className="px-6 py-4 whitespace-nowrap font-bold">{r.rate}</td>
                          <td className="px-6 py-4 whitespace-nowrap font-mono">{r.updatedAt}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                )}
              </div>
            </GlassCard>
            
            <div className="text-right text-xs text-text-muted px-2">
                 {view.filtersNote}
            </div>
          </div>

          {/* Right - KPIs & Donut */}
          <div className="space-y-6">
            {/* KPIs */}
            <div className="grid grid-cols-2 gap-4">
              {view.kpis.map((k, idx) => (
                <KpiCard key={k.title} kpi={k} delay={idx * 50} />
              ))}
            </div>

            {/* Chart 2 */}
            <GlassCard>
              <div className="flex items-center gap-2 mb-6">
                <div className="p-2 bg-primary/10 rounded-lg text-primary">
                    <PieChart size={20} />
                </div>
                <h3 className="text-lg font-bold text-text-primary">
                  {view.tab === "tasks" ? view.chart2.title : view.tab === "workers" ? view.chart2.title : view.chart2.title}
                </h3>
              </div>

              <div>
                {view.tab === "tasks" && (
                  <DonutWithLegend parts={view.chart2.parts} />
                )}

                {view.tab === "workers" && (
                  <HorizontalBars items={view.chart2.items} />
                )}

                {view.tab === "zones" && (
                  <DonutWithLegend parts={view.chart2.parts} />
                )}
              </div>
            </GlassCard>
          </div>
        </div>
      </div>
    </div>
  );
}

/* ---------- UI ---------- */

function Tabs({
  value,
  onChange,
  items,
}: {
  value: TabKey;
  onChange: (v: TabKey) => void;
  items: { key: TabKey; label: string, icon?: React.ReactNode }[];
}) {
  return (
    <GlassCard noPadding className="inline-flex p-1 gap-1">
      {items.map((it) => {
        const active = value === it.key;
        return (
          <button
            key={it.key}
            type="button"
            onClick={() => onChange(it.key)}
            className={[
              "h-10 px-5 rounded-lg font-bold text-sm transition-all flex items-center gap-2",
              active
                ? "bg-primary text-white shadow-md shadow-primary/30"
                : "text-text-muted hover:bg-primary/5 hover:text-primary",
            ].join(" ")}
          >
            {it.icon}
            {it.label}
          </button>
        );
      })}
    </GlassCard>
  );
}

function Select({
  label,
  value,
  onChange,
  options,
  disabled,
  helper,
}: {
  label: string;
  value: string;
  onChange: (v: string) => void;
  options: { value: string; label: string }[];
  disabled?: boolean;
  helper?: string;
}) {
  return (
    <div className="w-full relative group">
      <div className="text-right text-xs text-text-muted font-bold mb-2 uppercase tracking-wide">
        {label}
      </div>
      <div className="relative">
        <select
          value={value}
          onChange={(e) => onChange(e.target.value)}
          disabled={disabled}
          className={[
            "glass-input w-full h-[48px] px-4 text-right appearance-none cursor-pointer",
            "focus:bg-primary/5 text-text-primary [&>option]:text-black",
            disabled ? "opacity-50 cursor-not-allowed" : "",
          ].join(" ")}
        >
          {options.map((o) => (
            <option key={o.value} value={o.value}>
              {o.label}
            </option>
          ))}
        </select>

        <div className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-text-muted">
          <ChevronDown size={16} />
        </div>
      </div>

      {helper && <div className="mt-1 text-right text-[10px] text-text-muted opacity-60">{helper}</div>}
    </div>
  );
}

function DateField({
  label,
  value,
  onChange,
}: {
  label: string;
  value: string;
  onChange: (v: string) => void;
}) {
  return (
    <div className="w-full">
      <div className="text-right text-xs text-text-muted font-bold mb-2 uppercase tracking-wide">
        {label}
      </div>
      <div className="relative">
        <input
            type="date"
            value={value}
            onChange={(e) => onChange(e.target.value)}
            className="glass-input w-full h-[48px] px-4 text-right focus:bg-primary/5 text-text-primary"
        />
        <Calendar size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-text-muted pointer-events-none" />
      </div>
    </div>
  );
}

function KpiCard({ kpi, delay = 0 }: { kpi: KPI; delay?: number }) {
  // Map internal icon names to Lucide icons
  const IconMap = {
      check: <CheckCircle size={24} />,
      users: <Users size={24} />,
      speed: <Activity size={24} />,
      alert: <AlertCircle size={24} />
  };

  return (
    <GlassCard 
        className="flex flex-col gap-1 !p-5 relative overflow-hidden group animate-slide-up !backdrop-blur-none bg-white/80"
        style={{ animationDelay: `${delay}ms` }}
    >
      {/* Decorative gradient blob */}
      <div className="absolute top-0 right-0 w-24 h-24 bg-gradient-to-br from-white/5 to-transparent rounded-bl-[50%] opacity-50"></div>
      
      <div className="flex items-start justify-between gap-3 relative z-10 w-full">
         <div className="p-2.5 rounded-xl bg-primary/10" style={{ color: kpi.color }}>
            {IconMap[kpi.icon]}
        </div>
        <div className="flex-1 text-right">
          <div className="text-xs text-text-secondary font-bold mb-1 opacity-80">{kpi.title}</div>
          <div className="text-xl sm:text-2xl font-extrabold text-text-primary tracking-tight">{kpi.value}</div>
        </div>
      </div>
    </GlassCard>
  );
}

function Legend({ items }: { items: LegendItem[] }) {
  return (
    <div className="flex items-center gap-4 flex-wrap justify-end">
      {items.map((it) => (
        <div key={it.label} className="flex items-center gap-1.5">
          <div className="text-[10px] font-bold text-text-secondary uppercase">{it.label}</div>
          <span className="w-2.5 h-2.5 rounded-full shadow-sm" style={{ backgroundColor: it.color }} />
        </div>
      ))}
    </div>
  );
}

/* ---------- Charts ---------- */

function BarChartGrouped({
  points,
  colors,
}: {
  points: SeriesPoint[];
  colors: { a: string; b: string; c?: string };
}) {
  const maxY = Math.max(1, ...points.flatMap((p) => [p.a, p.b, p.c ?? 0]));

  return (
    <div className="w-full bg-primary/5 rounded-xl border border-primary/5 p-4 overflow-hidden relative">
      <div className="absolute inset-0 bg-[url('https://grainy-gradients.vercel.app/noise.svg')] opacity-10"></div>
      <div className="h-[200px] sm:h-[240px] w-full flex items-end gap-3 relative z-10">
        {points.map((p) => {
          const aH = (p.a / maxY) * 100;
          const bH = (p.b / maxY) * 100;
          const cH = p.c != null ? (p.c / maxY) * 100 : 0;

          return (
            <div key={p.label} className="flex-1 min-w-[24px] h-full flex flex-col justify-end items-center group">
              <div className="h-full w-full flex items-end justify-center gap-1">
                <div className="w-[8px] sm:w-[10px] rounded-t-full transition-all duration-500 group-hover:scale-y-110 opacity-90" style={{ height: `${aH}%`, backgroundColor: colors.a }} />
                <div className="w-[8px] sm:w-[10px] rounded-t-full transition-all duration-500 group-hover:scale-y-110 opacity-90" style={{ height: `${bH}%`, backgroundColor: colors.b }} />
                {p.c != null && (
                  <div className="w-[8px] sm:w-[10px] rounded-t-full transition-all duration-500 group-hover:scale-y-110 opacity-90" style={{ height: `${cH}%`, backgroundColor: colors.c }} />
                )}
              </div>
              <div className="mt-3 text-[10px] font-bold text-text-muted group-hover:text-white transition-colors">{p.label}</div>
            </div>
          );
        })}
      </div>
    </div>
  );
}

function HorizontalBars({
  items,
}: {
  items: { label: string; value: number; color: string }[];
}) {
  const max = Math.max(1, ...items.map((x) => x.value));

  return (
    <div className="w-full bg-primary/5 rounded-xl border border-primary/5 p-5">
      <div className="space-y-4">
        {items.map((it) => {
          const w = Math.round((it.value / max) * 100);
          return (
            <div key={it.label} className="flex items-center gap-4">
              <div className="w-[32px] text-right text-xs font-bold font-mono text-text-primary">{it.value}</div>
              <div className="flex-1 h-[6px] bg-primary/10 rounded-full overflow-hidden">
                <div className="h-full rounded-full shadow-[0_0_8px_rgba(255,255,255,0.2)]" style={{ width: `${w}%`, backgroundColor: it.color }} />
              </div>
              <div className="w-[100px] text-right text-xs font-bold text-text-secondary truncate">{it.label}</div>
            </div>
          );
        })}
      </div>
    </div>
  );
}

function DonutWithLegend({ parts }: { parts: DonutPart[] }) {
  return (
    <div className="flex items-center justify-between gap-4">
      <div className="shrink-0 relative">
        <Donut size={100} stroke={12} parts={parts} background="rgba(255,255,255,0.05)" />
        <div className="absolute inset-0 flex items-center justify-center pointer-events-none">
            <span className="text-xs font-bold text-white/20">KPI</span>
        </div>
      </div>

      <div className="flex-1 space-y-3">
        {parts.map((p) => (
          <div key={p.label} className="flex items-center justify-between gap-2 border-b border-white/5 pb-2 last:border-0 last:pb-0">
            <div className="text-xs font-mono font-bold text-text-muted">{p.value}%</div>
            <div className="flex items-center gap-2">
              <div className="text-xs font-bold text-text-secondary">{p.label}</div>
              <span className="w-2.5 h-2.5 rounded-full shadow-sm" style={{ backgroundColor: p.color }} />
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

function Donut({
  size,
  stroke,
  parts,
  background,
}: {
  size: number;
  stroke: number;
  parts: DonutPart[];
  background: string;
}) {
  const r = (size - stroke) / 2;
  const c = 2 * Math.PI * r;
  const total = parts.reduce((acc, p) => acc + p.value, 0) || 1;

  let offset = 0;

  return (
    <svg width={size} height={size} viewBox={`0 0 ${size} ${size}`}>
      <circle cx={size / 2} cy={size / 2} r={r} fill="none" stroke={background} strokeWidth={stroke} />
      {parts.map((p, idx) => {
        const frac = p.value / total;
        const dash = c * frac;
        const dashArray = `${dash} ${c - dash}`;
        const dashOffset = c * (1 - offset);
        offset += frac;

        return (
          <circle
            key={idx}
            cx={size / 2}
            cy={size / 2}
            r={r}
            fill="none"
            stroke={p.color}
            strokeWidth={stroke}
            strokeLinecap="round"
            strokeDasharray={dashArray}
            strokeDashoffset={dashOffset}
            transform={`rotate(-90 ${size / 2} ${size / 2})`}
            className="transition-all duration-1000 ease-out"
          />
        );
      })}
    </svg>
  );
}

/* ---------- API to ViewModel Builders ---------- */

// Translation helpers for English -> Arabic
function translateStatus(status: string): string {
  const map: Record<string, string> = {
    'Pending': 'معلقة',
    'InProgress': 'قيد التنفيذ',
    'Completed': 'مكتملة',
    'Rejected': 'مرفوضة',
    'Open': 'مفتوحة',
    'Closed': 'مغلقة',
  };
  return map[status] || status;
}

function translatePriority(priority: string): string {
  const map: Record<string, string> = {
    'High': 'عالية',
    'Medium': 'متوسطة',
    'Low': 'منخفضة',
    'Urgent': 'عاجلة',
  };
  return map[priority] || priority;
}

const palette = {
  blue: "#60A5FA", // brighter blue for dark mode
  green: "#34D399", // emerald
  red: "#F87171", // soft red
  gray: "#94A3B8", // slate 400
  light: "#F1F5F9",
  purple: "#A78BFA"
};

function buildViewFromTasksApi(data: TasksReportData, f: FiltersDraft, lastUpdated: string): ViewModel {
  const total = data.total || 1;
  const completedPct = Math.round((data.completed / total) * 100);
  const inProgressPct = Math.round((data.inProgress / total) * 100);
  const pendingPct = 100 - completedPct - inProgressPct;

  // Map byPeriod to chart points
  const points: SeriesPoint[] = data.byPeriod.map((p) => ({
    label: p.label,
    a: p.completed,
    b: p.inProgress,
    c: p.pending,
  }));

  const rows: TaskRow[] = data.tasks.map((t) => ({
    id: t.id.toString(),
    title: t.title,
    worker: t.worker,
    zone: t.zone,
    status: translateStatus(t.status),
    priority: translatePriority(t.priority),
    dueDate: t.dueDate ? new Date(t.dueDate).toLocaleDateString("ar-EG") : "—",
    time: t.createdAt ? new Date(t.createdAt).toLocaleTimeString("ar-EG", { hour: "2-digit", minute: "2-digit" }) : "—",
  }));

  return {
    tab: "tasks",
    title: "تقرير المهام",
    lastUpdated,
    filtersNote: "سيتم تنزيل التقرير حسب الفلاتر الحالية",
    kpis: [
      { title: "إجمالي المهام", value: data.total.toString(), icon: "check", color: palette.blue },
      { title: "معدل الإنجاز", value: `${completedPct}%`, icon: "speed", color: palette.green },
      { title: "مهام معلقة", value: data.pending.toString(), icon: "alert", color: palette.red },
      { title: "العمال النشطين", value: `${data.activeWorkers} / ${data.totalWorkers}`, icon: "users", color: palette.purple },
    ],
    chart1: {
      title: "أداء المهام حسب الفترة",
      legend: [
        { label: "مكتملة", color: palette.green },
        { label: "نشطة", color: palette.blue },
        { label: "معلقة", color: palette.red },
      ],
      points: points.length > 0 ? points : buildLabelsForPeriod(f.period).map((label) => ({ label, a: 0, b: 0, c: 0 })),
    },
    chart2: {
      title: "توزيع حالة المهام",
      parts: [
        { label: "مكتملة", value: completedPct, color: palette.green },
        { label: "نشطة", value: inProgressPct, color: palette.blue },
        { label: "معلقة", value: pendingPct, color: palette.red },
      ],
    },
    table: {
      title: "جدول المهام التفصيلي",
      columns: ["#", "المهمة", "العامل", "المنطقة", "الحالة", "الأولوية", "الموعد", "الوقت"],
      rows,
    },
  };
}

function buildViewFromWorkersApi(data: WorkersReportData, f: FiltersDraft, lastUpdated: string): ViewModel {
  // Map byPeriod to chart points
  const points: SeriesPoint[] = data.byPeriod.map((p) => ({
    label: p.label,
    a: p.present,
    b: p.absent,
  }));

  // Top workload horizontal bars
  const topWorkload = data.topWorkload.map((w) => ({
    label: w.name,
    value: w.activeTasks,
    color: palette.blue,
  }));

  // Worker table rows
  const rows: WorkerRow[] = data.workers.map((w) => ({
    id: w.id.toString(),
    name: w.name,
    presence: w.isPresent ? "حضور" : "غياب",
    lastSeen: w.lastCheckIn ? new Date(w.lastCheckIn).toLocaleTimeString("ar-EG", { hour: "2-digit", minute: "2-digit" }) : "—",
    activeTasks: w.activeTasks,
    doneTasks: w.completedTasks,
  }));

  return {
    tab: "workers",
    title: "تقرير العمال",
    lastUpdated,
    filtersNote: "سيتم تنزيل التقرير حسب الفلاتر الحالية",
    kpis: [
      { title: "إجمالي العمال", value: data.totalWorkers.toString(), icon: "users", color: palette.blue },
      { title: "الحضور", value: data.checkedIn.toString(), icon: "check", color: palette.green },
      { title: "الغياب", value: data.absent.toString(), icon: "alert", color: palette.red },
      { title: "الالتزام", value: `${data.compliancePercent}%`, icon: "speed", color: palette.purple },
    ],
    chart1: {
      title: "حضور / غياب العمال حسب الفترة",
      legend: [
        { label: "حضور", color: palette.green },
        { label: "غياب", color: palette.red },
      ],
      points: points.length > 0 ? points : buildLabelsForPeriod(f.period).map((label) => ({ label, a: 0, b: 0 })),
    },
    chart2: {
      title: "أعلى 5 عمّال إنتاجية",
      items: topWorkload.length > 0 ? topWorkload : [{ label: "—", value: 0, color: palette.blue }],
    },
    table: {
      title: "سجل دوام وأداء العمال",
      columns: ["العامل", "الحالة", "آخر ظهور", "مهام فعّالة", "مهام منجزة"],
      rows,
    },
  };
}

function buildViewFromZonesApi(data: ZonesReportData, _f: FiltersDraft, lastUpdated: string): ViewModel {
  const total = data.totalTasks || 1;
  const completedPct = Math.round((data.totalCompleted / total) * 100);
  const inProgressPct = Math.round((data.totalInProgress / total) * 100);
  const delayedPct = Math.round((data.totalDelayed / total) * 100);

  // Top zones by task count
  const topZones = data.zones
    .slice()
    .sort((a, b) => b.total - a.total)
    .slice(0, 5)
    .map((z) => ({
      label: z.name,
      value: z.total,
      color: palette.blue,
    }));

  // Zone table rows
  const rows: ZoneRow[] = data.zones.map((z) => ({
    id: z.id.toString(),
    zone: z.name,
    total: z.total,
    done: z.completed,
    inProgress: z.inProgress,
    delayed: z.delayed,
    rate: z.total > 0 ? `${Math.round((z.completed / z.total) * 100)}%` : "0%",
    updatedAt: z.lastUpdate ? new Date(z.lastUpdate).toLocaleTimeString("ar-EG", { hour: "2-digit", minute: "2-digit" }) : "—",
  }));

  return {
    tab: "zones",
    title: "تقرير المناطق",
    lastUpdated,
    filtersNote: "سيتم تنزيل التقرير حسب الفلاتر الحالية",
    kpis: [
      { title: "عدد المناطق", value: data.totalZones.toString(), icon: "users", color: palette.blue },
      { title: "أعلى منطقة ضغطاً", value: data.highestPressureZone || "—", icon: "alert", color: palette.red },
      { title: "معدل الإنجاز العام", value: `${completedPct}%`, icon: "speed", color: palette.green },
      { title: "بلاغات/تأخير", value: data.totalDelayed.toString(), icon: "check", color: palette.purple },
    ],
    chart1: {
      title: "المناطق الأكثر نشاطاً",
      items: topZones.length > 0 ? topZones : [{ label: "—", value: 0, color: palette.blue }],
    },
    chart2: {
      title: "حالة العمل في المناطق",
      parts: [
        { label: "منجزة", value: completedPct, color: palette.green },
        { label: "قيد التنفيذ", value: inProgressPct, color: palette.blue },
        { label: "متأخرة", value: delayedPct, color: palette.red },
      ],
    },
    table: {
      title: "أداء المناطق التفصيلي",
      columns: ["المنطقة", "الإجمالي", "منجزة", "قيد التنفيذ", "متأخرة", "الإنجاز", "آخر تحديث"],
      rows,
    },
  };
}

/* ---------- Mock Builder ---------- */

function buildMockView(tab: TabKey, f: FiltersDraft, lastUpdated: string): ViewModel {
  const labels = buildLabelsForPeriod(f.period);

  if (tab === "tasks") {
    const points: SeriesPoint[] = labels.map((label, i) => ({
      label,
      a: i % 4 === 0 ? 18 : 28 + (i % 5) * 3,
      b: i % 3 === 0 ? 12 : 18 + (i % 4) * 2,
      c: i % 6 === 0 ? 6 : 8 + (i % 3),
    }));

    const rows: TaskRow[] = [
      { id: "1", title: "تنظيف شارع", worker: "محمد", zone: "المنطقة 2", status: "مكتملة", priority: "متوسطة", dueDate: "2026-01-18", time: "09:15" },
      { id: "2", title: "إصلاح حفرة", worker: "أحمد", zone: "المنطقة 1", status: "نشطة", priority: "عالية", dueDate: "2026-01-18", time: "10:00" },
      { id: "3", title: "رفع نفايات", worker: "كمال", zone: "المنطقة 3", status: "معلقة", priority: "منخفضة", dueDate: "2026-01-19", time: "11:00" },
      { id: "4", title: "تنظيف دوار", worker: "نعيم", zone: "المنطقة 4", status: "مفتوحة", priority: "متوسطة", dueDate: "2026-01-20", time: "12:00" },
      { id: "5", title: "تنظيف شارع", worker: "سامر", zone: "المنطقة 4", status: "مكتملة", priority: "عالية", dueDate: "2026-01-20", time: "01:00" },
    ].filter((r) => {
      if (f.status === "all") return true;
      if (f.status === "closed") return r.status === "مكتملة";
      if (f.status === "in_progress") return r.status === "نشطة";
      if (f.status === "pending") return r.status === "معلقة";
      if (f.status === "open") return r.status === "مفتوحة";
      return true;
    });

    return {
      tab: "tasks",
      title: "تقرير المهام",
      lastUpdated,
      filtersNote: "سيتم تنزيل التقرير حسب الفلاتر الحالية",
      kpis: [
        { title: "إجمالي المهام", value: "33", icon: "check", color: palette.blue },
        { title: "معدل الإنجاز", value: "78%", icon: "speed", color: palette.green },
        { title: "مهام معلقة", value: "3", icon: "alert", color: palette.red },
        { title: "العمال النشطين", value: "28 / 35", icon: "users", color: palette.purple },
      ],
      chart1: {
        title: "أداء المهام حسب الفترة",
        legend: [
          { label: "مكتملة", color: palette.green },
          { label: "نشطة", color: palette.blue },
          { label: "معلقة", color: palette.red },
        ],
        points,
      },
      chart2: {
        title: "توزيع حالة المهام",
        parts: [
          { label: "مكتملة", value: 60, color: palette.green },
          { label: "نشطة", value: 30, color: palette.blue },
          { label: "معلقة", value: 10, color: palette.red },
        ],
      },
      table: {
        title: "جدول المهام التفصيلي",
        columns: ["#", "المهمة", "العامل", "المنطقة", "الحالة", "الأولوية", "الموعد", "الوقت"],
        rows,
      },
    };
  }

  if (tab === "workers") {
    const points: SeriesPoint[] = labels.map((label, i) => ({
      label,
      a: 24 + (i % 4) * 2, // حضور
      b: i % 6 === 0 ? 10 : 6 + (i % 3), // غياب
    }));

    const topWorkload = [
      { label: "محمد أحمد", value: 9, color: palette.blue },
      { label: "أبو عمار", value: 8, color: palette.blue },
      { label: "محمود", value: 7, color: palette.blue },
      { label: "أحمد", value: 6, color: palette.blue },
      { label: "سامر", value: 5, color: palette.blue },
    ];

    const rows: WorkerRow[] = [
      { id: "w1", name: "محمد أحمد", presence: "حضور", lastSeen: "10:45", activeTasks: 3, doneTasks: 6 },
      { id: "w2", name: "أبو عمار", presence: "حضور", lastSeen: "10:40", activeTasks: 4, doneTasks: 4 },
      { id: "w3", name: "أحمد", presence: "غياب", lastSeen: "—", activeTasks: 0, doneTasks: 3 },
      { id: "w4", name: "محمود", presence: "حضور", lastSeen: "10:50", activeTasks: 2, doneTasks: 5 },
      { id: "w5", name: "سامر", presence: "حضور", lastSeen: "10:20", activeTasks: 1, doneTasks: 2 },
    ];

    return {
      tab: "workers",
      title: "تقرير العمال",
      lastUpdated,
      filtersNote: "سيتم تنزيل التقرير حسب الفلاتر الحالية",
      kpis: [
        { title: "إجمالي العمال", value: "35", icon: "users", color: palette.blue },
        { title: "الحضور", value: "28", icon: "check", color: palette.green },
        { title: "الغياب", value: "7", icon: "alert", color: palette.red },
        { title: "الالتزام", value: "80%", icon: "speed", color: palette.purple },
      ],
      chart1: {
        title: "حضور / غياب العمال حسب الفترة",
        legend: [
          { label: "حضور", color: palette.green },
          { label: "غياب", color: palette.red },
        ],
        points: points.map((p) => ({ label: p.label, a: p.a, b: p.b })),
      },
      chart2: {
        title: "أعلى 5 عمّال إنتاجية",
        items: topWorkload,
      },
      table: {
        title: "سجل دوام وأداء العمال",
        columns: ["العامل", "الحالة", "آخر ظهور", "مهام فعّالة", "مهام منجزة"],
        rows,
      },
    };
  }

  const topZones = [
    { label: "المنطقة 4", value: 18, color: palette.blue },
    { label: "المنطقة 2", value: 14, color: palette.blue },
    { label: "المنطقة 1", value: 12, color: palette.blue },
    { label: "المنطقة 3", value: 10, color: palette.blue },
    { label: "المنطقة 5", value: 8, color: palette.blue },
  ];

  const rows: ZoneRow[] = [
    { id: "z1", zone: "المنطقة 1", total: 12, done: 7, inProgress: 3, delayed: 2, rate: "58%", updatedAt: "10:30" },
    { id: "z2", zone: "المنطقة 2", total: 14, done: 9, inProgress: 4, delayed: 1, rate: "64%", updatedAt: "10:25" },
    { id: "z3", zone: "المنطقة 3", total: 10, done: 6, inProgress: 3, delayed: 1, rate: "60%", updatedAt: "10:10" },
    { id: "z4", zone: "المنطقة 4", total: 18, done: 10, inProgress: 6, delayed: 2, rate: "56%", updatedAt: "10:40" },
    { id: "z5", zone: "المنطقة 5", total: 8, done: 5, inProgress: 2, delayed: 1, rate: "62%", updatedAt: "09:55" },
  ];

  return {
    tab: "zones",
    title: "تقرير المناطق",
    lastUpdated,
    filtersNote: "سيتم تنزيل التقرير حسب الفلاتر الحالية",
    kpis: [
      { title: "عدد المناطق", value: "12", icon: "users", color: palette.blue },
      { title: "أعلى ضغط", value: "المنطقة 4", icon: "alert", color: palette.gray },
      { title: "معدل الإنجاز", value: "61%", icon: "speed", color: palette.green },
      { title: "بلاغات/تأخير", value: "7", icon: "check", color: palette.purple },
    ],
    chart1: {
      title: "المناطق الأكثر نشاطاً",
      items: topZones,
    },
    chart2: {
      title: "توزيع حالة المناطق",
      parts: [
        { label: "منجزة", value: 62, color: palette.green },
        { label: "قيد التنفيذ", value: 28, color: palette.blue },
        { label: "متأخرة", value: 10, color: palette.red },
      ],
    },
    table: {
      title: "أداء المناطق التفصيلي",
      columns: ["المنطقة", "الإجمالي", "منجزة", "قيد التنفيذ", "متأخرة", "الإنجاز", "آخر تحديث"],
      rows,
    },
  };
}

function buildLabelsForPeriod(period: PeriodPreset): string[] {
  if (period === "daily") return ["1", "2", "3", "4", "5", "6", "7", "8"];
  if (period === "weekly") return ["س", "ح", "ن", "ث", "ر", "خ", "ج"];
  if (period === "yearly") return ["1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12"];
  if (period === "custom") return ["1", "2", "3", "4", "5", "6", "7", "8", "9", "10"];
  return ["1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12"];
}
