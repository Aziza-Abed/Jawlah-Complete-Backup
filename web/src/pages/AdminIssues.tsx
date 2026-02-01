import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { AlertTriangle, Search, Trash2, Eye, Filter, X } from "lucide-react";
import { getIssues, deleteIssue } from "../api/issues";
import type { IssueResponse } from "../types/issue";

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

const mapSeverity = (severity: string | undefined | null): Severity => {
  switch ((severity || "").toLowerCase()) {
    case "minor": return "low";
    case "medium": return "medium";
    case "major": return "high";
    case "critical": return "critical";
    default: return "medium";
  }
};

const mapStatus = (status: string): IssueStatus => {
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
    const byStatus = filter === "all" ? items : items.filter((x) => x.status === filter);

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
    low: "bg-[#6B7280]/10 text-[#6B7280] border-[#6B7280]/30",
    medium: "bg-[#F5B300]/10 text-[#D4A100] border-[#F5B300]/30",
    high: "bg-[#C86E5D]/10 text-[#C86E5D] border-[#C86E5D]/30",
    critical: "bg-[#C86E5D] text-white border-[#C86E5D]",
  };

  const severityLabels = {
    low: "منخفضة",
    medium: "متوسطة",
    high: "عالية",
    critical: "حرجة",
  };

  const statusColors = {
    new: "bg-[#7895B2]/10 text-[#7895B2] border-[#7895B2]/30",
    reviewing: "bg-[#F5B300]/10 text-[#D4A100] border-[#F5B300]/30",
    converted: "bg-[#8FA36A] text-white border-[#8FA36A]",
    closed: "bg-[#6B7280]/10 text-[#6B7280] border-[#6B7280]/30",
  };

  const statusLabels = {
    new: "جديد",
    reviewing: "قيد المراجعة",
    converted: "تم تحويله لمهمة",
    closed: "مغلق",
  };

  if (loading) {
    return (
      <div className="h-full w-full bg-[#F3F1ED] flex items-center justify-center">
        <div className="flex flex-col items-center gap-4">
          <div className="w-12 h-12 border-4 border-[#7895B2]/30 border-t-[#7895B2] rounded-full animate-spin"></div>
          <p className="text-[#6B7280] font-medium">جاري التحميل...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="h-full w-full bg-[#F3F1ED] overflow-auto">
      <div className="p-4 sm:p-6 md:p-8">
        <div className="max-w-[1200px] mx-auto space-y-6">
          {/* Header */}
          <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
            <div className="text-[#6B7280] text-[13px]">
              إجمالي: <span className="text-[#2F2F2F] font-bold">{items.length}</span> بلاغ
            </div>
            <div className="flex flex-row-reverse items-center gap-3">
              <div className="p-2.5 rounded-[12px] bg-[#C86E5D]/20">
                <AlertTriangle size={22} className="text-[#C86E5D]" />
              </div>
              <div className="text-right">
                <h1 className="font-sans font-bold text-[22px] sm:text-[24px] text-[#2F2F2F]">
                  البلاغات
                </h1>
                <p className="text-[13px] text-[#6B7280]">إدارة ومتابعة البلاغات المرسلة من العمال</p>
              </div>
            </div>
          </div>

          {/* Search & Filters */}
          <div className="bg-white rounded-[16px] p-4 shadow-[0_4px_20px_rgba(0,0,0,0.04)]">
            <div className="flex flex-col lg:flex-row gap-4">
              <div className="flex-1 relative">
                <Search className="absolute right-4 top-1/2 -translate-y-1/2 text-[#6B7280]" size={18} />
                <input
                  value={q}
                  onChange={(e) => setQ(e.target.value)}
                  placeholder="بحث (رقم البلاغ / عنوان / عامل / منطقة / نوع)..."
                  className="w-full h-[46px] pr-11 pl-4 bg-[#F3F1ED] rounded-[12px] text-right text-[14px] text-[#2F2F2F] placeholder:text-[#6B7280] border-0 outline-none focus:ring-2 focus:ring-[#7895B2]/30"
                />
                {q && (
                  <button
                    onClick={() => setQ("")}
                    className="absolute left-4 top-1/2 -translate-y-1/2 text-[#6B7280] hover:text-[#2F2F2F]"
                  >
                    <X size={16} />
                  </button>
                )}
              </div>

              <div className="flex items-center gap-2 flex-wrap">
                <Filter size={16} className="text-[#6B7280]" />
                {(["all", "new", "reviewing", "converted", "closed"] as FilterKey[]).map((key) => (
                  <button
                    key={key}
                    onClick={() => setFilter(key)}
                    className={`px-3 py-2 rounded-[10px] text-[13px] font-semibold transition-all border ${
                      filter === key
                        ? "bg-[#7895B2] text-white border-[#7895B2] shadow-md"
                        : "bg-[#F3F1ED] text-[#6B7280] border-[#E5E7EB] hover:bg-[#E8E6E2]"
                    }`}
                  >
                    {key === "all" ? "الكل" : statusLabels[key]}
                    <span className={`mr-2 px-1.5 py-0.5 rounded text-[11px] ${
                      filter === key ? "bg-white/20" : "bg-[#7895B2]/10"
                    }`}>
                      {counts[key]}
                    </span>
                  </button>
                ))}
              </div>
            </div>
          </div>

          {/* Error State */}
          {error && (
            <div className="bg-[#C86E5D]/10 border border-[#C86E5D]/30 rounded-[12px] p-4 text-center">
              <p className="text-[#C86E5D] font-medium">{error}</p>
            </div>
          )}

          {/* Issues List */}
          {!error && (
            <div className="space-y-3">
              {filtered.length === 0 ? (
                <div className="bg-white rounded-[16px] p-8 shadow-[0_4px_20px_rgba(0,0,0,0.04)] text-center">
                  <AlertTriangle className="mx-auto text-[#7895B2]/20 mb-4" size={48} />
                  <p className="text-[#6B7280] font-medium">لا يوجد نتائج حسب الفلتر/البحث الحالي</p>
                </div>
              ) : (
                filtered.map((item) => (
                  <div
                    key={item.id}
                    className="bg-white rounded-[16px] p-5 shadow-[0_4px_20px_rgba(0,0,0,0.04)] cursor-pointer transition-all hover:shadow-[0_8px_30px_rgba(0,0,0,0.08)]"
                    onClick={() => openIssue(item.displayId)}
                  >
                    <div className="flex items-start justify-between gap-4">
                      {/* Right Side - Main Info */}
                      <div className="flex-1 min-w-0 text-right">
                        <div className="flex items-start justify-between gap-3 mb-3">
                          <div>
                            <h3 className="text-[#2F2F2F] font-bold text-[15px] truncate">
                              {item.title}
                            </h3>
                            <p className="text-[#6B7280] text-[12px] mt-1">
                              #{item.displayId} • {item.reporterName} • {item.zone}
                            </p>
                          </div>
                          <div className="flex items-center gap-2 shrink-0">
                            <span className={`px-2.5 py-1 rounded-full text-[10px] font-bold border ${statusColors[item.status]}`}>
                              {statusLabels[item.status]}
                            </span>
                            <span className={`px-2.5 py-1 rounded-full text-[10px] font-bold border ${severityColors[item.severity]}`}>
                              {severityLabels[item.severity]}
                            </span>
                          </div>
                        </div>

                        {/* Meta Info Grid */}
                        <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
                          <div className="bg-[#F3F1ED] rounded-[10px] px-3 py-2 border border-[#E5E7EB]">
                            <p className="text-[#6B7280] text-[11px]">نوع المشكلة</p>
                            <p className="text-[#2F2F2F] text-[13px] font-semibold">{item.type}</p>
                          </div>
                          <div className="bg-[#F3F1ED] rounded-[10px] px-3 py-2 border border-[#E5E7EB]">
                            <p className="text-[#6B7280] text-[11px]">وصف الموقع</p>
                            <p className="text-[#2F2F2F] text-[13px] font-semibold truncate">{item.locationText}</p>
                          </div>
                          <div className="bg-[#F3F1ED] rounded-[10px] px-3 py-2 border border-[#E5E7EB]">
                            <p className="text-[#6B7280] text-[11px]">تاريخ الإبلاغ</p>
                            <p className="text-[#2F2F2F] text-[13px] font-semibold">{item.reportedAt}</p>
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
                          className="p-2 rounded-[10px] bg-[#7895B2]/10 text-[#7895B2] hover:bg-[#7895B2]/20 transition-colors border border-[#7895B2]/20"
                          title="عرض التفاصيل"
                        >
                          <Eye size={18} />
                        </button>
                        <button
                          onClick={(e) => handleDelete(item.id, e)}
                          disabled={deleting === item.id}
                          className="p-2 rounded-[10px] bg-[#C86E5D]/10 text-[#C86E5D] hover:bg-[#C86E5D]/20 transition-colors border border-[#C86E5D]/20 disabled:opacity-50"
                          title="حذف البلاغ"
                        >
                          {deleting === item.id ? (
                            <div className="w-[18px] h-[18px] border-2 border-[#C86E5D] border-t-transparent rounded-full animate-spin" />
                          ) : (
                            <Trash2 size={18} />
                          )}
                        </button>
                      </div>
                    </div>
                  </div>
                ))
              )}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
