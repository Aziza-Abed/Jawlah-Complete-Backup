import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { getTasks } from "../api/tasks";
import type { TaskResponse } from "../types/task";

type TaskStatus = "Open" | "InProgress" | "Submitted" | "Approved" | "Rejected";
type TaskPriority = "Low" | "Medium" | "High";

type TaskRow = {
  id: string;
  title: string;
  workerName: string;
  zoneName: string;
  status: TaskStatus;
  priority: TaskPriority;
  dueDate?: string; // YYYY-MM-DD
  updatedAt: string; // HH:MM or ISO later
};

type FilterKey = "all" | "open" | "inprogress" | "submitted" | "approved" | "rejected";

// Map backend status to frontend status
const mapStatus = (backendStatus: string): TaskStatus => {
  switch (backendStatus) {
    case "Pending": return "Open";
    case "InProgress": return "InProgress";
    case "Completed": return "Submitted";
    case "Approved": return "Approved";
    case "Rejected": return "Rejected";
    default: return "Open";
  }
};

// Map backend priority to frontend priority
const mapPriority = (backendPriority: string): TaskPriority => {
  if (backendPriority === "Urgent") return "High";
  return backendPriority as TaskPriority;
};

// Convert backend TaskResponse to frontend TaskRow
const mapTaskToRow = (task: TaskResponse): TaskRow => {
  const updatedTime = task.completedAt || task.startedAt || task.createdAt;
  const date = new Date(updatedTime);
  const hours = date.getHours().toString().padStart(2, "0");
  const minutes = date.getMinutes().toString().padStart(2, "0");

  return {
    id: task.taskId.toString(),
    title: task.title,
    workerName: task.assignedToUserName || "غير محدد",
    zoneName: task.zoneName || "غير محدد",
    status: mapStatus(task.status),
    priority: mapPriority(task.priority),
    dueDate: task.dueDate?.split("T")[0],
    updatedAt: `${hours}:${minutes}`,
  };
};

export default function TasksList() {
  const navigate = useNavigate();

  const [items, setItems] = useState<TaskRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [filter, setFilter] = useState<FilterKey>("all");
  const [q, setQ] = useState("");

  // Fetch tasks from backend
  useEffect(() => {
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
    fetchTasks();
  }, []);

  const stats = useMemo(() => {
    const total = items.length;
    const submitted = items.filter((x) => x.status === "Submitted").length;
    const inProgress = items.filter((x) => x.status === "InProgress").length;
    const open = items.filter((x) => x.status === "Open").length;
    return { total, submitted, inProgress, open };
  }, [items]);

  const filtered = useMemo(() => {
    const byFilter = (x: TaskRow) => {
      if (filter === "all") return true;
      if (filter === "open") return x.status === "Open";
      if (filter === "inprogress") return x.status === "InProgress";
      if (filter === "submitted") return x.status === "Submitted";
      if (filter === "approved") return x.status === "Approved";
      return x.status === "Rejected";
    };

    const query = q.trim().toLowerCase();
    const bySearch = (x: TaskRow) => {
      if (!query) return true;
      const hay = `${x.id} ${x.title} ${x.workerName} ${x.zoneName}`.toLowerCase();
      return hay.includes(query);
    };

    return items.filter((x) => byFilter(x) && bySearch(x));
  }, [items, filter, q]);

  const exportCsv = () => {
    const headers = ["id", "title", "workerName", "zoneName", "status", "priority", "dueDate", "updatedAt"];
    const esc = (v: unknown) => `"${String(v ?? "").replaceAll('"', '""')}"`;
    const lines = [
      headers.join(","),
      ...filtered.map((r) => headers.map((h) => esc((r as any)[h])).join(",")),
    ];
    downloadTextFile(lines.join("\n"), "tasks-report.csv", "text/csv;charset=utf-8");
  };

  const openTask = (id: string) => {
    navigate(`/tasks/${id}`);
  };

  return (
    <div className="h-full w-full bg-[#D9D9D9] overflow-auto">
      <div className="p-4 sm:p-6 md:p-8">
        <div className="max-w-[1100px] mx-auto">
          <div className="flex items-center justify-between gap-3">
            <h1 className="text-right font-sans font-semibold text-[20px] sm:text-[22px] text-[#2F2F2F]">
              المهام
            </h1>

            <div className="flex items-center gap-2">
              <button
                type="button"
                onClick={exportCsv}
                className="h-[38px] px-3 rounded-[10px] bg-white border border-black/10 text-[#2F2F2F] font-sans font-semibold text-[13px] hover:opacity-95"
              >
                تنزيل Excel
              </button>

              <button
                type="button"
                onClick={() => navigate("/tasks/new")}
                className="h-[38px] px-4 rounded-[10px] bg-[#60778E] text-white font-sans font-semibold text-[13px] shadow-[0_2px_0_rgba(0,0,0,0.15)] hover:opacity-95"
              >
                تعيين مهمة جديدة
              </button>
            </div>
          </div>

          {loading && (
            <div className="mt-8 text-center text-[#2F2F2F] font-sans">جاري التحميل...</div>
          )}

          {error && (
            <div className="mt-4 p-4 rounded-[12px] bg-red-100 text-red-700 text-right font-sans">
              {error}
            </div>
          )}

          {!loading && !error && (
            <>
              <div className="mt-4 grid grid-cols-1 sm:grid-cols-3 gap-3">
                <MiniStat title="إجمالي المهام" value={String(stats.total)} />
                <MiniStat title="بانتظار اعتماد" value={String(stats.submitted)} />
            <MiniStat title="قيد التنفيذ" value={String(stats.inProgress)} />
          </div>

          <div className="mt-4 bg-[#F3F1ED] rounded-[14px] border border-black/10 shadow-[0_2px_0_rgba(0,0,0,0.08)] p-3 sm:p-4">
            <div className="flex items-center justify-between gap-3 flex-wrap">
              <div className="flex items-center gap-2 flex-wrap justify-end">
                <FilterChip active={filter === "all"} onClick={() => setFilter("all")}>
                  الكل
                </FilterChip>
                <FilterChip active={filter === "open"} onClick={() => setFilter("open")}>
                  مفتوحة
                </FilterChip>
                <FilterChip active={filter === "inprogress"} onClick={() => setFilter("inprogress")}>
                  قيد التنفيذ
                </FilterChip>
                <FilterChip active={filter === "submitted"} onClick={() => setFilter("submitted")}>
                  بانتظار اعتماد
                </FilterChip>
                <FilterChip active={filter === "approved"} onClick={() => setFilter("approved")}>
                  معتمدة
                </FilterChip>
                <FilterChip active={filter === "rejected"} onClick={() => setFilter("rejected")}>
                  مرفوضة
                </FilterChip>
              </div>

              <div className="w-full sm:w-[320px]">
                <input
                  value={q}
                  onChange={(e) => setQ(e.target.value)}
                  placeholder="بحث بالرقم / العنوان / العامل / المنطقة..."
                  className="w-full h-[38px] rounded-[10px] bg-white border border-black/10 px-4 text-right outline-none focus:ring-2 focus:ring-black/10 text-[13px]"
                />
              </div>
            </div>
          </div>

          <div className="mt-4">
            {filtered.length === 0 ? (
              <div className="bg-white rounded-[14px] border border-black/10 p-5 text-right text-[#6B7280]">
                لا يوجد مهام حسب الفلاتر الحالية.
              </div>
            ) : (
              <div className="bg-[#F3F1ED] rounded-[16px] border border-black/10 shadow-[0_2px_0_rgba(0,0,0,0.06)] overflow-hidden">
                <div className="overflow-auto">
                  <table className="min-w-[980px] w-full text-right">
                    <thead className="bg-[#E9E6E0]">
                      <tr className="text-[12px] text-[#2F2F2F]">
                        <Th>رقم المهمة</Th>
                        <Th>العنوان</Th>
                        <Th>العامل</Th>
                        <Th>المنطقة</Th>
                        <Th>الحالة</Th>
                        <Th>الأولوية</Th>
                        <Th>الموعد</Th>
                        <Th>آخر تحديث</Th>
                      </tr>
                    </thead>
                    <tbody>
                      {filtered.map((r) => (
                        <tr
                          key={r.id}
                          className="border-t border-black/5 text-[12px] text-[#2F2F2F] hover:bg-white/60 cursor-pointer"
                          onClick={() => openTask(r.id)}
                        >
                          <Td className="font-semibold text-[#60778E]">{r.id}</Td>
                          <Td>{r.title}</Td>
                          <Td>{r.workerName}</Td>
                          <Td>{r.zoneName}</Td>
                          <Td>
                            <StatusPill status={r.status} />
                          </Td>
                          <Td>
                            <PriorityPill priority={r.priority} />
                          </Td>
                          <Td className="whitespace-nowrap">{r.dueDate ?? "—"}</Td>
                          <Td className="whitespace-nowrap">{r.updatedAt}</Td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>

                <div className="p-3 text-right text-[12px] text-[#6B7280]">
                  اضغطي على أي سطر لفتح تفاصيل المهمة.
                </div>
              </div>
            )}
          </div>

          <div className="mt-6 text-right text-[12px] text-[#6B7280]">
            {/* TODO: backend should provide tasks list with filters, pagination, and sorting */}
          </div>
            </>
          )}
        </div>
      </div>
    </div>
  );
}

/* ---------- Small UI ---------- */

function MiniStat({ title, value }: { title: string; value: string }) {
  return (
    <div className="bg-[#F3F1ED] rounded-[14px] border border-black/10 shadow-[0_2px_0_rgba(0,0,0,0.06)] p-4">
      <div className="text-right text-[12px] text-[#6B7280] font-sans font-semibold">{title}</div>
      <div className="mt-2 text-right text-[18px] text-[#2F2F2F] font-sans font-bold">{value}</div>
    </div>
  );
}

function FilterChip({
  active,
  onClick,
  children,
}: {
  active: boolean;
  onClick: () => void;
  children: React.ReactNode;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={[
        "h-[34px] px-3 rounded-[10px] font-sans font-semibold text-[13px] border",
        active ? "bg-[#60778E] text-white border-black/10" : "bg-white text-[#2F2F2F] border-black/10 hover:opacity-95",
      ].join(" ")}
    >
      {children}
    </button>
  );
}

function Th({ children }: { children: React.ReactNode }) {
  return <th className="px-3 py-3 font-sans font-semibold whitespace-nowrap">{children}</th>;
}

function Td({ children, className = "" }: { children: React.ReactNode; className?: string }) {
  return <td className={["px-3 py-3 whitespace-nowrap", className].join(" ")}>{children}</td>;
}

function StatusPill({ status }: { status: TaskStatus }) {
  const map: Record<TaskStatus, { label: string; bg: string; text: string }> = {
    Open: { label: "مفتوحة", bg: "#FFFFFF", text: "#2F2F2F" },
    InProgress: { label: "قيد التنفيذ", bg: "#F3E7C8", text: "#2F2F2F" },
    Submitted: { label: "بانتظار اعتماد", bg: "#F2D3CD", text: "#2F2F2F" },
    Approved: { label: "معتمدة", bg: "#8FA36A", text: "#FFFFFF" },
    Rejected: { label: "مرفوضة", bg: "#C86E5D", text: "#FFFFFF" },
  };
  const s = map[status];
  return (
    <span
      className="inline-flex items-center justify-center text-[11px] px-3 py-1.5 rounded-full border border-black/10 font-sans font-semibold"
      style={{ backgroundColor: s.bg, color: s.text }}
    >
      {s.label}
    </span>
  );
}

function PriorityPill({ priority }: { priority: TaskPriority }) {
  const map: Record<TaskPriority, { label: string; bg: string; text: string }> = {
    Low: { label: "منخفضة", bg: "#E5E7EB", text: "#2F2F2F" },
    Medium: { label: "متوسطة", bg: "#60778E", text: "#FFFFFF" },
    High: { label: "عالية", bg: "#C86E5D", text: "#FFFFFF" },
  };
  const p = map[priority];
  return (
    <span
      className="inline-flex items-center justify-center text-[11px] px-3 py-1.5 rounded-full border border-black/10 font-sans font-semibold"
      style={{ backgroundColor: p.bg, color: p.text }}
    >
      {p.label}
    </span>
  );
}

/* ---------- Export Helper ---------- */

function downloadTextFile(text: string, filename: string, mime: string) {
  const blob = new Blob([text], { type: mime });
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = filename;
  document.body.appendChild(a);
  a.click();
  a.remove();
  URL.revokeObjectURL(url);
}
