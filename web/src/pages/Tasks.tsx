import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
// ✅ TODO backend: re-enable when backend is ready
// import { getTasks } from "../api/tasks";
import type { TaskResponse } from "../types/task";
import { ArrowUpDown, Download, Plus, RefreshCw, Search, X } from "lucide-react";

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
  updatedAtISO?: string; // ISO
};

type FilterKey =
  | "all"
  | "open"
  | "inprogress"
  | "submitted"
  | "approved"
  | "rejected";

type SortKey =
  | "id"
  | "title"
  | "workerName"
  | "zoneName"
  | "status"
  | "priority"
  | "dueDate"
  | "updatedAt";
type SortDir = "asc" | "desc";

const PAGE_SIZE = 10;

/* -------------------- MOCK DATA (DEV ONLY) -------------------- */
const MOCK_TASKS: TaskRow[] = [
  {
    id: "101",
    title: "تنظيف الشارع الرئيسي",
    workerName: "أحمد علي",
    zoneName: "المنطقة 1",
    status: "InProgress",
    priority: "Medium",
    dueDate: "2026-01-22",
    updatedAtISO: new Date(Date.now() - 1000 * 60 * 20).toISOString(), // منذ 20 دقيقة
  },
  {
    id: "102",
    title: "إصلاح حفرة قرب المدرسة",
    workerName: "محمد سليم",
    zoneName: "المنطقة 2",
    status: "Open",
    priority: "High",
    dueDate: "2026-01-21",
    updatedAtISO: new Date(Date.now() - 1000 * 60 * 60 * 3).toISOString(), // منذ 3 ساعات
  },
  {
    id: "103",
    title: "تركيب إشارة مرور",
    workerName: "سارة حسن",
    zoneName: "المنطقة 3",
    status: "Submitted",
    priority: "High",
    dueDate: "2026-01-23",
    updatedAtISO: new Date(Date.now() - 1000 * 60 * 60 * 25).toISOString(), // منذ يوم تقريبًا
  },
  {
    id: "104",
    title: "قص الأعشاب في الجزيرة",
    workerName: "يزن محمود",
    zoneName: "المنطقة 4",
    status: "Approved",
    priority: "Low",
    dueDate: "2026-01-20",
    updatedAtISO: new Date(Date.now() - 1000 * 60 * 5).toISOString(),
  },
  {
    id: "105",
    title: "إزالة مخلفات بناء",
    workerName: "لينا أحمد",
    zoneName: "المنطقة 5",
    status: "Rejected",
    priority: "Medium",
    dueDate: "2026-01-24",
    updatedAtISO: new Date(Date.now() - 1000 * 60 * 60 * 7).toISOString(),
  },
];

// (لحتى يبين عندك أكثر من صفحة Pagination)
for (let i = 106; i <= 130; i++) {
  MOCK_TASKS.push({
    id: String(i),
    title: `مهمة تجريبية رقم ${i}`,
    workerName: ["أحمد علي", "محمد سليم", "سارة حسن", "يزن محمود", "لينا أحمد"][i % 5],
    zoneName: `المنطقة ${((i % 5) + 1).toString()}`,
    status: (["Open", "InProgress", "Submitted", "Approved", "Rejected"][
      i % 5
    ] as TaskStatus),
    priority: (["Low", "Medium", "High"][i % 3] as TaskPriority),
    dueDate: `2026-01-${String((i % 9) + 20).padStart(2, "0")}`,
    updatedAtISO: new Date(Date.now() - 1000 * 60 * (i % 180)).toISOString(),
  });
}

/* -------------------- HELPERS -------------------- */

function formatDueDate(due?: string) {
  if (!due) return "—";
  const [y, m, d] = due.split("-").map((x) => Number(x));
  if (!y || !m || !d) return due;
  return `${d.toString().padStart(2, "0")}/${m.toString().padStart(2, "0")}/${y}`;
}

function formatTimeFromISO(iso?: string) {
  if (!iso) return "—";
  const dt = new Date(iso);
  if (Number.isNaN(dt.getTime())) return "—";
  const hh = String(dt.getHours()).padStart(2, "0");
  const mm = String(dt.getMinutes()).padStart(2, "0");
  return `${hh}:${mm}`;
}

function formatRelativeFromISO(iso?: string) {
  if (!iso) return "—";
  const dt = new Date(iso);
  const now = new Date();
  const ms = now.getTime() - dt.getTime();
  if (Number.isNaN(ms)) return "—";

  const mins = Math.floor(ms / (1000 * 60));
  if (mins < 1) return "الآن";
  if (mins < 60) return `منذ ${mins} دقيقة`;

  const hours = Math.floor(mins / 60);
  if (hours < 24) return `منذ ${hours} ساعة`;

  const days = Math.floor(hours / 24);
  return `منذ ${days} يوم`;
}

function compareValues(a: any, b: any) {
  if (a == null && b == null) return 0;
  if (a == null) return -1;
  if (b == null) return 1;

  const an = Number(a);
  const bn = Number(b);
  const aIsNum = !Number.isNaN(an) && String(a).trim() !== "";
  const bIsNum = !Number.isNaN(bn) && String(b).trim() !== "";
  if (aIsNum && bIsNum) return an - bn;

  return String(a).localeCompare(String(b), "ar");
}

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

/* -------------------- PAGE -------------------- */

export default function TasksList() {
  const navigate = useNavigate();

  const [items, setItems] = useState<TaskRow[]>([]);
  const [loading, setLoading] = useState(true);

  // ✅ بالنسخة هاي ما رح يطلع Error نهائيًا لأننا سكّرنا الباك
  const [error, setError] = useState("");

  const [filter, setFilter] = useState<FilterKey>("all");

  const [qInput, setQInput] = useState("");
  const [q, setQ] = useState("");

  const [sortKey, setSortKey] = useState<SortKey>("updatedAt");
  const [sortDir, setSortDir] = useState<SortDir>("desc");

  const [page, setPage] = useState(1);

  const fetchTasks = async () => {
    // ✅ DEV ONLY: no backend call
    try {
      setLoading(true);
      setError("");

      // TODO backend: enable
      // const tasks: TaskResponse[] = await getTasks();
      // const rows = tasks.map(mapTaskToRow);
      // setItems(rows);

      // Mock
      await new Promise((r) => setTimeout(r, 250));
      setItems(MOCK_TASKS);
    } catch (err) {
      // عمليًا لن يحدث الآن
      setError("فشل تحميل المهام");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchTasks();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    const t = window.setTimeout(() => {
      setQ(qInput);
      setPage(1);
    }, 350);
    return () => window.clearTimeout(t);
  }, [qInput]);

  useEffect(() => {
    setPage(1);
  }, [filter]);

  const counts = useMemo(() => {
    const c = {
      all: items.length,
      open: items.filter((x) => x.status === "Open").length,
      inprogress: items.filter((x) => x.status === "InProgress").length,
      submitted: items.filter((x) => x.status === "Submitted").length,
      approved: items.filter((x) => x.status === "Approved").length,
      rejected: items.filter((x) => x.status === "Rejected").length,
    };
    return c;
  }, [items]);

  const miniStats = useMemo(() => {
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

  const sorted = useMemo(() => {
    const getVal = (r: TaskRow, key: SortKey) => {
      switch (key) {
        case "updatedAt":
          return r.updatedAtISO || "";
        default:
          return (r as any)[key];
      }
    };

    const arr = [...filtered];
    arr.sort((a, b) => {
      const av = getVal(a, sortKey);
      const bv = getVal(b, sortKey);
      const cmp = compareValues(av, bv);
      return sortDir === "asc" ? cmp : -cmp;
    });
    return arr;
  }, [filtered, sortKey, sortDir]);

  const totalPages = useMemo(() => Math.max(1, Math.ceil(sorted.length / PAGE_SIZE)), [sorted.length]);

  const pageItems = useMemo(() => {
    const start = (page - 1) * PAGE_SIZE;
    const end = start + PAGE_SIZE;
    return sorted.slice(start, end);
  }, [sorted, page]);

  useEffect(() => {
    if (page > totalPages) setPage(totalPages);
    if (page < 1) setPage(1);
  }, [page, totalPages]);

  const openTask = (id: string) => navigate(`/tasks/${id}`);

  const exportCsv = () => {
    const headers = ["id", "title", "workerName", "zoneName", "status", "priority", "dueDate", "updatedAt"];
    const esc = (v: unknown) => `"${String(v ?? "").replaceAll('"', '""')}"`;

    const lines = [
      headers.join(","),
      ...sorted.map((r) =>
        headers
          .map((h) => {
            if (h === "updatedAt") return esc(formatTimeFromISO(r.updatedAtISO));
            return esc((r as any)[h]);
          })
          .join(",")
      ),
    ];

    downloadTextFile(lines.join("\n"), "tasks-report.csv", "text/csv;charset=utf-8");
  };

  const toggleSort = (key: SortKey) => {
    if (sortKey === key) {
      setSortDir((d) => (d === "asc" ? "desc" : "asc"));
      return;
    }
    setSortKey(key);
    setSortDir("asc");
  };

  return (
    <div className="h-full w-full bg-[#D9D9D9] overflow-auto" dir="rtl">
      <div className="p-4 sm:p-6 md:p-8">
        <div className="max-w-[1100px] mx-auto">
          <div className="flex items-start sm:items-center justify-between gap-3 flex-wrap">
            <div className="flex flex-col gap-1">
              <h1 className="text-right font-sans font-semibold text-[20px] sm:text-[22px] text-[#2F2F2F]">
                المهام
              </h1>
              <div className="text-right text-[12px] text-[#6B7280]">
                إدارة المهام، الفلاتر، والبحث — اضغطي على أي مهمة لفتح التفاصيل.
              </div>
            </div>

            <div className="flex items-center gap-2">
              <button
                type="button"
                onClick={exportCsv}
                className="h-[38px] px-3 rounded-[10px] bg-white border border-black/10 text-[#2F2F2F] font-sans font-semibold text-[13px] hover:opacity-95 inline-flex items-center gap-2"
              >
                <Download size={16} />
                تنزيل CSV
              </button>

              <button
                type="button"
                onClick={() => navigate("/tasks/new")}
                className="h-[38px] px-4 rounded-[10px] bg-[#60778E] text-white font-sans font-semibold text-[13px] shadow-[0_2px_0_rgba(0,0,0,0.15)] hover:opacity-95 inline-flex items-center gap-2"
              >
                <Plus size={16} />
                تعيين مهمة جديدة
              </button>
            </div>
          </div>

          {loading && (
            <div className="mt-8 text-center text-[#2F2F2F] font-sans">جاري التحميل...</div>
          )}

          {!loading && error && (
            <div className="mt-4 p-4 rounded-[12px] bg-red-100 text-red-700 text-right font-sans">
              <div className="font-semibold">حدث خطأ</div>
              <div className="mt-1">{error}</div>

              <div className="mt-3 flex items-center justify-end gap-2 flex-wrap">
                <button
                  type="button"
                  onClick={fetchTasks}
                  className="h-[36px] px-4 rounded-[10px] bg-white border border-black/10 text-[#2F2F2F] font-sans font-semibold text-[13px] hover:opacity-95 inline-flex items-center gap-2"
                >
                  <RefreshCw size={16} />
                  إعادة المحاولة
                </button>
              </div>
            </div>
          )}

          {!loading && !error && (
            <>
              <div className="mt-4 grid grid-cols-1 sm:grid-cols-3 gap-3">
                <MiniStat title="إجمالي المهام" value={String(miniStats.total)} />
                <MiniStat title="بانتظار اعتماد" value={String(miniStats.submitted)} />
                <MiniStat title="قيد التنفيذ" value={String(miniStats.inProgress)} />
              </div>

              <div className="mt-4 bg-[#F3F1ED] rounded-[14px] border border-black/10 shadow-[0_2px_0_rgba(0,0,0,0.08)] p-3 sm:p-4">
                <div className="flex items-center justify-between gap-3 flex-wrap">
                  <div className="flex items-center gap-2 flex-wrap justify-end">
                    <FilterChip active={filter === "all"} onClick={() => setFilter("all")} count={counts.all}>
                      الكل
                    </FilterChip>
                    <FilterChip active={filter === "open"} onClick={() => setFilter("open")} count={counts.open}>
                      مفتوحة
                    </FilterChip>
                    <FilterChip
                      active={filter === "inprogress"}
                      onClick={() => setFilter("inprogress")}
                      count={counts.inprogress}
                    >
                      قيد التنفيذ
                    </FilterChip>
                    <FilterChip
                      active={filter === "submitted"}
                      onClick={() => setFilter("submitted")}
                      count={counts.submitted}
                    >
                      بانتظار اعتماد
                    </FilterChip>
                    <FilterChip
                      active={filter === "approved"}
                      onClick={() => setFilter("approved")}
                      count={counts.approved}
                    >
                      معتمدة
                    </FilterChip>
                    <FilterChip
                      active={filter === "rejected"}
                      onClick={() => setFilter("rejected")}
                      count={counts.rejected}
                    >
                      مرفوضة
                    </FilterChip>
                  </div>

                  <div className="w-full sm:w-[360px]">
                    <div className="relative">
                      <span className="absolute right-3 top-1/2 -translate-y-1/2 text-[#60778E]">
                        <Search size={16} />
                      </span>

                      {qInput.trim() ? (
                        <button
                          type="button"
                          onClick={() => setQInput("")}
                          className="absolute left-2 top-1/2 -translate-y-1/2 w-[30px] h-[30px] rounded-full grid place-items-center hover:bg-black/5"
                          aria-label="مسح البحث"
                        >
                          <X size={16} className="text-[#6B7280]" />
                        </button>
                      ) : null}

                      <input
                        value={qInput}
                        onChange={(e) => setQInput(e.target.value)}
                        placeholder="بحث بالرقم / العنوان / العامل / المنطقة..."
                        className="w-full h-[38px] rounded-[10px] bg-white border border-black/10 pr-10 pl-10 text-right outline-none focus:ring-2 focus:ring-black/10 text-[13px]"
                      />
                    </div>

                    {q.trim() ? (
                      <div className="mt-1 text-right text-[11px] text-[#6B7280]">
                        نتائج البحث عن: <span className="font-semibold">{q}</span>
                      </div>
                    ) : null}
                  </div>
                </div>
              </div>

              {sorted.length === 0 ? (
                <div className="mt-4 bg-white rounded-[14px] border border-black/10 p-5 text-right">
                  <div className="text-[#2F2F2F] font-sans font-semibold">لا يوجد مهام حسب الفلاتر الحالية.</div>
                  <div className="mt-1 text-[#6B7280] text-[13px]">جربي تغيير الفلاتر أو البحث.</div>
                </div>
              ) : (
                <>
                  <div className="mt-4 hidden md:block">
                    <div className="bg-[#F3F1ED] rounded-[16px] border border-black/10 shadow-[0_2px_0_rgba(0,0,0,0.06)] overflow-hidden">
                      <div className="overflow-auto">
                        <table className="min-w-[980px] w-full text-right">
                          <thead className="bg-[#E9E6E0]">
                            <tr className="text-[12px] text-[#2F2F2F]">
                              <SortableTh active={sortKey === "id"} dir={sortDir} onClick={() => toggleSort("id")}>
                                رقم المهمة
                              </SortableTh>
                              <SortableTh
                                active={sortKey === "title"}
                                dir={sortDir}
                                onClick={() => toggleSort("title")}
                              >
                                العنوان
                              </SortableTh>
                              <SortableTh
                                active={sortKey === "workerName"}
                                dir={sortDir}
                                onClick={() => toggleSort("workerName")}
                              >
                                العامل
                              </SortableTh>
                              <SortableTh
                                active={sortKey === "zoneName"}
                                dir={sortDir}
                                onClick={() => toggleSort("zoneName")}
                              >
                                المنطقة
                              </SortableTh>
                              <SortableTh
                                active={sortKey === "status"}
                                dir={sortDir}
                                onClick={() => toggleSort("status")}
                              >
                                الحالة
                              </SortableTh>
                              <SortableTh
                                active={sortKey === "priority"}
                                dir={sortDir}
                                onClick={() => toggleSort("priority")}
                              >
                                الأولوية
                              </SortableTh>
                              <SortableTh
                                active={sortKey === "dueDate"}
                                dir={sortDir}
                                onClick={() => toggleSort("dueDate")}
                              >
                                الموعد
                              </SortableTh>
                              <SortableTh
                                active={sortKey === "updatedAt"}
                                dir={sortDir}
                                onClick={() => toggleSort("updatedAt")}
                              >
                                آخر تحديث
                              </SortableTh>
                              <Th>إجراء</Th>
                            </tr>
                          </thead>

                          <tbody>
                            {pageItems.map((r) => (
                              <tr
                                key={r.id}
                                className="border-t border-black/5 text-[12px] text-[#2F2F2F] hover:bg-white/60"
                              >
                                <Td className="font-semibold text-[#60778E]">{r.id}</Td>
                                <Td className="max-w-[260px] truncate">{r.title}</Td>
                                <Td>{r.workerName}</Td>
                                <Td>{r.zoneName}</Td>
                                <Td>
                                  <StatusPill status={r.status} />
                                </Td>
                                <Td>
                                  <PriorityPill priority={r.priority} />
                                </Td>
                                <Td className="whitespace-nowrap">{formatDueDate(r.dueDate)}</Td>
                                <Td className="whitespace-nowrap">
                                  <div className="flex flex-col items-end">
                                    <span className="text-[#2F2F2F] font-sans font-semibold">
                                      {formatTimeFromISO(r.updatedAtISO)}
                                    </span>
                                    <span className="text-[11px] text-[#6B7280]">
                                      {formatRelativeFromISO(r.updatedAtISO)}
                                    </span>
                                  </div>
                                </Td>
                                <Td>
                                  <button
                                    type="button"
                                    onClick={() => openTask(r.id)}
                                    className="h-[30px] px-3 rounded-[10px] bg-white border border-black/10 text-[#2F2F2F] font-sans font-semibold text-[12px] hover:opacity-95"
                                  >
                                    عرض
                                  </button>
                                </Td>
                              </tr>
                            ))}
                          </tbody>
                        </table>
                      </div>

                      <div className="p-3 flex items-center justify-between gap-3 flex-wrap">
                        <div className="text-right text-[12px] text-[#6B7280]">
                          عرض {pageItems.length} من أصل {sorted.length} مهمة
                        </div>

                        <Pagination
                          page={page}
                          totalPages={totalPages}
                          onPrev={() => setPage((p) => Math.max(1, p - 1))}
                          onNext={() => setPage((p) => Math.min(totalPages, p + 1))}
                          onGoto={(p) => setPage(p)}
                        />
                      </div>
                    </div>
                  </div>

                  <div className="mt-4 md:hidden space-y-3">
                    {pageItems.map((r) => (
                      <button
                        key={r.id}
                        type="button"
                        onClick={() => openTask(r.id)}
                        className="w-full text-right bg-[#F3F1ED] rounded-[16px] border border-black/10 shadow-[0_2px_0_rgba(0,0,0,0.06)] p-4"
                      >
                        <div className="flex items-start justify-between gap-3">
                          <div className="flex-1 min-w-0">
                            <div className="text-[12px] text-[#6B7280]">
                              رقم المهمة: <span className="font-semibold text-[#60778E]">{r.id}</span>
                            </div>
                            <div className="mt-1 font-sans font-semibold text-[#2F2F2F] text-[14px] truncate">
                              {r.title}
                            </div>
                          </div>

                          <div className="flex flex-col items-end gap-2">
                            <StatusPill status={r.status} />
                            <PriorityPill priority={r.priority} />
                          </div>
                        </div>

                        <div className="mt-3 grid grid-cols-2 gap-2 text-[12px] text-[#2F2F2F]">
                          <InfoBox label="العامل" value={r.workerName} />
                          <InfoBox label="المنطقة" value={r.zoneName} />
                          <InfoBox label="الموعد" value={formatDueDate(r.dueDate)} />
                          <InfoBox
                            label="آخر تحديث"
                            value={`${formatTimeFromISO(r.updatedAtISO)} — ${formatRelativeFromISO(r.updatedAtISO)}`}
                          />
                        </div>

                        <div className="mt-3 text-[11px] text-[#6B7280]">اضغطي لفتح التفاصيل</div>
                      </button>
                    ))}

                    <div className="pt-2">
                      <Pagination
                        page={page}
                        totalPages={totalPages}
                        onPrev={() => setPage((p) => Math.max(1, p - 1))}
                        onNext={() => setPage((p) => Math.min(totalPages, p + 1))}
                        onGoto={(p) => setPage(p)}
                      />
                    </div>
                  </div>

                  <div className="mt-6 text-right text-[12px] text-[#6B7280]">
                    {/* TODO backend: tasks list with filters, pagination, and sorting server-side */}
                  </div>
                </>
              )}
            </>
          )}
        </div>
      </div>
    </div>
  );
}

/* -------------------- SMALL UI -------------------- */

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
  count,
  children,
}: {
  active: boolean;
  onClick: () => void;
  count: number;
  children: React.ReactNode;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={[
        "h-[34px] px-3 rounded-[10px] font-sans font-semibold text-[13px] border",
        "inline-flex items-center gap-2",
        active ? "bg-[#60778E] text-white border-black/10" : "bg-white text-[#2F2F2F] border-black/10 hover:opacity-95",
      ].join(" ")}
    >
      <span>{children}</span>
      <span
        className={[
          "min-w-[22px] h-[22px] px-2 rounded-full text-[11px] grid place-items-center",
          active ? "bg-white/20 text-white" : "bg-[#F3F1ED] text-[#2F2F2F]",
        ].join(" ")}
      >
        {count}
      </span>
    </button>
  );
}

function Th({ children }: { children: React.ReactNode }) {
  return <th className="px-3 py-3 font-sans font-semibold whitespace-nowrap">{children}</th>;
}

function SortableTh({
  children,
  active,
  dir,
  onClick,
}: {
  children: React.ReactNode;
  active: boolean;
  dir: SortDir;
  onClick: () => void;
}) {
  return (
    <th className="px-3 py-3 font-sans font-semibold whitespace-nowrap">
      <button type="button" onClick={onClick} className="inline-flex items-center gap-2 hover:opacity-80 transition" title="فرز">
        <span>{children}</span>
        <span className={active ? "opacity-100" : "opacity-50"}>
          <ArrowUpDown size={14} />
        </span>
        {active ? <span className="text-[11px] text-[#6B7280]">{dir === "asc" ? "تصاعدي" : "تنازلي"}</span> : null}
      </button>
    </th>
  );
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

function Pagination({
  page,
  totalPages,
  onPrev,
  onNext,
  onGoto,
}: {
  page: number;
  totalPages: number;
  onPrev?: () => void;
  onNext?: () => void;
  onGoto?: (p: number) => void;
}) {
  const canPrev = page > 1;
  const canNext = page < totalPages;

  const windowPages = useMemo(() => {
    const arr: number[] = [];
    const start = Math.max(1, page - 2);
    const end = Math.min(totalPages, page + 2);
    for (let p = start; p <= end; p++) arr.push(p);
    return arr;
  }, [page, totalPages]);

  return (
    <div className="flex items-center gap-2 justify-end">
      <button
        type="button"
        onClick={onPrev}
        disabled={!canPrev}
        className="h-[34px] px-3 rounded-[10px] bg-white border border-black/10 text-[#2F2F2F] font-sans font-semibold text-[12px] disabled:opacity-50"
      >
        السابق
      </button>

      <div className="flex items-center gap-1">
        {windowPages.map((p) => (
          <button
            key={p}
            type="button"
            onClick={() => onGoto?.(p)}
            className={[
              "w-[34px] h-[34px] rounded-[10px] border font-sans font-semibold text-[12px]",
              p === page ? "bg-[#60778E] text-white border-black/10" : "bg-white text-[#2F2F2F] border-black/10 hover:opacity-95",
            ].join(" ")}
          >
            {p}
          </button>
        ))}
      </div>

      <button
        type="button"
        onClick={onNext}
        disabled={!canNext}
        className="h-[34px] px-3 rounded-[10px] bg-white border border-black/10 text-[#2F2F2F] font-sans font-semibold text-[12px] disabled:opacity-50"
      >
        التالي
      </button>
    </div>
  );
}

function InfoBox({ label, value }: { label: string; value: string }) {
  return (
    <div className="bg-white/70 rounded-[12px] border border-black/10 p-2">
      <div className="text-[#6B7280] text-[11px]">{label}</div>
      <div className="mt-1 font-sans font-semibold">{value}</div>
    </div>
  );
}
