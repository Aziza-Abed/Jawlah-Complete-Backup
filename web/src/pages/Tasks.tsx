import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { usePageTitle } from "../hooks/usePageTitle";
import { getTasks } from "../api/tasks";
import type { TaskResponse } from "../types/task";
import { Plus, RefreshCw, ChevronLeft } from "lucide-react";
import MiniStat from "../components/common/MiniStat";
import FilterChip from "../components/common/FilterChip";

type TaskStatus = "Pending" | "InProgress" | "UnderReview" | "Completed" | "Rejected" | "Cancelled";
type TaskPriority = "Low" | "Medium" | "High" | "Urgent";

type TaskRow = {
  id: string;
  title: string;
  workerName: string;
  zoneName: string;
  status: TaskStatus;
  priority: TaskPriority;
  dueDate?: string;
  updatedAt: string;
  progressPercentage: number;
  isTeamTask: boolean;
  teamName?: string;
};

type FilterKey = "all" | "pending" | "inprogress" | "underreview" | "completed" | "rejected";

const mapStatus = (backendStatus: string): TaskStatus => {
  switch (backendStatus) {
    case "Pending":
    case "Created":
    case "Assigned":
      return "Pending";
    case "InProgress":
    case "Accepted":
      return "InProgress";
    case "UnderReview":
    case "Submitted":
      return "UnderReview";
    case "Completed":
    case "Synced":
      return "Completed";
    case "Rejected":
      return "Rejected";
    case "Cancelled":
      return "Cancelled";
    default:
      return "Pending";
  }
};

const mapPriority = (backendPriority: string): TaskPriority => {
  return backendPriority as TaskPriority;
};

const mapTaskToRow = (task: TaskResponse): TaskRow => {
  const updatedTime = task.completedAt || task.startedAt || task.createdAt;
  const date = new Date(updatedTime);
  const hours = date.getHours().toString().padStart(2, "0");
  const minutes = date.getMinutes().toString().padStart(2, "0");

  let progress = task.progressPercentage || 0;
  if (task.status === "UnderReview" || task.status === "Completed") {
    progress = 100;
  } else if (task.status === "InProgress" && progress === 0) {
    progress = 50;
  }

  return {
    id: task.taskId.toString(),
    title: task.title,
    workerName: task.isTeamTask ? (task.teamName || "فريق") : (task.assignedToUserName || "غير محدد"),
    zoneName: task.zoneName || "غير محدد",
    status: mapStatus(task.status),
    priority: mapPriority(task.priority),
    dueDate: task.dueDate?.split("T")[0],
    updatedAt: `${hours}:${minutes}`,
    progressPercentage: progress,
    isTeamTask: task.isTeamTask,
    teamName: task.teamName,
  };
};

export default function TasksList() {
  usePageTitle("المهام");
  const navigate = useNavigate();

  const [items, setItems] = useState<TaskRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [filter, setFilter] = useState<FilterKey>("all");
  const [q, setQ] = useState("");

  const fetchTasks = async () => {
    try {
      setLoading(true);
      setError("");
      const tasks = await getTasks();
      const rows = tasks.map(mapTaskToRow);
      setItems(rows);
    } catch (err) {
      console.error("Failed to fetch tasks:", err);
      setError("فشل تحميل المهام");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchTasks();
  }, []);

  const stats = {
    total: items.length,
    underReview: items.filter((x) => x.status === "UnderReview").length,
    inProgress: items.filter((x) => x.status === "InProgress").length,
    pending: items.filter((x) => x.status === "Pending").length,
  };

  const query = q.trim().toLowerCase();
  const filtered = items.filter((x) => {
    if (filter !== "all") {
      const statusMap: Record<FilterKey, TaskStatus | null> = {
        all: null, pending: "Pending", inprogress: "InProgress",
        underreview: "UnderReview", completed: "Completed", rejected: "Rejected"
      };
      if (statusMap[filter] && x.status !== statusMap[filter]) return false;
    }
    if (query) {
      const hay = `${x.id} ${x.title} ${x.workerName} ${x.zoneName}`.toLowerCase();
      if (!hay.includes(query)) return false;
    }
    return true;
  });

  if (loading) {
    return (
      <div className="h-full w-full bg-[#F3F1ED] grid place-items-center">
        <div className="text-[#2F2F2F] font-sans font-semibold">جاري التحميل...</div>
      </div>
    );
  }

  return (
    <div className="h-full w-full bg-[#F3F1ED] overflow-auto">
      <div className="p-4 sm:p-6 md:p-8">
        <div className="max-w-[1100px] mx-auto">
          {/* Header */}
          <div className="flex items-center justify-between gap-3">
            <h1 className="text-right font-black text-[28px] text-[#2F2F2F] tracking-tight">
              المهام الميدانية
            </h1>
            <div className="flex items-center gap-2">
              <button
                type="button"
                onClick={fetchTasks}
                className="h-[38px] px-3 rounded-[10px] bg-white border border-black/10 text-[#2F2F2F] hover:bg-black/5"
              >
                <RefreshCw size={16} />
              </button>
              <button
                type="button"
                onClick={() => navigate("/tasks/new")}
                className="h-[38px] px-4 rounded-[10px] bg-[#7895B2] text-white font-sans font-semibold text-[13px] shadow-[0_2px_0_rgba(0,0,0,0.15)] hover:opacity-95 flex items-center gap-2"
              >
                <Plus size={16} />
                تعيين مهمة
              </button>
            </div>
          </div>

          {error && (
            <div className="mt-4 p-3 bg-red-100 text-red-700 rounded-[10px] text-right">
              {error}
            </div>
          )}

          {/* Mini Stats Grid */}
          <div className="mt-4 grid grid-cols-1 sm:grid-cols-4 gap-3">
            <MiniStat title="إجمالي المهام" value={String(stats.total)} />
            <MiniStat title="بانتظار اعتماد" value={String(stats.underReview)} />
            <MiniStat title="قيد التنفيذ" value={String(stats.inProgress)} />
            <MiniStat title="مفتوحة" value={String(stats.pending)} />
          </div>

          {/* Filter Bar */}
          <div className="mt-4 bg-[#F3F1ED] rounded-[14px] border border-black/10 shadow-[0_2px_0_rgba(0,0,0,0.08)] p-3 sm:p-4">
            <div className="flex flex-col lg:flex-row lg:items-center gap-3">
              <div className="flex-1">
                <input
                  value={q}
                  onChange={(e) => setQ(e.target.value)}
                  placeholder="بحث بالرقم، العنوان، العامل، أو المنطقة..."
                  className="w-full h-[44px] rounded-[12px] bg-white border border-black/10 px-4 text-right outline-none focus:ring-2 focus:ring-black/10 text-[13px]"
                />
              </div>

              <div className="flex items-center gap-2 flex-wrap justify-end">
                <FilterChip active={filter === "all"} onClick={() => setFilter("all")} label="الكل" count={stats.total} />
                <FilterChip active={filter === "pending"} onClick={() => setFilter("pending")} label="مفتوحة" count={stats.pending} />
                <FilterChip active={filter === "inprogress"} onClick={() => setFilter("inprogress")} label="قيد التنفيذ" count={stats.inProgress} />
                <FilterChip active={filter === "underreview"} onClick={() => setFilter("underreview")} label="بانتظار اعتماد" count={stats.underReview} />
              </div>
            </div>
          </div>

          {/* Tasks List (Unified Card Style) */}
          <div className="mt-4 space-y-3">
            {filtered.length === 0 ? (
              <div className="bg-white rounded-[14px] border border-black/10 p-5 text-right text-[#6B7280]">
                لا يوجد مهام تطابق البحث.
              </div>
            ) : (
              filtered.map((t) => (
                <TaskCard key={t.id} task={t} onClick={() => navigate(`/tasks/${t.id}`)} />
              ))
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

function TaskCard({ task, onClick }: { task: TaskRow; onClick: () => void }) {
  return (
    <button
      onClick={onClick}
      className="w-full bg-[#F3F1ED] rounded-[14px] border border-black/10 shadow-[0_2px_0_rgba(0,0,0,0.06)] p-4 sm:p-5 hover:bg-white/60 transition text-right flex flex-col sm:flex-row sm:items-center justify-between gap-4"
    >
      <div className="flex items-center gap-4 flex-1">
        <div className="w-12 h-12 rounded-[12px] bg-white border border-black/10 flex items-center justify-center shrink-0 text-[16px] font-bold text-[#7895B2] shadow-sm">
          {task.id}
        </div>
        <div className="flex-1 flex items-center justify-start gap-4 text-right overflow-hidden">
           <div className="text-[15px] font-sans font-semibold text-[#2F2F2F] truncate">
             {task.title}
           </div>
           <div className="text-[12px] text-[#6B7280] hidden md:block truncate">
             • {task.workerName} • {task.zoneName}
           </div>
        </div>
      </div>

      <div className="flex items-center gap-4 flex-wrap justify-end">
        <div className="grid grid-cols-2 gap-2">
           <MetaSmallPill label="الحالة" value={formatStatusLabel(task.status)} color={getStatusColor(task.status)} />
           <MetaSmallPill label="التقدم" value={`${task.progressPercentage}%`} />
        </div>
         <ChevronLeft size={20} className="text-[#7895B2] hidden sm:block" strokeWidth={3} />
      </div>
    </button>
  );
}

function MetaSmallPill({ label, value, color }: { label: string; value: string; color?: string }) {
  return (
    <div className="bg-white/80 rounded-[10px] border border-black/5 px-3 py-1.5 min-w-[90px] text-right">
      <div className="text-[10px] text-[#6B7280] font-semibold">{label}</div>
      <div className="text-[13px] font-bold" style={{ color: color || "#2F2F2F" }}>{value}</div>
    </div>
  );
}

function formatStatusLabel(s: TaskStatus) {
  const map: Record<TaskStatus, string> = {
    Pending: "مفتوحة",
    InProgress: "قيد التنفيذ",
    UnderReview: "بانتظار اعتماد",
    Completed: "مكتملة",
    Rejected: "مرفوضة",
    Cancelled: "ملغاة",
  };
  return map[s];
}

function getStatusColor(s: TaskStatus) {
  const map: Record<TaskStatus, string> = {
    Pending: "#6B7280",
    InProgress: "#7895B2",
    UnderReview: "#C86E5D",
    Completed: "#8FA36A",
    Rejected: "#C86E5D",
    Cancelled: "#6B7280",
  };
  return map[s];
}
