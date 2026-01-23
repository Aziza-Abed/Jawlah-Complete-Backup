import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { AlertTriangle, Search, Trash2, Eye, Filter, X } from "lucide-react";
import { getIssues, deleteIssue } from "../api/issues";
import type { IssueResponse } from "../types/issue";
import GlassCard from "../components/UI/GlassCard";

type Severity = "low" | "medium" | "high" | "critical";
type IssueStatus = "new" | "reviewing" | "converted" | "closed";

type IssueListItem = {
  id: number;
  displayId: string;
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

// Map backend severity to frontend severity
const mapSeverity = (severity: string): Severity => {
  switch (severity.toLowerCase()) {
    case "minor": return "low";
    case "medium": return "medium";
    case "major": return "high";
    case "critical": return "critical";
    default: return "medium";
  }
};

// Map backend status to frontend status
const mapStatus = (status: string): IssueStatus => {
  switch (status) {
    case "Reported": return "new";
    case "UnderReview": return "reviewing";
    case "Resolved": return "closed";
    case "Dismissed": return "closed";
    default: return "new";
  }
};

// Map backend IssueType enum to Arabic
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

// Convert backend IssueResponse to frontend IssueListItem
const mapIssueToListItem = (issue: IssueResponse): IssueListItem => {
  const date = new Date(issue.reportedAt);
  const formattedDate = `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")} ${String(date.getHours()).padStart(2, "0")}:${String(date.getMinutes()).padStart(2, "0")}`;

  return {
    id: issue.issueId,
    displayId: `i-${issue.issueId}`,
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

export default function AdminIssues() {
  const navigate = useNavigate();

  const [items, setItems] = useState<IssueListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [filter, setFilter] = useState<FilterKey>("all");
  const [q, setQ] = useState("");
  const [deleting, setDeleting] = useState<number | null>(null);

  // Fetch issues from backend
  useEffect(() => {
    fetchIssues();
  }, []);

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

  const handleDelete = async (id: number, e: React.MouseEvent) => {
    e.stopPropagation();
    if (!confirm("هل أنت متأكد من حذف هذا البلاغ؟")) return;

    try {
      setDeleting(id);
      await deleteIssue(id);
      setItems(prev => prev.filter(item => item.id !== id));
    } catch (err) {
      console.error("Failed to delete issue:", err);
      alert("فشل حذف البلاغ");
    } finally {
      setDeleting(null);
    }
  };

  const counts = useMemo(() => {
    const base = { all: items.length, new: 0, reviewing: 0, converted: 0, closed: 0 };
    for (const it of items) base[it.status]++;
    return base;
  }, [items]);

  const filtered = useMemo(() => {
    const query = q.trim();
    const byStatus =
      filter === "all" ? items : items.filter((x) => x.status === filter);

    if (!query) return byStatus;

    const norm = (s: string) => s.toLowerCase();
    const nq = norm(query);

    return byStatus.filter((x) => {
      return (
        norm(x.displayId).includes(nq) ||
        norm(x.title).includes(nq) ||
        norm(x.reporterName).includes(nq) ||
        norm(x.zone).includes(nq) ||
        norm(x.type).includes(nq)
      );
    });
  }, [items, filter, q]);

  const openIssue = (id: string) => {
    navigate(`/issues/${id}`);
  };

  const severityColors = {
    low: "bg-text-muted/10 text-text-muted border-text-muted/30",
    medium: "bg-warning/10 text-warning border-warning/30",
    high: "bg-accent/10 text-accent border-accent/30",
    critical: "bg-accent text-white border-accent",
  };

  const severityLabels = {
    low: "منخفضة",
    medium: "متوسطة",
    high: "عالية",
    critical: "حرجة",
  };

  const statusColors = {
    new: "bg-primary/10 text-primary border-primary/30",
    reviewing: "bg-warning/10 text-warning border-warning/30",
    converted: "bg-secondary text-white border-secondary",
    closed: "bg-text-muted/10 text-text-muted border-text-muted/30",
  };

  const statusLabels = {
    new: "جديد",
    reviewing: "قيد المراجعة",
    converted: "تم تحويله لمهمة",
    closed: "مغلق",
  };

  return (
    <div className="min-h-screen bg-background-paper p-6 animate-fade-in">
      <div className="max-w-7xl mx-auto space-y-6">
        {/* Header */}
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-6">
          <div className="flex flex-row-reverse items-center gap-3">
            <div className="p-3 rounded-xl bg-accent/10 text-accent border border-accent/10">
              <AlertTriangle size={24} />
            </div>
            <div className="text-right">
              <h1 className="text-2xl font-bold text-text-primary">البلاغات</h1>
              <p className="text-text-secondary text-sm">
                إدارة ومتابعة البلاغات المرسلة من العمال
              </p>
            </div>
          </div>
          <div className="text-text-secondary text-sm">
            إجمالي: <span className="text-text-primary font-bold">{items.length}</span> بلاغ
          </div>
        </div>

        {/* Search & Filters */}
        <GlassCard>
          <div className="flex flex-col lg:flex-row gap-4">
            {/* Search */}
            <div className="flex-1 relative">
              <Search className="absolute right-4 top-1/2 -translate-y-1/2 text-text-muted" size={18} />
              <input
                value={q}
                onChange={(e) => setQ(e.target.value)}
                placeholder="بحث (رقم البلاغ / عنوان / عامل / منطقة / نوع)..."
                className="w-full h-12 pr-12 pl-4 rounded-xl bg-primary/5 border border-primary/10 text-text-primary text-right placeholder:text-text-muted focus:outline-none focus:border-primary/30 transition-colors"
              />
              {q && (
                <button
                  onClick={() => setQ("")}
                  className="absolute left-4 top-1/2 -translate-y-1/2 text-text-muted hover:text-text-primary"
                >
                  <X size={16} />
                </button>
              )}
            </div>

            {/* Filter Chips */}
            <div className="flex items-center gap-2 flex-wrap">
              <Filter size={16} className="text-text-muted" />
              {(["all", "new", "reviewing", "converted", "closed"] as FilterKey[]).map((key) => (
                <button
                  key={key}
                  onClick={() => setFilter(key)}
                  className={`px-3 py-2 rounded-lg text-sm font-medium transition-all border ${
                    filter === key
                      ? "bg-primary text-white border-primary shadow-lg shadow-primary/20"
                      : "bg-primary/5 text-text-secondary border-primary/5 hover:bg-primary/10"
                  }`}
                >
                  {key === "all" ? "الكل" : statusLabels[key]}
                  <span className={`mr-2 px-1.5 py-0.5 rounded text-xs ${
                    filter === key ? "bg-white/20" : "bg-primary/10"
                  }`}>
                    {counts[key]}
                  </span>
                </button>
              ))}
            </div>
          </div>
        </GlassCard>

        {/* Loading State */}
        {loading && (
          <div className="text-center py-12">
            <div className="w-8 h-8 border-2 border-primary border-t-transparent rounded-full animate-spin mx-auto mb-4" />
            <p className="text-text-secondary">جاري التحميل...</p>
          </div>
        )}

        {/* Error State */}
        {error && (
          <GlassCard className="border-red-500/30 bg-red-500/10">
            <p className="text-red-400 text-center">{error}</p>
          </GlassCard>
        )}

        {/* Issues List */}
        {!loading && !error && (
          <div className="space-y-3">
            {filtered.length === 0 ? (
              <GlassCard className="text-center py-12">
                <AlertTriangle className="mx-auto text-text-muted mb-4" size={48} />
                <p className="text-text-secondary">لا يوجد نتائج حسب الفلتر/البحث الحالي</p>
              </GlassCard>
            ) : (
              filtered.map((item) => (
                <GlassCard
                  key={item.id}
                  variant="hover"
                  className="cursor-pointer"
                  onClick={() => openIssue(item.displayId)}
                >
                  <div className="flex items-start justify-between gap-4">
                    {/* Right Side - Main Info */}
                    <div className="flex-1 min-w-0">
                      <div className="flex items-start justify-between gap-3 mb-3">
                        <div>
                          <h3 className="text-text-primary font-semibold text-lg truncate">
                            {item.title}
                          </h3>
                          <p className="text-text-muted text-sm mt-1">
                            #{item.displayId} • {item.reporterName} • {item.zone}
                          </p>
                        </div>
                        <div className="flex items-center gap-2 shrink-0">
                          <span className={`px-3 py-1 rounded-full text-xs font-medium border ${statusColors[item.status]}`}>
                            {statusLabels[item.status]}
                          </span>
                          <span className={`px-3 py-1 rounded-full text-xs font-medium border ${severityColors[item.severity]}`}>
                            {severityLabels[item.severity]}
                          </span>
                        </div>
                      </div>

                       {/* Meta Info Grid */}
                      <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
                        <div className="bg-primary/5 rounded-lg px-3 py-2 border border-primary/5">
                          <p className="text-text-muted text-xs">نوع المشكلة</p>
                          <p className="text-text-primary text-sm font-medium">{item.type}</p>
                        </div>
                        <div className="bg-primary/5 rounded-lg px-3 py-2 border border-primary/5">
                          <p className="text-text-muted text-xs">وصف الموقع</p>
                          <p className="text-text-primary text-sm font-medium truncate">{item.locationText}</p>
                        </div>
                        <div className="bg-primary/5 rounded-lg px-3 py-2 border border-primary/5">
                          <p className="text-text-muted text-xs">تاريخ الإبلاغ</p>
                          <p className="text-text-primary text-sm font-medium">{item.reportedAt}</p>
                        </div>
                      </div>
                    </div>

                    {/* Left Side - Actions */}
                    <div className="flex flex-col gap-2">
                      <button
                        onClick={(e) => {
                          e.stopPropagation();
                          openIssue(item.displayId);
                        }}
                        className="p-2 rounded-lg bg-primary/10 text-primary hover:bg-primary/20 transition-colors border border-primary/10"
                        title="عرض التفاصيل"
                      >
                        <Eye size={18} />
                      </button>
                      <button
                        onClick={(e) => handleDelete(item.id, e)}
                        disabled={deleting === item.id}
                        className="p-2 rounded-lg bg-accent/10 text-accent hover:bg-accent/20 transition-colors border border-accent/10 disabled:opacity-50"
                        title="حذف البلاغ"
                      >
                        {deleting === item.id ? (
                          <div className="w-[18px] h-[18px] border-2 border-accent border-t-transparent rounded-full animate-spin" />
                        ) : (
                          <Trash2 size={18} />
                        )}
                      </button>
                    </div>
                  </div>
                </GlassCard>
              ))
            )}
          </div>
        )}
      </div>
    </div>
  );
}
