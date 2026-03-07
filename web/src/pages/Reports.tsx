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
    CheckCircle,
    AlertCircle,
    Users,
    Activity,
    BarChart3,
    PieChart,
    Printer
} from "lucide-react";
import {
  ReportFilters,
  ReportKPICards,
  ReportTasksTable,
  ReportWorkersTable,
  ReportZonesTable,
  Legend,
  BarChartGrouped,
  HorizontalBars,
  DonutWithLegend,
  buildViewFromTasksApi,
  buildViewFromWorkersApi,
  buildViewFromZonesApi,
  buildEmptyView,
} from "../components/reports";
import type { TabKey, FiltersDraft, ViewModel } from "../components/reports";

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
    // Use API data - show empty state if no data
    if (tab === "tasks" && tasksData) {
      return buildViewFromTasksApi(tasksData, applied, lastUpdated);
    }
    if (tab === "workers" && workersData) {
      return buildViewFromWorkersApi(workersData, applied, lastUpdated);
    }
    if (tab === "zones" && zonesData) {
      return buildViewFromZonesApi(zonesData, applied, lastUpdated);
    }
    // Return empty view structure when no data
    return buildEmptyView(tab, applied, lastUpdated);
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
      } else if (tab === "workers") {
        await downloadAttendanceReport(format, filters);
      } else {
        setError("تصدير تقرير المناطق غير متاح حالياً");
        return;
      }
    } catch (error: any) {
      console.error("Export failed:", error);
      const msg = error?.message || "فشل تصدير التقرير. يرجى المحاولة مرة أخرى.";
      setError(msg);
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
                    <h1 className="font-black text-[28px] text-[#2F2F2F] tracking-tight">
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
                <span className="font-semibold flex-1">{error}</span>
                <button onClick={() => setError("")} className="text-[#C86E5D] hover:opacity-70 text-sm font-bold shrink-0">✕</button>
            </div>
        )}

        {/* Tabs */}
        <div className="flex justify-start">
          <div className="bg-white/50 backdrop-blur-sm p-1.5 rounded-[20px] border border-black/5 shadow-sm inline-flex gap-1">
            {[
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

        {/* KPIs + Donut side by side */}
        <div className="grid grid-cols-1 xl:grid-cols-[1fr_1fr] gap-6">
          {/* Right - KPIs (2x2) */}
          <ReportKPICards kpis={view.kpis} />

          {/* Left - Donut Chart */}
          <div className="bg-white rounded-[16px] p-5 shadow-[0_4px_20px_rgba(0,0,0,0.04)]">
            <div className="flex items-center gap-2 mb-4">
              <div className="p-2 bg-[#7895B2]/10 rounded-lg text-[#7895B2]">
                <PieChart size={20} />
              </div>
              <h3 className="text-lg font-bold text-[#2F2F2F]">
                {view.chart2.title}
              </h3>
            </div>
            <div>
              {view.tab === "tasks" && <DonutWithLegend parts={view.chart2.parts} />}
              {view.tab === "workers" && <HorizontalBars items={view.chart2.items} />}
              {view.tab === "zones" && <DonutWithLegend parts={view.chart2.parts} />}
            </div>
          </div>
        </div>

        {/* Filters */}
        <ReportFilters
          draft={draft}
          setDraft={setDraft}
          onApply={onApply}
          onReset={onReset}
          showStatusFilter={showStatusFilter}
        />

        {/* Bar Chart - full width */}
        <div className="bg-white rounded-[16px] p-5 shadow-[0_4px_20px_rgba(0,0,0,0.04)]">
          <div className="flex items-center justify-between gap-3 mb-6">
            <div className="flex items-center gap-2">
              <div className="p-2 bg-[#7895B2]/10 rounded-lg text-[#7895B2]">
                <BarChart3 size={20} />
              </div>
              <h3 className="text-xl font-bold text-[#2F2F2F]">
                {view.chart1.title}
              </h3>
            </div>
            {(view.tab === "tasks" || view.tab === "workers") && <Legend items={view.chart1.legend} />}
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

        {/* Table + Export - full width */}
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
            {view.tab === "tasks" && <ReportTasksTable columns={view.table.columns} rows={view.table.rows} />}
            {view.tab === "workers" && <ReportWorkersTable columns={view.table.columns} rows={view.table.rows} />}
            {view.tab === "zones" && <ReportZonesTable columns={view.table.columns} rows={view.table.rows} />}
          </div>
        </div>

        <div className="text-right text-xs text-[#6B7280] px-2">
          {view.filtersNote}
        </div>
      </div>
      </div>
    </div>
  );
}
