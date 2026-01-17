// Report types matching backend DTOs

export type ReportPeriod = "daily" | "weekly" | "monthly" | "yearly" | "custom";

export type ReportFilters = {
  period: ReportPeriod;
  status?: string;
  startDate?: string;
  endDate?: string;
};

// Tasks Report
export type TasksReportData = {
  total: number;
  completed: number;
  inProgress: number;
  pending: number;
  cancelled: number;
  activeWorkers: number;
  totalWorkers: number;
  byPeriod: TasksByPeriod[];
  tasks: TaskReportItem[];
};

export type TasksByPeriod = {
  label: string;
  completed: number;
  inProgress: number;
  pending: number;
};

export type TaskReportItem = {
  id: number;
  title: string;
  worker: string;
  zone: string;
  status: string;
  priority: string;
  dueDate?: string;
  createdAt: string;
};

// Workers Report
export type WorkersReportData = {
  totalWorkers: number;
  checkedIn: number;
  absent: number;
  compliancePercent: number;
  byPeriod: AttendanceByPeriod[];
  topWorkload: WorkerWorkload[];
  workers: WorkerReportItem[];
};

export type AttendanceByPeriod = {
  label: string;
  present: number;
  absent: number;
};

export type WorkerWorkload = {
  userId: number;
  name: string;
  activeTasks: number;
};

export type WorkerReportItem = {
  id: number;
  name: string;
  isPresent: boolean;
  lastCheckIn?: string;
  activeTasks: number;
  completedTasks: number;
};

// Zones Report
export type ZonesReportData = {
  totalZones: number;
  totalTasks: number;
  totalCompleted: number;
  totalInProgress: number;
  totalDelayed: number;
  highestPressureZone: string;
  zones: ZoneReportItem[];
};

export type ZoneReportItem = {
  id: number;
  name: string;
  total: number;
  completed: number;
  inProgress: number;
  delayed: number;
  lastUpdate?: string;
};
