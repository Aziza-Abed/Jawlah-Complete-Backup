import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { getIssues } from "../api/issues";
import type { IssueResponse } from "../types/issue";
import { RefreshCw, ChevronLeft } from "lucide-react";

type Severity = "low" | "medium" | "high" | "critical";
type IssueStatus = "new" | "reviewing" | "converted" | "closed";

type IssueListItem = {
  id: string;
  title: string;
  type: string;
  severity: Severity;
  reporterName: string;
  zone: string;
  locationText: string;
  reportedAt: string;
  status: IssueStatus;
};

type FilterKey = "all" | "new" | "reviewing" | "converted" | "closed";

const mapSeverity = (severity: string | null | undefined): Severity => {
  if (!severity) return "medium";
  switch (severity.toLowerCase()) {
    case "minor": return "low";
    case "medium": return "medium";
    case "major": return "high";
    case "critical": return "critical";
    default: return "medium";
  }
};

const mapStatus = (status: string | null | undefined): IssueStatus => {
  if (!status) return "new";
  switch (status) {
    case "Reported": return "new";
    case "UnderReview": return "reviewing";
    case "Resolved": return "closed";
    case "Dismissed": return "closed";
    default: return "new";
  }
};

const mapTypeToArabic = (type: string): string => {
  switch (type) {
    case "Infrastructure": return "بنية تحتية";
    case "Safety": return "سلامة";
    case "Sanitation": return "صحة ونظافة";
    case "Equipment": return "معدات";
    case "Other": return "أخرى";
    default: return type;
  }
};

const mapIssueToListItem = (issue: IssueResponse): IssueListItem => {
  const date = issue.reportedAt ? new Date(issue.reportedAt) : new Date();
  const formattedDate = isNaN(date.getTime()) 
    ? "تاريخ غير صالح" 
    : `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")} ${String(date.getHours()).padStart(2, "0")}:${String(date.getMinutes()).padStart(2, "0")}`;

  return {
    id: issue.issueId.toString(),
    title: issue.title,
    type: mapTypeToArabic(issue.type),
    severity: mapSeverity(issue.severity),
    reporterName: issue.reportedByName || "غير محدد",
    zone: issue.zoneName || "غير محدد",
    locationText: issue.locationDescription || "غير محدد",
    reportedAt: formattedDate,
    status: mapStatus(issue.status),
  };
};

export default function Issues() {
  const navigate = useNavigate();

  const [items, setItems] = useState<IssueListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [filter, setFilter] = useState<FilterKey>("all");
  const [q, setQ] = useState("");

  const fetchIssues = async () => {
    try {
      setLoading(true);
      setError("");
      const issues = await getIssues();
      const listItems = issues.map(mapIssueToListItem);
      setItems(listItems);
    } catch (err) {
      console.error("Failed to fetch issues:", err);
      setError("فشل تحميل البلاغات");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchIssues();
  }, []);

  const counts = useMemo(() => {
    const base = { all: items.length, new: 0, reviewing: 0, converted: 0, closed: 0 };
    for (const it of items) base[it.status]++;
    return base;
  }, [items]);

  const filtered = useMemo(() => {
    const query = q.trim().toLowerCase();
    const byStatus = filter === "all" ? items : items.filter((x) => x.status === filter);
    if (!query) return byStatus;
    return byStatus.filter((x) => 
      x.id.toLowerCase().includes(query) || 
      x.title.toLowerCase().includes(query) || 
      x.reporterName.toLowerCase().includes(query)
    );
  }, [items, filter, q]);

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
             <h1 className="text-right font-sans font-semibold text-[20px] sm:text-[22px] text-[#2F2F2F]">
                البلاغات الواردة
             </h1>
             <button
               type="button"
               onClick={fetchIssues}
               className="h-[38px] px-4 rounded-[10px] bg-white border border-black/10 text-[#2F2F2F] font-sans font-semibold text-[13px] hover:opacity-95 flex items-center gap-2"
             >
               <RefreshCw size={14} />
               تحديث
             </button>
          </div>

          {error && (
            <div className="mt-4 p-3 bg-red-100 text-red-700 rounded-[10px] text-right">
              {error}
            </div>
          )}

          {/* Mini Stats Grid */}
          <div className="mt-4 grid grid-cols-1 sm:grid-cols-4 gap-3">
             <MiniStat title="إجمالي البلاغات" value={String(counts.all)} />
             <MiniStat title="بلاغات جديدة" value={String(counts.new)} />
             <MiniStat title="قيد المراجعة" value={String(counts.reviewing)} />
             <MiniStat title="مغلقة" value={String(counts.closed)} />
          </div>

          {/* Filter Bar */}
          <div className="mt-4 bg-[#F3F1ED] rounded-[14px] border border-black/10 shadow-[0_2px_0_rgba(0,0,0,0.08)] p-3 sm:p-4">
            <div className="flex flex-col lg:flex-row lg:items-center gap-3">
              <div className="flex-1">
                <input
                  value={q}
                  onChange={(e) => setQ(e.target.value)}
                  placeholder="بحث برقم البلاغ، العنوان، أو اسم المبلغ..."
                  className="w-full h-[44px] rounded-[12px] bg-white border border-black/10 px-4 text-right outline-none focus:ring-2 focus:ring-black/10 text-[13px]"
                />
              </div>

              <div className="flex items-center gap-2 flex-wrap justify-end">
                <FilterChip active={filter === "all"} onClick={() => setFilter("all")} label="الكل" count={counts.all} />
                <FilterChip active={filter === "new"} onClick={() => setFilter("new")} label="جديد" count={counts.new} />
                <FilterChip active={filter === "reviewing"} onClick={() => setFilter("reviewing")} label="مراجعة" count={counts.reviewing} />
                <FilterChip active={filter === "closed"} onClick={() => setFilter("closed")} label="مغلق" count={counts.closed} />
              </div>
            </div>
          </div>

          {/* Issues List */}
          <div className="mt-4 space-y-3">
            {filtered.length === 0 ? (
              <div className="bg-white rounded-[14px] border border-black/10 p-5 text-right text-[#6B7280]">
                لا يوجد بلاغات تطابق البحث.
              </div>
            ) : (
              filtered.map((it) => (
                <IssueCard key={it.id} issue={it} onClick={() => navigate(`/issues/${it.id}`)} />
              ))
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

function MiniStat({ title, value }: { title: string; value: string }) {
  return (
    <div className="bg-[#F3F1ED] rounded-[14px] border border-black/10 shadow-[0_2px_0_rgba(0,0,0,0.06)] p-4">
      <div className="text-right text-[12px] text-[#6B7280] font-sans font-semibold">{title}</div>
      <div className="mt-2 text-right text-[18px] text-[#2F2F2F] font-sans font-bold">{value}</div>
    </div>
  );
}

function FilterChip({ active, onClick, label, count }: { active: boolean; onClick: () => void; label: string; count: number }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={[
        "h-[34px] px-3 rounded-[10px] font-sans font-semibold text-[13px] border flex items-center gap-2 transition-all",
        active ? "bg-[#7895B2] text-white border-black/10" : "bg-white text-[#2F2F2F] border-black/10 hover:opacity-95",
      ].join(" ")}
    >
      <span>{label}</span>
      <span className={["min-w-[20px] h-[20px] px-1 rounded-full text-[11px] font-bold grid place-items-center", active ? "bg-white/20" : "bg-black/5"].join(" ")}>
        {count}
      </span>
    </button>
  );
}

function IssueCard({ issue, onClick }: { issue: IssueListItem; onClick: () => void }) {
  return (
    <button
      onClick={onClick}
      className="w-full bg-[#F3F1ED] rounded-[14px] border border-black/10 shadow-[0_2px_0_rgba(0,0,0,0.06)] p-4 sm:p-5 hover:bg-white/60 transition text-right flex flex-col sm:flex-row sm:items-center justify-between gap-4"
    >
      <div className="flex items-center gap-4 flex-1">
        <div className="w-12 h-12 rounded-[12px] bg-white border border-black/10 flex items-center justify-center shrink-0 text-[16px] font-bold text-[#7895B2] shadow-sm">
          {issue.id}
        </div>
        <div className="flex-1 flex items-center justify-start gap-4 text-right overflow-hidden">
           <div className="text-[15px] font-sans font-semibold text-[#2F2F2F] truncate">
             {issue.title}
           </div>
           <div className="text-[12px] text-[#6B7280] hidden md:block truncate">
             • {issue.reporterName} • {issue.zone}
           </div>
        </div>
      </div>

      <div className="flex items-center gap-4 flex-wrap justify-end">
        <div className="grid grid-cols-2 gap-2">
           <MetaSmallPill label="الحالة" value={formatStatusLabel(issue.status)} color={getStatusColor(issue.status)} />
           <MetaSmallPill label="الأهمية" value={formatSeverityLabel(issue.severity)} color={getSeverityColor(issue.severity)} />
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

function formatStatusLabel(s: IssueStatus) {
  const map: Record<IssueStatus, string> = {
    new: "جديد",
    reviewing: "قيد المراجعة",
    converted: "محول لمهمة",
    closed: "مغلق",
  };
  return map[s];
}

function getStatusColor(s: IssueStatus) {
  const map: Record<IssueStatus, string> = {
    new: "#7895B2",
    reviewing: "#F3E7C8",
    converted: "#8FA36A",
    closed: "#6B7280",
  };
  return map[s];
}

function formatSeverityLabel(s: Severity) {
  const map: Record<Severity, string> = {
    low: "منخفضة",
    medium: "متوسطة",
    high: "عالية",
    critical: "حرجة",
  };
  return map[s];
}

function getSeverityColor(s: Severity) {
  const map: Record<Severity, string> = {
    low: "#6B7280",
    medium: "#7895B2",
    high: "#C86E5D",
    critical: "#C86E5D",
  };
  return map[s];
}

