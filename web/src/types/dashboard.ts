// Dashboard types matching backend response

export type DashboardOverview = {
  workers: {
    total: number;
    checkedIn: number;
    checkedOut: number;
    notCheckedIn: number;
  };
  tasks: {
    createdToday: number;
    pending: number;
    inProgress: number;
    completedToday: number;
  };
  issues: {
    reportedToday: number;
    unresolved: number;
  };
  date: string;
};

export type WorkerStatus = {
  userId: number;
  fullName: string;
  employeeId: string;
  status: string;
  checkInTime?: string;
  zoneName?: string;
  todayTasksCount: number;
  pendingTasksCount: number;
  completedTasksCount: number;
};
