import React, { useEffect, useMemo, useState } from "react";
import { getTasksReport, getWorkersReport, getZonesReport, getAttendanceReportUrl, getTasksReportUrl } from "../api/reports";
import type {
  TasksReportData,
  WorkersReportData,
  ZonesReportData,
  ReportPeriod,
} from "../types/report";

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
  bg: string;
  text?: string;
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
    // Fallback to mock while loading
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
    <div className="h-full w-full bg-[#D9D9D9] overflow-auto">
      <div className="p-4 sm:p-6 md:p-8">
        <div className="max-w-[1100px] mx-auto">
          {/* Header */}
          <div className="flex items-center justify-between gap-3">
            <h1 className="text-right font-sans font-semibold text-[20px] sm:text-[22px] text-[#2F2F2F]">
              التقارير
            </h1>
            <div className="flex items-center gap-3">
              {loading && <div className="text-[12px] text-[#6B7280]">جاري التحميل...</div>}
              <div className="text-right text-[12px] text-[#6B7280]">آخر تحديث: {view.lastUpdated}</div>
            </div>
          </div>

          {error && (
            <div className="mt-4 p-3 bg-red-100 text-red-700 rounded-[10px] text-right">
              {error}
            </div>
          )}

          {/* Tabs */}
          <div className="mt-4">
            <Tabs
              value={tab}
              onChange={(v) => setTab(v)}
              items={[
                { key: "tasks", label: "تقرير المهام" },
                { key: "workers", label: "تقرير العمال" },
                { key: "zones", label: "تقرير المناطق" },
              ]}
            />
          </div>

          {/* Filters */}
          <div className="mt-4 bg-[#F3F1ED] rounded-[14px] border border-black/10 shadow-[0_2px_0_rgba(0,0,0,0.08)] p-4 sm:p-5">
            <div className="grid grid-cols-1 lg:grid-cols-[1fr_auto] gap-4 items-end">
              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
                <Select
                  label="الفترة الزمنية:"
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
                  label="نوع التقرير:"
                  value={tab}
                  onChange={(v) => setTab(v as TabKey)}
                  options={[
                    { value: "tasks", label: "تقرير المهام" },
                    { value: "workers", label: "تقرير العمال" },
                    { value: "zones", label: "تقرير المناطق" },
                  ]}
                />

                <Select
                  label="حالة المهمة:"
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

              <div className="flex items-center gap-3 justify-end">
                <button
                  type="button"
                  onClick={onReset}
                  className="h-[44px] px-4 rounded-[10px] bg-white border border-black/10 text-[#2F2F2F] font-sans font-semibold text-[14px] hover:opacity-95"
                >
                  إعادة تعيين
                </button>
                <button
                  type="button"
                  onClick={onApply}
                  className="h-[44px] px-6 rounded-[10px] bg-[#60778E] text-white font-sans font-semibold text-[14px] shadow-[0_2px_0_rgba(0,0,0,0.15)] hover:opacity-95"
                >
                  تطبيق
                </button>
              </div>
            </div>

            {draft.period === "custom" && (
              <div className="mt-4 grid grid-cols-1 sm:grid-cols-2 gap-3">
                <DateField
                  label="من:"
                  value={draft.from}
                  onChange={(v) => setDraft((p) => ({ ...p, from: v }))}
                />
                <DateField
                  label="إلى:"
                  value={draft.to}
                  onChange={(v) => setDraft((p) => ({ ...p, to: v }))}
                />
              </div>
            )}
          </div>

          {/* Layout */}
          <div className="mt-5 grid grid-cols-1 lg:grid-cols-[1fr_340px] gap-5">
            {/* Left */}
            <div className="space-y-5">
              {/* Chart 1 */}
              <Card>
                <div className="flex items-center justify-between gap-3">
                  <div className="text-right font-sans font-semibold text-[16px] sm:text-[18px] text-[#2F2F2F]">
                    {view.tab === "tasks" || view.tab === "workers" ? view.chart1.title : view.chart1.title}
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
              </Card>

              {/* Table + Export */}
              <Card>
                <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
                  <div className="text-right font-sans font-semibold text-[16px] sm:text-[18px] text-[#2F2F2F]">
                    {view.table.title}
                  </div>

                  <div className="flex items-center gap-2 justify-end">
                    <button
                      type="button"
                      onClick={exportExcel}
                      className="h-[36px] px-3 rounded-[8px] bg-white border border-black/10 text-[#2F2F2F] font-sans font-semibold text-[13px] hover:opacity-95"
                    >
                      Excel
                    </button>
                    <button
                      type="button"
                      onClick={exportPdf}
                      className="h-[36px] px-3 rounded-[8px] bg-white border border-black/10 text-[#2F2F2F] font-sans font-semibold text-[13px] hover:opacity-95"
                    >
                      PDF
                    </button>
                  </div>
                </div>

                <div className="mt-2 text-right text-[12px] text-[#6B7280]">{view.filtersNote}</div>

                <div className="mt-4 overflow-auto rounded-[12px] border border-black/10 bg-white">
                  {view.tab === "tasks" && (
                    <table className="min-w-[980px] w-full text-right">
                      <thead className="bg-[#E9E6E0]">
                        <tr className="text-[12px] text-[#2F2F2F]">
                          {view.table.columns.map((c) => (
                            <th key={c} className="px-3 py-3 font-sans font-semibold whitespace-nowrap">
                              {c}
                            </th>
                          ))}
                        </tr>
                      </thead>
                      <tbody>
                        {view.table.rows.map((r) => (
                          <tr key={r.id} className="border-t border-black/5 text-[12px] text-[#2F2F2F]">
                            <td className="px-3 py-3 whitespace-nowrap">{r.id}</td>
                            <td className="px-3 py-3 whitespace-nowrap">{r.title}</td>
                            <td className="px-3 py-3 whitespace-nowrap">{r.worker}</td>
                            <td className="px-3 py-3 whitespace-nowrap">{r.zone}</td>
                            <td className="px-3 py-3 whitespace-nowrap">{r.status}</td>
                            <td className="px-3 py-3 whitespace-nowrap">{r.priority}</td>
                            <td className="px-3 py-3 whitespace-nowrap">{r.dueDate}</td>
                            <td className="px-3 py-3 whitespace-nowrap">{r.time}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  )}

                  {view.tab === "workers" && (
                    <table className="min-w-[820px] w-full text-right">
                      <thead className="bg-[#E9E6E0]">
                        <tr className="text-[12px] text-[#2F2F2F]">
                          {view.table.columns.map((c) => (
                            <th key={c} className="px-3 py-3 font-sans font-semibold whitespace-nowrap">
                              {c}
                            </th>
                          ))}
                        </tr>
                      </thead>
                      <tbody>
                        {view.table.rows.map((r) => (
                          <tr key={r.id} className="border-t border-black/5 text-[12px] text-[#2F2F2F]">
                            <td className="px-3 py-3 whitespace-nowrap">{r.name}</td>
                            <td className="px-3 py-3 whitespace-nowrap">{r.presence}</td>
                            <td className="px-3 py-3 whitespace-nowrap">{r.lastSeen}</td>
                            <td className="px-3 py-3 whitespace-nowrap">{r.activeTasks}</td>
                            <td className="px-3 py-3 whitespace-nowrap">{r.doneTasks}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  )}

                  {view.tab === "zones" && (
                    <table className="min-w-[900px] w-full text-right">
                      <thead className="bg-[#E9E6E0]">
                        <tr className="text-[12px] text-[#2F2F2F]">
                          {view.table.columns.map((c) => (
                            <th key={c} className="px-3 py-3 font-sans font-semibold whitespace-nowrap">
                              {c}
                            </th>
                          ))}
                        </tr>
                      </thead>
                      <tbody>
                        {view.table.rows.map((r) => (
                          <tr key={r.id} className="border-t border-black/5 text-[12px] text-[#2F2F2F]">
                            <td className="px-3 py-3 whitespace-nowrap">{r.zone}</td>
                            <td className="px-3 py-3 whitespace-nowrap">{r.total}</td>
                            <td className="px-3 py-3 whitespace-nowrap">{r.done}</td>
                            <td className="px-3 py-3 whitespace-nowrap">{r.inProgress}</td>
                            <td className="px-3 py-3 whitespace-nowrap">{r.delayed}</td>
                            <td className="px-3 py-3 whitespace-nowrap">{r.rate}</td>
                            <td className="px-3 py-3 whitespace-nowrap">{r.updatedAt}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  )}
                </div>
              </Card>
            </div>

            {/* Right */}
            <div className="space-y-5">
              {/* KPIs */}
              <div className="grid grid-cols-2 gap-4">
                {view.kpis.map((k) => (
                  <KpiCard key={k.title} kpi={k} />
                ))}
              </div>

              {/* Chart 2 */}
              <Card>
                <div className="text-right font-sans font-semibold text-[14px] text-[#2F2F2F]">
                  {view.tab === "tasks" ? view.chart2.title : view.tab === "workers" ? view.chart2.title : view.chart2.title}
                </div>

                <div className="mt-4">
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
              </Card>

              <div className="text-right text-[12px] text-[#6B7280]">
                {/* TODO: Backend should return summary KPIs, chart series, and table rows based on applied filters and active tab. */}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

/* ---------- UI ---------- */

function Card({ children }: { children: React.ReactNode }) {
  return (
    <div className="bg-[#F3F1ED] rounded-[16px] shadow-[0_2px_0_rgba(0,0,0,0.08)] border border-black/10 p-4 sm:p-5">
      {children}
    </div>
  );
}

function Tabs({
  value,
  onChange,
  items,
}: {
  value: TabKey;
  onChange: (v: TabKey) => void;
  items: { key: TabKey; label: string }[];
}) {
  return (
    <div className="flex items-center justify-end gap-2 flex-wrap">
      {items.map((it) => {
        const active = value === it.key;
        return (
          <button
            key={it.key}
            type="button"
            onClick={() => onChange(it.key)}
            className={[
              "h-[40px] px-4 rounded-[10px] font-sans font-semibold text-[14px] border",
              active
                ? "bg-[#60778E] text-white border-black/10"
                : "bg-white text-[#2F2F2F] border-black/10 hover:opacity-95",
            ].join(" ")}
          >
            {it.label}
          </button>
        );
      })}
    </div>
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
    <div className="w-full">
      <div className="text-right text-[12px] text-[#2F2F2F] font-sans font-semibold mb-2">
        {label}
      </div>
      <div className="relative">
        <select
          value={value}
          onChange={(e) => onChange(e.target.value)}
          disabled={disabled}
          className={[
            "w-full h-[40px] rounded-[10px] bg-white border border-black/10 px-4 text-right outline-none appearance-none",
            "focus:ring-2 focus:ring-black/10",
            disabled ? "opacity-60 cursor-not-allowed" : "",
          ].join(" ")}
        >
          {options.map((o) => (
            <option key={o.value} value={o.value}>
              {o.label}
            </option>
          ))}
        </select>

        <div className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-[#60778E]">
          <ChevronDown />
        </div>
      </div>

      {helper && <div className="mt-1 text-right text-[11px] text-[#6B7280]">{helper}</div>}
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
      <div className="text-right text-[12px] text-[#2F2F2F] font-sans font-semibold mb-2">
        {label}
      </div>
      <input
        type="date"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        className="w-full h-[40px] rounded-[10px] bg-white border border-black/10 px-4 text-right outline-none focus:ring-2 focus:ring-black/10"
      />
    </div>
  );
}

function KpiCard({ kpi }: { kpi: KPI }) {
  const textColor = kpi.text ?? "#FFFFFF";
  return (
    <div
      className="rounded-[16px] shadow-[0_2px_0_rgba(0,0,0,0.08)] border border-black/10 p-4 flex flex-col gap-3"
      style={{ backgroundColor: kpi.bg }}
    >
      <div className="flex items-start justify-between gap-3" style={{ color: textColor }}>
        <div className="opacity-95">
          <KpiIcon kind={kpi.icon} />
        </div>
        <div className="text-right">
          <div className="text-[12px] font-sans font-semibold opacity-90">{kpi.title}</div>
          <div className="text-[18px] sm:text-[20px] font-sans font-bold mt-1">{kpi.value}</div>
        </div>
      </div>
    </div>
  );
}

function Legend({ items }: { items: LegendItem[] }) {
  return (
    <div className="flex items-center gap-3 flex-wrap justify-end">
      {items.map((it) => (
        <div key={it.label} className="flex items-center gap-2">
          <div className="text-[12px] text-[#2F2F2F]">{it.label}</div>
          <span className="w-3 h-3 rounded-full" style={{ backgroundColor: it.color }} />
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
    <div className="w-full bg-white rounded-[12px] border border-black/10 p-3 overflow-hidden">
      <div className="h-[200px] sm:h-[240px] w-full flex items-end gap-3">
        {points.map((p) => {
          const aH = (p.a / maxY) * 100;
          const bH = (p.b / maxY) * 100;
          const cH = p.c != null ? (p.c / maxY) * 100 : 0;

          return (
            <div key={p.label} className="flex-1 min-w-[18px] h-full flex flex-col justify-end items-center">
              <div className="h-full w-full flex items-end justify-center gap-1.5">
                <div className="w-[10px] sm:w-[12px] rounded-t-[5px]" style={{ height: `${aH}%`, backgroundColor: colors.a }} />
                <div className="w-[10px] sm:w-[12px] rounded-t-[5px]" style={{ height: `${bH}%`, backgroundColor: colors.b }} />
                {p.c != null && (
                  <div className="w-[10px] sm:w-[12px] rounded-t-[5px]" style={{ height: `${cH}%`, backgroundColor: colors.c }} />
                )}
              </div>
              <div className="mt-2 text-[11px] text-[#6B7280]">{p.label}</div>
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
    <div className="w-full bg-white rounded-[12px] border border-black/10 p-4">
      <div className="space-y-3">
        {items.map((it) => {
          const w = Math.round((it.value / max) * 100);
          return (
            <div key={it.label} className="flex items-center gap-3">
              <div className="w-[48px] text-left text-[12px] text-[#2F2F2F]">{it.value}</div>
              <div className="flex-1 h-[10px] bg-[#E5E7EB] rounded-full overflow-hidden">
                <div className="h-full rounded-full" style={{ width: `${w}%`, backgroundColor: it.color }} />
              </div>
              <div className="w-[130px] text-right text-[12px] text-[#2F2F2F] truncate">{it.label}</div>
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
      <div className="shrink-0">
        <Donut size={92} stroke={14} parts={parts} background="#E5E7EB" />
      </div>

      <div className="flex-1 space-y-2">
        {parts.map((p) => (
          <div key={p.label} className="flex items-center justify-between gap-2">
            <div className="text-[12px] text-[#2F2F2F]">{p.value}%</div>
            <div className="flex items-center gap-2">
              <div className="text-[12px] text-[#2F2F2F]">{p.label}</div>
              <span className="w-3 h-3 rounded-full" style={{ backgroundColor: p.color }} />
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
            strokeLinecap="butt"
            strokeDasharray={dashArray}
            strokeDashoffset={dashOffset}
            transform={`rotate(-90 ${size / 2} ${size / 2})`}
          />
        );
      })}
      <circle cx={size / 2} cy={size / 2} r={r - stroke / 2} fill="#F3F1ED" />
    </svg>
  );
}

/* ---------- Icons ---------- */

function KpiIcon({ kind }: { kind: KPI["icon"] }) {
  if (kind === "check") {
    return (
      <svg width="24" height="24" viewBox="0 0 24 24" fill="none" aria-hidden="true">
        <path d="M20 6 9 17l-5-5" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round" />
      </svg>
    );
  }
  if (kind === "users") {
    return (
      <svg width="24" height="24" viewBox="0 0 24 24" fill="none" aria-hidden="true">
        <path d="M17 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
        <path d="M11.5 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8Z" stroke="currentColor" strokeWidth="2" />
        <path d="M22 21v-2a4 4 0 0 0-3-3.87" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
        <path d="M17 3.13a4 4 0 0 1 0 7.75" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
      </svg>
    );
  }
  if (kind === "speed") {
    return (
      <svg width="24" height="24" viewBox="0 0 24 24" fill="none" aria-hidden="true">
        <path d="M20 13a8 8 0 1 1-16 0" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
        <path d="M12 13 16 9" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
        <path d="M12 3v2" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
      </svg>
    );
  }
  return (
    <svg width="24" height="24" viewBox="0 0 24 24" fill="none" aria-hidden="true">
      <path d="M12 9v4" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
      <path d="M12 17h.01" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" />
      <path d="M10.3 4.5h3.4L21 20H3L10.3 4.5Z" stroke="currentColor" strokeWidth="2" strokeLinejoin="round" />
    </svg>
  );
}

function ChevronDown() {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" aria-hidden="true">
      <path d="M6 9l6 6 6-6" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}

/* ---------- Export Helpers ---------- */


/* ---------- API to ViewModel Builders ---------- */

const palette = {
  blue: "#60778E",
  green: "#8FA36A",
  red: "#C86E5D",
  gray: "#6B7280",
  light: "#E5E7EB",
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

  // Map tasks to table rows
  const statusMap: Record<string, string> = {
    Pending: "معلقة",
    InProgress: "نشطة",
    Completed: "مكتملة",
    Cancelled: "ملغاة",
  };
  const priorityMap: Record<string, string> = {
    Low: "منخفضة",
    Medium: "متوسطة",
    High: "عالية",
    Urgent: "عاجلة",
  };

  const rows: TaskRow[] = data.tasks.map((t) => ({
    id: t.id.toString(),
    title: t.title,
    worker: t.worker,
    zone: t.zone,
    status: statusMap[t.status] || t.status,
    priority: priorityMap[t.priority] || t.priority,
    dueDate: t.dueDate ? new Date(t.dueDate).toLocaleDateString("ar-EG") : "—",
    time: t.createdAt ? new Date(t.createdAt).toLocaleTimeString("ar-EG", { hour: "2-digit", minute: "2-digit" }) : "—",
  }));

  return {
    tab: "tasks",
    title: "تقرير المهام",
    lastUpdated,
    filtersNote: "سيتم تنزيل التقرير حسب الفلاتر الحالية",
    kpis: [
      { title: "إجمالي المهام", value: data.total.toString(), icon: "check", bg: palette.blue },
      { title: "معدل الإنجاز", value: `${completedPct}%`, icon: "speed", bg: palette.gray },
      { title: "مهام معلقة", value: data.pending.toString(), icon: "alert", bg: palette.light, text: "#2F2F2F" },
      { title: "العمال النشطين", value: `${data.activeWorkers} / ${data.totalWorkers}`, icon: "users", bg: palette.green },
    ],
    chart1: {
      title: "تقرير المهام حسب الفترة",
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
      title: "تقرير المهام التفصيلي:",
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
      { title: "إجمالي العمال", value: data.totalWorkers.toString(), icon: "users", bg: palette.green },
      { title: "الحضور", value: data.checkedIn.toString(), icon: "check", bg: palette.blue },
      { title: "الغياب", value: data.absent.toString(), icon: "alert", bg: palette.gray },
      { title: "الالتزام", value: `${data.compliancePercent}%`, icon: "speed", bg: palette.light, text: "#2F2F2F" },
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
      title: "أعلى 5 عمّال من حيث الضغط (مهام فعالة)",
      items: topWorkload.length > 0 ? topWorkload : [{ label: "—", value: 0, color: palette.blue }],
    },
    table: {
      title: "تقرير العمال التفصيلي:",
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
      { title: "عدد المناطق", value: data.totalZones.toString(), icon: "users", bg: palette.blue },
      { title: "أعلى ضغط", value: data.highestPressureZone || "—", icon: "alert", bg: palette.gray },
      { title: "معدل الإنجاز", value: `${completedPct}%`, icon: "speed", bg: palette.light, text: "#2F2F2F" },
      { title: "بلاغات/تأخير", value: data.totalDelayed.toString(), icon: "check", bg: palette.green },
    ],
    chart1: {
      title: "أكثر 5 مناطق من حيث عدد المهام خلال الفترة",
      items: topZones.length > 0 ? topZones : [{ label: "—", value: 0, color: palette.blue }],
    },
    chart2: {
      title: "توزيع حالة المناطق",
      parts: [
        { label: "منجزة", value: completedPct, color: palette.green },
        { label: "قيد التنفيذ", value: inProgressPct, color: palette.blue },
        { label: "متأخرة", value: delayedPct, color: palette.red },
      ],
    },
    table: {
      title: "تقرير المناطق التفصيلي:",
      columns: ["المنطقة", "الإجمالي", "منجزة", "قيد التنفيذ", "متأخرة", "الإنجاز", "آخر تحديث"],
      rows,
    },
  };
}

/* ---------- Mock Builder ---------- */

function buildMockView(tab: TabKey, f: FiltersDraft, lastUpdated: string): ViewModel {
  const palette = {
    blue: "#60778E",
    green: "#8FA36A",
    red: "#C86E5D",
    gray: "#6B7280",
    light: "#E5E7EB",
  };

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
        { title: "إجمالي المهام", value: "33", icon: "check", bg: palette.blue },
        { title: "معدل الإنجاز", value: "78%", icon: "speed", bg: palette.gray },
        { title: "مهام معلقة", value: "3", icon: "alert", bg: palette.light, text: "#2F2F2F" },
        { title: "العمال النشطين", value: "28 / 35", icon: "users", bg: palette.green },
      ],
      chart1: {
        title: "تقرير المهام حسب الفترة",
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
        title: "تقرير المهام التفصيلي:",
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
        { title: "إجمالي العمال", value: "35", icon: "users", bg: palette.green },
        { title: "الحضور", value: "28", icon: "check", bg: palette.blue },
        { title: "الغياب", value: "7", icon: "alert", bg: palette.gray },
        { title: "الالتزام", value: "80%", icon: "speed", bg: palette.light, text: "#2F2F2F" },
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
        title: "أعلى 5 عمّال من حيث الضغط (مهام فعالة)",
        items: topWorkload,
      },
      table: {
        title: "تقرير العمال التفصيلي:",
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
      { title: "عدد المناطق", value: "12", icon: "users", bg: palette.blue },
      { title: "أعلى ضغط", value: "المنطقة 4", icon: "alert", bg: palette.gray },
      { title: "معدل الإنجاز", value: "61%", icon: "speed", bg: palette.light, text: "#2F2F2F" },
      { title: "بلاغات/تأخير", value: "7", icon: "check", bg: palette.green },
    ],
    chart1: {
      title: "أكثر 5 مناطق من حيث عدد المهام خلال الفترة",
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
      title: "تقرير المناطق التفصيلي:",
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
