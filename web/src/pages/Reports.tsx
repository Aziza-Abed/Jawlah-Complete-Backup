import { useEffect, useMemo, useState } from "react";
import { getTasksReport, getWorkersReport, getZonesReport, downloadAttendanceReport, downloadTasksReport } from "../api/reports";
import type {
  TasksReportData,
  WorkersReportData,
  ZonesReportData,
  ReportPeriod,
} from "../types/report";
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

  const [exporting, setExporting] = useState(false);

  const handleExport = async (format: "excel" | "pdf") => {
    const filters = {
      period: applied.period,
      status: applied.status,
      startDate: applied.from || undefined,
      endDate: applied.to || undefined,
    };

    setExporting(true);
    try {
      if (tab === "tasks") {
        await downloadTasksReport(format, filters);
      } else {
        await downloadAttendanceReport(format, filters);
      }
    } catch (error) {
      console.error("Export failed:", error);
      alert("فشل تصدير التقرير. يرجى المحاولة مرة أخرى.");
    } finally {
      setExporting(false);
    }
  };

  const exportPdf = () => {
    handleExport("pdf");
  };

  const exportExcel = () => {
    handleExport("excel");
  };

  const showStatusFilter = tab === "tasks";

  return (
    <div className="h-full w-full bg-[#F3F1ED] overflow-auto">
      <div className="p-4 sm:p-6 md:p-8">
        <div className="max-w-[1400px] mx-auto space-y-6">
        {/* Header */}
        {/* Header (Premium RTL Layout) */}
        <div className="flex flex-col md:flex-row items-center justify-between gap-6">
            <div className="flex items-center gap-4">
                <div className="p-3.5 rounded-[18px] bg-[#7895B2]/10 text-[#7895B2] shadow-sm">
                    <BarChart3 size={28} />
                </div>
                <div className="text-right">
                    <h1 className="font-sans font-black text-[28px] text-[#2F2F2F] tracking-tight">
                        التقارير والإحصائيات
                    </h1>
                    <p className="text-[14px] font-bold text-[#AFAFAF] mt-1">تحليل شامل لأداء النظام والعمال والمناطق</p>
                </div>
            </div>

            <div className="flex items-center gap-4">
                 <div className="bg-white/60 backdrop-blur-md rounded-[18px] p-2 pr-6 border border-black/5 shadow-sm flex items-center gap-4">
                    <div className="text-right">
                        <span className="text-[10px] text-[#AFAFAF] font-black uppercase tracking-widest block">آخر تحديث للبيانات</span>
                        <span className="text-[#2F2F2F] font-black text-[15px]">{view.lastUpdated}</span>
                    </div>
                    <div className={`w-10 h-10 rounded-xl flex items-center justify-center ${loading ? 'bg-[#7895B2] text-white animate-spin' : 'bg-[#F9F8F6] text-[#AFAFAF]'}`}>
                        <Activity size={18} />
                    </div>
                </div>
            </div>
        </div>

        {error && (
            <div className="mb-6 p-4 rounded-[12px] bg-[#C86E5D]/10 border border-[#C86E5D]/20 text-[#C86E5D] flex items-center gap-3">
                <AlertCircle size={20} />
                <span className="font-semibold">{error}</span>
            </div>
        )}

        {/* Tabs - Modern Minimalist */}
        <div className="flex justify-start">
          <div className="bg-white/50 backdrop-blur-sm p-1.5 rounded-[20px] border border-black/5 shadow-sm inline-flex gap-1">
            { [
              { key: "tasks", label: "تقرير المهام", icon: <CheckCircle size={16}/> },
              { key: "workers", label: "تقرير العمال", icon: <Users size={16}/> },
              { key: "zones", label: "تقرير المناطق", icon: <Activity size={16}/> },
            ].map((it) => (
              <button
                key={it.key}
                onClick={() => setTab(it.key as TabKey)}
                className={`flex items-center gap-2 px-6 py-2.5 rounded-[15px] text-[13px] font-black transition-all ${
                  tab === it.key 
                  ? 'bg-[#7895B2] text-white shadow-lg shadow-[#7895B2]/20' 
                  : 'text-[#6B7280] hover:text-[#2F2F2F] hover:bg-white'
                }`}
              >
                {it.icon}
                {it.label}
              </button>
            ))}
          </div>
        </div>

        {/* Filters - Table Style Grid */}
        <div className="bg-white rounded-[24px] p-6 shadow-[0_4px_25px_rgba(0,0,0,0.03)] border border-black/5">
          <div className="flex flex-col xl:flex-row gap-8 items-end">
            <div className="flex-1 grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6 w-full">
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
              />
              
              {draft.period === "custom" ? (
                <div className="grid grid-cols-2 gap-3 animate-in fade-in slide-in-from-top-2 duration-300">
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
              ) : (
                <div className="flex flex-col justify-end">
                   <span className="text-[10px] text-[#AFAFAF] font-black uppercase tracking-widest mb-2 text-right">ملاحظة الفلاتر</span>
                   <p className="text-[12px] font-bold text-[#6B7280] text-right">يتم تطبيق الفلترة على كافة المخططات والجداول أدناه.</p>
                </div>
              )}
            </div>

            <div className="flex items-center gap-3 w-full xl:w-auto">
              <button
                type="button"
                onClick={onReset}
                className="flex-1 xl:flex-none h-[52px] px-8 rounded-[18px] bg-[#F9F8F6] text-[#6B7280] hover:bg-[#F3F1ED] transition-all font-black text-[14px]"
              >
                إعادة تعيين
              </button>
              <button
                type="button"
                onClick={onApply}
                className="flex-1 xl:flex-none h-[52px] px-10 rounded-[18px] bg-[#7895B2] hover:bg-[#647e99] text-white font-black shadow-lg shadow-[#7895B2]/20 transition-all active:scale-95 flex items-center justify-center gap-3 group"
              >
                <Filter size={18} className="group-hover:rotate-12 transition-transform" />
                تطبيق الفلترة
              </button>
            </div>
          </div>
        </div>

        {/* Layout */}
        <div className="grid grid-cols-1 xl:grid-cols-[1fr_360px] gap-8">
          {/* Left - Charts & Table */}
          <div className="space-y-6">
            {/* Chart 1 */}
            <div className="bg-white rounded-[16px] p-5 shadow-[0_4px_20px_rgba(0,0,0,0.04)]">
              <div className="flex items-center justify-between gap-3 mb-6">
                <div className="flex items-center gap-2">
                    <div className="p-2 bg-[#7895B2]/10 rounded-lg text-[#7895B2]">
                        <BarChart3 size={20} />
                    </div>
                    <h3 className="text-xl font-bold text-[#2F2F2F]">
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
            </div>

            {/* Table + Export */}
            {/* Table + Export */}
            <div className="bg-white rounded-[24px] shadow-[0_4px_25px_rgba(0,0,0,0.03)] border border-black/5 overflow-hidden">
                <div className="p-8 border-b border-black/5 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-6">
                    <div className="flex items-center gap-4">
                         <div className="p-3 bg-[#8FA36A]/10 rounded-[14px] text-[#8FA36A]">
                             <Activity size={24} />
                         </div>
                         <div className="text-right">
                             <h3 className="text-[18px] font-black text-[#2F2F2F] tracking-tight">
                                 {view.table.title}
                             </h3>
                             <p className="text-[12px] font-bold text-[#AFAFAF]">تفاصيل البيانات المفلترة حسب خياراتك</p>
                         </div>
                    </div>

                    <div className="flex items-center gap-2">
                      <button
                        type="button"
                        onClick={exportExcel}
                        disabled={exporting}
                        className="h-[44px] px-5 rounded-[12px] bg-[#F9F8F6] text-[#2F2F2F] hover:bg-[#7895B2] hover:text-white transition-all font-black text-xs flex items-center gap-2 disabled:opacity-50 shadow-sm"
                      >
                        <Download size={14} />
                        تصدير Excel
                      </button>
                      <button
                        type="button"
                        onClick={exportPdf}
                        disabled={exporting}
                        className="h-[44px] px-5 rounded-[12px] bg-[#F9F8F6] text-[#2F2F2F] hover:bg-[#7895B2] hover:text-white transition-all font-black text-xs flex items-center gap-2 disabled:opacity-50 shadow-sm"
                      >
                        <Printer size={14} />
                        تصدير PDF
                      </button>
                    </div>
                </div>

              <div className="overflow-x-auto pb-6">
                {view.tab === "tasks" && (
                  <table className="w-full text-right">
                    <thead>
                      <tr className="border-b border-black/5 bg-[#F9F8F6]/50">
                        {view.table.columns.map((c) => (
                          <th key={c} className="px-8 py-5 text-[11px] font-black text-[#AFAFAF] uppercase tracking-widest">
                            {c}
                          </th>
                        ))}
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-black/5">
                      {view.table.rows.map((r) => (
                        <tr key={r.id} className="hover:bg-[#F9F8F6] transition-all group">
                          <td className="px-8 py-5 text-[12px] font-black text-[#AFAFAF]">{r.id}</td>
                          <td className="px-8 py-5 text-[14px] font-black text-[#2F2F2F] group-hover:text-[#7895B2]">{r.title}</td>
                          <td className="px-8 py-5 text-[13px] font-bold text-[#6B7280]">{r.worker}</td>
                          <td className="px-8 py-5 text-[13px] font-bold text-[#6B7280]">{r.zone}</td>
                          <td className="px-8 py-5">
                              <span className={`px-3 py-1 rounded-full text-[10px] font-black border tracking-wider uppercase ${
                                  r.status === 'مكتملة' ? 'bg-[#8FA36A]/10 text-[#8FA36A] border-[#8FA36A]/20' :
                                  r.status === 'قيد التنفيذ' ? 'bg-[#7895B2]/10 text-[#7895B2] border-[#7895B2]/20' :
                                  r.status === 'معلقة' ? 'bg-[#C86E5D]/10 text-[#C86E5D] border-[#C86E5D]/20' :
                                  r.status === 'مرفوضة' ? 'bg-red-500/10 text-red-600 border-red-500/20' : 'bg-[#7895B2]/5 text-[#AFAFAF] border-black/5'
                              }`}>
                                {r.status}
                              </span>
                          </td>
                          <td className="px-8 py-5 text-[13px] font-bold text-[#6B7280]">{r.priority}</td>
                          <td className="px-8 py-5 text-[13px] font-black text-[#2F2F2F]">{r.dueDate}</td>
                          <td className="px-8 py-5 text-[12px] font-bold text-[#AFAFAF]">{r.time}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                )}

                {view.tab === "workers" && (
                  <table className="w-full text-right">
                    <thead>
                      <tr className="border-b border-black/5 bg-[#F9F8F6]/50">
                        {view.table.columns.map((c) => (
                          <th key={c} className="px-8 py-5 text-[11px] font-black text-[#AFAFAF] uppercase tracking-widest">
                            {c}
                          </th>
                        ))}
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-black/5">
                      {view.table.rows.map((r) => (
                        <tr key={r.id} className="hover:bg-[#F9F8F6] transition-all group">
                          <td className="px-8 py-5 text-[14px] font-black text-[#2F2F2F] group-hover:text-[#7895B2]">{r.name}</td>
                          <td className="px-8 py-5">
                              <span className={`px-3 py-1 rounded-full text-[10px] font-black border tracking-wider uppercase ${r.presence === 'حضور' ? 'bg-[#8FA36A]/10 text-[#8FA36A] border-[#8FA36A]/20' : 'bg-[#C86E5D]/10 text-[#C86E5D] border-[#C86E5D]/20'}`}>
                                  {r.presence}
                              </span>
                          </td>
                          <td className="px-8 py-5 text-[13px] font-black text-[#2F2F2F]">{r.lastSeen}</td>
                          <td className="px-8 py-5 text-[14px] font-black text-[#7895B2]">{r.activeTasks}</td>
                          <td className="px-8 py-5 text-[14px] font-black text-[#8FA36A]">{r.doneTasks}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                )}

                {view.tab === "zones" && (
                  <table className="w-full text-right">
                    <thead>
                      <tr className="border-b border-black/5 bg-[#F9F8F6]/50">
                        {view.table.columns.map((c) => (
                          <th key={c} className="px-8 py-5 text-[11px] font-black text-[#AFAFAF] uppercase tracking-widest">
                            {c}
                          </th>
                        ))}
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-black/5">
                      {view.table.rows.map((r) => (
                        <tr key={r.id} className="hover:bg-[#F9F8F6] transition-all group">
                          <td className="px-8 py-5 text-[14px] font-black text-[#2F2F2F] group-hover:text-[#7895B2]">{r.zone}</td>
                          <td className="px-8 py-5 text-[13px] font-black text-[#6B7280]">{r.total}</td>
                          <td className="px-8 py-5 text-[13px] font-black text-[#8FA36A]">{r.done}</td>
                          <td className="px-8 py-5 text-[13px] font-black text-[#7895B2]">{r.inProgress}</td>
                          <td className="px-8 py-5 text-[13px] font-black text-[#C86E5D]">{r.delayed}</td>
                          <td className="px-8 py-5 text-[13px] font-black text-[#2F2F2F]">{r.rate}</td>
                          <td className="px-8 py-5 text-[12px] font-bold text-[#AFAFAF]">{r.updatedAt}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                )}
              </div>
            </div>

            <div className="text-right text-xs text-[#6B7280] px-2">
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
            <div className="bg-white rounded-[16px] p-5 shadow-[0_4px_20px_rgba(0,0,0,0.04)]">
              <div className="flex items-center gap-2 mb-6">
                <div className="p-2 bg-[#7895B2]/10 rounded-lg text-[#7895B2]">
                    <PieChart size={20} />
                </div>
                <h3 className="text-lg font-bold text-[#2F2F2F]">
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
            </div>
          </div>
        </div>
      </div>
      </div>
    </div>
  );
}

/* ---------- UI ---------- */

function Select({
  label,
  value,
  onChange,
  options,
  disabled,
}: {
  label: string;
  value: string;
  onChange: (v: string) => void;
  options: { value: string; label: string }[];
  disabled?: boolean;
}) {
  return (
    <div className="w-full relative">
      <div className="text-right text-[10px] text-[#AFAFAF] font-black uppercase tracking-widest mb-2">
        {label}
      </div>
      <div className="relative">
        <select
          value={value}
          onChange={(e) => onChange(e.target.value)}
          disabled={disabled}
          className={[
            "w-full h-[52px] pr-6 pl-12 bg-[#F9F8F6] rounded-[18px] text-[14px] font-black text-[#2F2F2F] border-0 outline-none focus:ring-4 focus:ring-[#7895B2]/10 transition-all appearance-none cursor-pointer",
            disabled ? "opacity-40 cursor-not-allowed" : "",
          ].join(" ")}
        >
          {options.map((o) => (
            <option key={o.value} value={o.value}>
              {o.label}
            </option>
          ))}
        </select>

        <div className="pointer-events-none absolute left-4 top-1/2 -translate-y-1/2 text-[#AFAFAF]">
          <ChevronDown size={18} />
        </div>
      </div>
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
      <div className="text-right text-[10px] text-[#AFAFAF] font-black uppercase tracking-widest mb-2">
        {label}
      </div>
      <div className="relative">
        <input
            type="date"
            value={value}
            onChange={(e) => onChange(e.target.value)}
            className="w-full h-[52px] pr-6 pl-12 bg-[#F9F8F6] rounded-[18px] text-[14px] font-black text-[#2F2F2F] border-0 outline-none focus:ring-4 focus:ring-[#7895B2]/10 transition-all text-right"
        />
        <Calendar size={18} className="absolute left-4 top-1/2 -translate-y-1/2 text-[#AFAFAF] pointer-events-none" />
      </div>
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

function Legend({ items }: { items: LegendItem[] }) {
  return (
    <div className="flex items-center gap-4 flex-wrap justify-end">
      {items.map((it) => (
        <div key={it.label} className="flex items-center gap-1.5">
          <div className="text-[10px] font-bold text-[#6B7280] uppercase">{it.label}</div>
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
    <div className="w-full bg-[#7895B2]/5 rounded-xl border border-[#7895B2]/5 p-4 overflow-hidden relative">
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
              <div className="mt-3 text-[10px] font-bold text-[#6B7280] group-hover:text-white transition-colors">{p.label}</div>
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
    <div className="w-full bg-[#7895B2]/5 rounded-xl border border-[#7895B2]/5 p-5">
      <div className="space-y-4">
        {items.map((it) => {
          const w = Math.round((it.value / max) * 100);
          return (
            <div key={it.label} className="flex items-center gap-4">
              <div className="w-[32px] text-right text-xs font-bold font-sans text-[#2F2F2F]">{it.value}</div>
              <div className="flex-1 h-[6px] bg-[#7895B2]/10 rounded-full overflow-hidden">
                <div className="h-full rounded-full shadow-[0_0_8px_rgba(255,255,255,0.2)]" style={{ width: `${w}%`, backgroundColor: it.color }} />
              </div>
              <div className="w-[100px] text-right text-xs font-bold text-[#6B7280] truncate">{it.label}</div>
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
            <div className="text-xs font-sans font-bold text-[#6B7280]">{p.value}%</div>
            <div className="flex items-center gap-2">
              <div className="text-xs font-bold text-[#6B7280]">{p.label}</div>
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
