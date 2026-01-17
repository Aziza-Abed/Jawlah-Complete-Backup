import React, { useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";

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
  reportedAt: string; // "2026-01-17 09:58"
  status: IssueStatus;
};

type FilterKey = "all" | "new" | "reviewing" | "converted" | "closed";

export default function Issues() {
  const navigate = useNavigate();

  const data: IssueListItem[] = useMemo(
    () => [
      {
        id: "i-77",
        title: "تسريب مياه قرب دوار البلدية",
        type: "تسريب مياه",
        severity: "high",
        reporterName: "أبو عمار",
        zone: "المنطقة 4",
        locationText: "قرب دوار البلدية - بجانب الرصيف",
        reportedAt: "2026-01-17 09:58",
        status: "reviewing",
      },
      {
        id: "i-78",
        title: "حفرة خطرة في شارع المدارس",
        type: "حفرة",
        severity: "critical",
        reporterName: "محمد أحمد",
        zone: "المنطقة 2",
        locationText: "شارع المدارس - مقابل المدخل الرئيسي",
        reportedAt: "2026-01-17 10:12",
        status: "new",
      },
      {
        id: "i-79",
        title: "تلف حاوية نفايات",
        type: "نفايات",
        severity: "medium",
        reporterName: "محمود",
        zone: "المنطقة 1",
        locationText: "قرب دوار السوق",
        reportedAt: "2026-01-16 14:30",
        status: "converted",
      },
      {
        id: "i-80",
        title: "عائق على الرصيف",
        type: "عوائق",
        severity: "low",
        reporterName: "أحمد",
        zone: "المنطقة 3",
        locationText: "شارع القدس - بجانب المخبز",
        reportedAt: "2026-01-15 11:05",
        status: "closed",
      },
    ],
    []
  );

  const [items, setItems] = useState<IssueListItem[]>(data);
  const [filter, setFilter] = useState<FilterKey>("all");
  const [q, setQ] = useState("");

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
        norm(x.id).includes(nq) ||
        norm(x.title).includes(nq) ||
        norm(x.reporterName).includes(nq) ||
        norm(x.zone).includes(nq) ||
        norm(x.type).includes(nq)
      );
    });
  }, [items, filter, q]);

  const clearClosed = () => {
    setItems((prev) => prev.filter((x) => x.status !== "closed"));
  };

  const openIssue = (id: string) => {
    navigate(`/issues/${id}`);
  };

  return (
    <div className="h-full w-full bg-[#D9D9D9] overflow-auto">
      <div className="p-4 sm:p-6 md:p-8">
        <div className="max-w-[1100px] mx-auto">
          <div className="flex items-center justify-between gap-3">
            <h1 className="text-right font-sans font-semibold text-[20px] sm:text-[22px] text-[#2F2F2F]">
              البلاغات
            </h1>

            <button
              type="button"
              onClick={clearClosed}
              className="h-[38px] px-3 rounded-[10px] bg-white border border-black/10 text-[#2F2F2F] font-sans font-semibold text-[13px] hover:opacity-95"
            >
              مسح المُغلقة
            </button>
          </div>

          <div className="mt-4 bg-[#F3F1ED] rounded-[14px] border border-black/10 shadow-[0_2px_0_rgba(0,0,0,0.08)] p-3 sm:p-4">
            <div className="flex flex-col md:flex-row md:items-center gap-3">
              <div className="flex-1">
                <div className="relative">
                  <input
                    value={q}
                    onChange={(e) => setQ(e.target.value)}
                    placeholder="بحث (رقم البلاغ / عنوان / عامل / منطقة / نوع)..."
                    className="w-full h-[44px] rounded-[12px] bg-white border border-black/10 px-4 text-right outline-none focus:ring-2 focus:ring-black/10"
                  />
                </div>
              </div>

              <div className="flex items-center gap-2 flex-wrap justify-end">
                <FilterChip
                  active={filter === "all"}
                  onClick={() => setFilter("all")}
                  label="الكل"
                  count={counts.all}
                />
                <FilterChip
                  active={filter === "new"}
                  onClick={() => setFilter("new")}
                  label="جديد"
                  count={counts.new}
                />
                <FilterChip
                  active={filter === "reviewing"}
                  onClick={() => setFilter("reviewing")}
                  label="قيد المراجعة"
                  count={counts.reviewing}
                />
                <FilterChip
                  active={filter === "converted"}
                  onClick={() => setFilter("converted")}
                  label="تم تحويله لمهمة"
                  count={counts.converted}
                />
                <FilterChip
                  active={filter === "closed"}
                  onClick={() => setFilter("closed")}
                  label="مغلق"
                  count={counts.closed}
                />
              </div>
            </div>
          </div>

          <div className="mt-4 space-y-3">
            {filtered.length === 0 ? (
              <div className="bg-white rounded-[14px] border border-black/10 p-5 text-right text-[#6B7280]">
                لا يوجد نتائج حسب الفلتر/البحث الحالي.
              </div>
            ) : (
              filtered.map((it) => (
                <button
                  key={it.id}
                  type="button"
                  onClick={() => openIssue(it.id)}
                  className={[
                    "w-full text-right",
                    "bg-[#F3F1ED] rounded-[14px] border border-black/10 shadow-[0_2px_0_rgba(0,0,0,0.06)]",
                    "p-4 sm:p-5",
                    "hover:opacity-95 transition",
                  ].join(" ")}
                >
                  <div className="flex items-start justify-between gap-3">
                    <div className="flex items-center gap-2">
                      <span className="text-[12px] text-[#6B7280]">{it.reportedAt}</span>
                    </div>

                    <div className="flex-1">
                      <div className="flex items-start justify-between gap-3">
                        <div className="min-w-0">
                          <div className="text-[14px] sm:text-[15px] font-sans font-semibold text-[#2F2F2F] truncate">
                            {it.title}
                          </div>
                          <div className="mt-1 text-[12px] text-[#6B7280]">
                            #{it.id} • {it.reporterName} • {it.zone}
                          </div>
                        </div>

                        <div className="flex items-center gap-2 shrink-0">
                          <StatusBadge status={it.status} />
                          <SeverityBadge severity={it.severity} />
                        </div>
                      </div>

                      <div className="mt-3 grid grid-cols-1 sm:grid-cols-3 gap-2">
                        <MetaPill label="نوع المشكلة" value={it.type} />
                        <MetaPill label="وصف الموقع" value={it.locationText} />
                        <MetaPill label="المنطقة" value={it.zone} />
                      </div>

                      <div className="mt-3 text-[12px] text-[#60778E] font-sans font-semibold">
                        اضغط لعرض التفاصيل
                      </div>
                    </div>
                  </div>
                </button>
              ))
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

function FilterChip({
  active,
  onClick,
  label,
  count,
}: {
  active: boolean;
  onClick: () => void;
  label: string;
  count: number;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={[
        "h-[34px] px-3 rounded-[10px] font-sans font-semibold text-[13px] border flex items-center gap-2",
        active
          ? "bg-[#60778E] text-white border-black/10"
          : "bg-white text-[#2F2F2F] border-black/10 hover:opacity-95",
      ].join(" ")}
    >
      <span>{label}</span>
      <span
        className={[
          "min-w-[20px] h-[20px] px-1 rounded-full text-[12px] font-bold grid place-items-center",
          active ? "bg-white/20 text-white" : "bg-[#F3F1ED] text-[#2F2F2F]",
        ].join(" ")}
      >
        {count}
      </span>
    </button>
  );
}

function MetaPill({ label, value }: { label: string; value: string }) {
  return (
    <div className="bg-white rounded-[12px] border border-black/10 px-3 py-2">
      <div className="text-[11px] text-[#6B7280] font-sans font-semibold text-right">
        {label}
      </div>
      <div className="mt-1 text-[12px] text-[#2F2F2F] font-sans font-semibold text-right line-clamp-1">
        {value}
      </div>
    </div>
  );
}

function SeverityBadge({ severity }: { severity: "low" | "medium" | "high" | "critical" }) {
  const map: Record<typeof severity, { label: string; bg: string; text: string }> = {
    low: { label: "منخفضة", bg: "#E5E7EB", text: "#2F2F2F" },
    medium: { label: "متوسطة", bg: "#F3E7C8", text: "#2F2F2F" },
    high: { label: "عالية", bg: "#F2D3CD", text: "#2F2F2F" },
    critical: { label: "حرجة", bg: "#C86E5D", text: "#FFFFFF" },
  };
  const s = map[severity];
  return (
    <span
      className="text-[12px] px-3 py-1.5 rounded-full border border-black/10 font-sans font-semibold"
      style={{ backgroundColor: s.bg, color: s.text }}
    >
      {s.label}
    </span>
  );
}

function StatusBadge({ status }: { status: "new" | "reviewing" | "converted" | "closed" }) {
  const map: Record<typeof status, { label: string; bg: string; text: string }> = {
    new: { label: "جديد", bg: "#E5E7EB", text: "#2F2F2F" },
    reviewing: { label: "قيد المراجعة", bg: "#F3F1ED", text: "#2F2F2F" },
    converted: { label: "تم تحويله لمهمة", bg: "#8FA36A", text: "#FFFFFF" },
    closed: { label: "مغلق", bg: "#6B7280", text: "#FFFFFF" },
  };
  const s = map[status];
  return (
    <span
      className="text-[12px] px-3 py-1.5 rounded-full border border-black/10 font-sans font-semibold"
      style={{ backgroundColor: s.bg, color: s.text }}
    >
      {s.label}
    </span>
  );
}
