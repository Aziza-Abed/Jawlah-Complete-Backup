export type TabKey = "tasks" | "workers" | "zones";
export type PeriodPreset = "daily" | "weekly" | "monthly" | "yearly" | "custom";
export type TaskStatus = "all" | "pending" | "in_progress" | "underreview" | "completed";

export type FiltersDraft = {
  period: PeriodPreset;
  status: TaskStatus;
  from: string;
  to: string;
};

export type KPI = {
  title: string;
  value: string;
  icon: "check" | "users" | "speed" | "alert";
  color: string;
};

export type SeriesPoint = { label: string; a: number; b: number; c?: number };
export type LegendItem = { label: string; color: string };

export type TaskRow = {
  id: string;
  title: string;
  worker: string;
  zone: string;
  status: string;
  priority: string;
  dueDate: string;
  time: string;
};

export type WorkerRow = {
  id: string;
  name: string;
  presence: string;
  lastSeen: string;
  activeTasks: number;
  doneTasks: number;
};

export type ZoneRow = {
  id: string;
  zone: string;
  total: number;
  done: number;
  inProgress: number;
  delayed: number;
  rate: string;
  updatedAt: string;
};

export type DonutPart = { label: string; value: number; color: string };

export type ViewModel =
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
