import type {
  TasksReportData,
  WorkersReportData,
  ZonesReportData,
} from "../../types/report";
import type {
  FiltersDraft,
  TabKey,
  ViewModel,
  SeriesPoint,
  TaskRow,
  WorkerRow,
  ZoneRow,
  PeriodPreset,
} from "./types";
import { REPORT_TOP_ZONES_COUNT } from "../../constants/appConstants";

// Translation helpers for English -> Arabic
function translateStatus(status: string): string {
  const map: Record<string, string> = {
    'Pending': 'معلقة',
    'InProgress': 'قيد التنفيذ',
    'UnderReview': 'قيد المراجعة',
    'Completed': 'مكتملة',
    'Rejected': 'مرفوضة',
    'Cancelled': 'ملغاة',
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

export const palette = {
  blue: "#60A5FA", // brighter blue for dark mode
  green: "#34D399", // emerald
  red: "#F87171", // soft red
  gray: "#94A3B8", // slate 400
  light: "#F1F5F9",
  purple: "#A78BFA"
};

export function buildViewFromTasksApi(data: TasksReportData, f: FiltersDraft, lastUpdated: string): ViewModel {
  const total = data.total || 1;
  const completedPct = Math.round((data.completed / total) * 100);
  const inProgressPct = Math.round((data.inProgress / total) * 100);
  const underReviewPct = Math.round(((data.underReview ?? 0) / total) * 100);
  const pendingPct = Math.max(0, 100 - completedPct - inProgressPct - underReviewPct);

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
    dueDate: t.dueDate ? new Date(t.dueDate).toLocaleDateString("ar-EG") : "\u2014",
    time: t.createdAt ? new Date(t.createdAt).toLocaleTimeString("ar-EG", { hour: "2-digit", minute: "2-digit" }) : "\u2014",
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
        { label: "قيد المراجعة", value: underReviewPct, color: palette.purple },
        { label: "معلقة", value: pendingPct, color: palette.red },
      ].filter(p => p.value > 0),
    },
    table: {
      title: "جدول المهام التفصيلي",
      columns: ["#", "المهمة", "العامل", "المنطقة", "الحالة", "الأولوية", "الموعد", "الوقت"],
      rows,
    },
  };
}

export function buildViewFromWorkersApi(data: WorkersReportData, f: FiltersDraft, lastUpdated: string): ViewModel {
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
    lastSeen: w.lastCheckIn ? new Date(w.lastCheckIn).toLocaleTimeString("ar-EG", { hour: "2-digit", minute: "2-digit" }) : "\u2014",
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
      items: topWorkload.length > 0 ? topWorkload : [{ label: "\u2014", value: 0, color: palette.blue }],
    },
    table: {
      title: "سجل دوام وأداء العمال",
      columns: ["العامل", "الحالة", "آخر ظهور", "مهام فعّالة", "مهام منجزة"],
      rows,
    },
  };
}

export function buildViewFromZonesApi(data: ZonesReportData, _f: FiltersDraft, lastUpdated: string): ViewModel {
  const total = data.totalTasks || 1;
  const completedPct = Math.round((data.totalCompleted / total) * 100);
  const inProgressPct = Math.round((data.totalInProgress / total) * 100);
  const delayedPct = Math.round((data.totalDelayed / total) * 100);

  // Top zones by task count
  const topZones = data.zones
    .slice()
    .sort((a, b) => b.total - a.total)
    .slice(0, REPORT_TOP_ZONES_COUNT)
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
    updatedAt: z.lastUpdate ? new Date(z.lastUpdate).toLocaleTimeString("ar-EG", { hour: "2-digit", minute: "2-digit" }) : "\u2014",
  }));

  return {
    tab: "zones",
    title: "تقرير المناطق",
    lastUpdated,
    filtersNote: "سيتم تنزيل التقرير حسب الفلاتر الحالية",
    kpis: [
      { title: "عدد المناطق", value: data.totalZones.toString(), icon: "users", color: palette.blue },
      { title: "أعلى منطقة ضغطاً", value: data.highestPressureZone || "\u2014", icon: "alert", color: palette.red },
      { title: "معدل الإنجاز العام", value: `${completedPct}%`, icon: "speed", color: palette.green },
      { title: "بلاغات/تأخير", value: data.totalDelayed.toString(), icon: "check", color: palette.purple },
    ],
    chart1: {
      title: "المناطق الأكثر نشاطاً",
      items: topZones.length > 0 ? topZones : [{ label: "\u2014", value: 0, color: palette.blue }],
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

/* ---------- Empty View Builder ---------- */

export function buildEmptyView(tab: TabKey, f: FiltersDraft, lastUpdated: string): ViewModel {
  const labels = buildLabelsForPeriod(f.period);
  const emptyPoints = labels.map((label) => ({ label, a: 0, b: 0, c: 0 }));

  if (tab === "tasks") {
    return {
      tab: "tasks",
      title: "تقرير المهام",
      lastUpdated,
      filtersNote: "لا توجد بيانات للفترة المحددة",
      kpis: [
        { title: "إجمالي المهام", value: "0", icon: "check", color: palette.blue },
        { title: "معدل الإنجاز", value: "0%", icon: "speed", color: palette.green },
        { title: "مهام معلقة", value: "0", icon: "alert", color: palette.red },
        { title: "العمال النشطين", value: "0 / 0", icon: "users", color: palette.purple },
      ],
      chart1: {
        title: "أداء المهام حسب الفترة",
        legend: [
          { label: "مكتملة", color: palette.green },
          { label: "نشطة", color: palette.blue },
          { label: "معلقة", color: palette.red },
        ],
        points: emptyPoints,
      },
      chart2: {
        title: "توزيع حالة المهام",
        parts: [
          { label: "مكتملة", value: 0, color: palette.green },
          { label: "نشطة", value: 0, color: palette.blue },
          { label: "معلقة", value: 0, color: palette.red },
        ],
      },
      table: {
        title: "جدول المهام التفصيلي",
        columns: ["#", "المهمة", "العامل", "المنطقة", "الحالة", "الأولوية", "الموعد", "الوقت"],
        rows: [],
      },
    };
  }

  if (tab === "workers") {
    return {
      tab: "workers",
      title: "تقرير العمال",
      lastUpdated,
      filtersNote: "لا توجد بيانات للفترة المحددة",
      kpis: [
        { title: "إجمالي العمال", value: "0", icon: "users", color: palette.blue },
        { title: "الحضور", value: "0", icon: "check", color: palette.green },
        { title: "الغياب", value: "0", icon: "alert", color: palette.red },
        { title: "الالتزام", value: "0%", icon: "speed", color: palette.purple },
      ],
      chart1: {
        title: "حضور / غياب العمال حسب الفترة",
        legend: [
          { label: "حضور", color: palette.green },
          { label: "غياب", color: palette.red },
        ],
        points: emptyPoints.map((p) => ({ label: p.label, a: 0, b: 0 })),
      },
      chart2: {
        title: "أعلى 5 عمّال إنتاجية",
        items: [{ label: "لا توجد بيانات", value: 0, color: palette.gray }],
      },
      table: {
        title: "سجل دوام وأداء العمال",
        columns: ["العامل", "الحالة", "آخر ظهور", "مهام فعّالة", "مهام منجزة"],
        rows: [],
      },
    };
  }

  return {
    tab: "zones",
    title: "تقرير المناطق",
    lastUpdated,
    filtersNote: "لا توجد بيانات للفترة المحددة",
    kpis: [
      { title: "عدد المناطق", value: "0", icon: "users", color: palette.blue },
      { title: "أعلى ضغط", value: "\u2014", icon: "alert", color: palette.gray },
      { title: "معدل الإنجاز", value: "0%", icon: "speed", color: palette.green },
      { title: "بلاغات/تأخير", value: "0", icon: "check", color: palette.purple },
    ],
    chart1: {
      title: "المناطق الأكثر نشاطاً",
      items: [{ label: "لا توجد بيانات", value: 0, color: palette.gray }],
    },
    chart2: {
      title: "توزيع حالة المناطق",
      parts: [
        { label: "منجزة", value: 0, color: palette.green },
        { label: "قيد التنفيذ", value: 0, color: palette.blue },
        { label: "متأخرة", value: 0, color: palette.red },
      ],
    },
    table: {
      title: "أداء المناطق التفصيلي",
      columns: ["المنطقة", "الإجمالي", "منجزة", "قيد التنفيذ", "متأخرة", "الإنجاز", "آخر تحديث"],
      rows: [],
    },
  };
}

function buildLabelsForPeriod(period: PeriodPreset): string[] {
  if (period === "daily") return ["1", "2", "3", "4", "5", "6", "7", "8"];
  if (period === "weekly") return ["س", "ح", "ن", "ث", "ر", "خ", "ج"];
  if (period === "monthly") {
    // 4 weeks of a month
    return ["أسبوع 1", "أسبوع 2", "أسبوع 3", "أسبوع 4"];
  }
  if (period === "yearly") return ["1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12"];
  if (period === "custom") return ["1", "2", "3", "4", "5", "6", "7", "8", "9", "10"];
  return ["1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12"];
}
