import React, { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";

type Severity = "low" | "medium" | "high" | "critical";
type IssueStatus = "new" | "reviewing" | "converted" | "forwarded" | "rejected" | "closed";

type IssueDTO = {
  id: string; // رقم البلاغ
  title: string; // عنوان البلاغ
  description: string; // وصف المشكلة

  type: string; // نوع المشكلة
  severity: Severity; // مستوى الخطورة

  reporterName: string; // اسم العامل المُبلّغ
  zone: string; // Zone
  locationText: string; // وصف الموقع
  gps?: { lat: number; lng: number }; // optional

  reportedAt: string; // تاريخ ووقت الإبلاغ
  images: string[]; // روابط الصور

  status: IssueStatus;
};

type Option = { id: string; name: string };

export default function IssueDetails() {
  const { id } = useParams(); // /issues/:id
  const navigate = useNavigate();

  // TODO: Replace with API call GET /issues/:id
  const mock: IssueDTO = useMemo(
    () => ({
      id: (id as string) || "i-77",
      title: "بلاغ: تسريب مياه",
      description:
        "العامل أبلغ عن تسريب مياه قرب دوار البلدية. يحتاج متابعة من القسم المختص أو تحويله لمهمة لعامل.",
      type: "تسريب مياه",
      severity: "high",
      reporterName: "أبو عمار",
      zone: "المنطقة 4",
      locationText: "قرب دوار البلدية - بجانب الرصيف",
      gps: { lat: 31.903, lng: 35.211 }, // placeholder
      reportedAt: "2026-01-17 09:58",
      images: [],
      status: "reviewing",
    }),
    [id]
  );

  const [issue, setIssue] = useState<IssueDTO | null>(null);

  // Modals
  const [forwardOpen, setForwardOpen] = useState(false);
  const [rejectOpen, setRejectOpen] = useState(false);

  const [selectedSupervisorId, setSelectedSupervisorId] = useState<string>("");
  const [note, setNote] = useState<string>("");

  const [rejectReason, setRejectReason] = useState<string>("");

  // Mock supervisors (later from API)
  const supervisors: Option[] = useMemo(
    () => [
      { id: "s-1", name: "مشرف 1" },
      { id: "s-2", name: "مشرف 2" },
    ],
    []
  );

  useEffect(() => {
    setIssue(mock);
  }, [mock]);

  const onConvertToTask = () => {
    if (!issue) return;

    navigate("/tasks/new", {
      state: {
        fromIssue: {
          issueId: issue.id,
          title: issue.title.replace("بلاغ:", "").trim(),
          description: issue.description,
          zone: issue.zone,
          locationText: issue.locationText,
          gps: issue.gps,
          severity: issue.severity,
          images: issue.images,
          type: issue.type,
          reporterName: issue.reporterName,
          reportedAt: issue.reportedAt,
        },
      },
    });

    // TODO backend:
    // POST /issues/:id/convert-to-task (or POST /tasks/from-issue)
  };

  const submitForwardToSupervisor = async () => {
    if (!issue) return;
    if (!selectedSupervisorId) return;

    // UI optimistic
    setIssue({ ...issue, status: "forwarded" });
    setForwardOpen(false);
    setNote("");
    setSelectedSupervisorId("");

    // TODO backend:
    // POST /issues/:id/forward  body: { supervisorId, note }
  };

  const submitReject = async () => {
    if (!issue) return;
    if (!rejectReason.trim()) return;

    // UI optimistic
    setIssue({ ...issue, status: "rejected" });
    setRejectOpen(false);
    setRejectReason("");

    // TODO backend:
    // POST /issues/:id/reject  body: { reason }
  };

  if (!issue) {
    return (
      <div className="h-full w-full bg-[#D9D9D9] grid place-items-center">
        <div className="text-[#C86E5D] font-sans font-semibold">فشل في تحميل البيانات</div>
      </div>
    );
  }

  return (
    <div className="h-full w-full bg-[#D9D9D9] overflow-auto">
      <div className="p-4 sm:p-6 md:p-8">
        <div className="max-w-[1100px] mx-auto">
          {/* Header: title on RIGHT, no back button */}
         <div className="flex items-start justify-between gap-3" dir="rtl">
  {/* RIGHT: Title */}
  <div className="text-right">
    <h1 className="font-sans font-semibold text-[20px] sm:text-[22px] text-[#2F2F2F]">
      تفاصيل البلاغ
    </h1>
    <div className="mt-1 text-[12px] text-[#6B7280]">
      رقم البلاغ: #{issue.id} • {issue.reportedAt}
    </div>
  </div>

  {/* LEFT: Status */}
  <StatusBadge status={issue.status} />
</div>

          <Card className="mt-4">
            {/* Title */}
          <div className="flex items-center justify-between gap-3" dir="rtl">
  {/* RIGHT: Issue title */}
  <div className="text-right font-sans font-semibold text-[18px] text-[#2F2F2F]">
    {issue.title}
  </div>

  {/* LEFT: Severity */}
  <SeverityBadge severity={issue.severity} />
</div>


            {/* Meta */}
            <div className="mt-3 grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
              <InfoBox label="اسم المُبلّغ" value={issue.reporterName} />
              <InfoBox label="المنطقة (Zone)" value={issue.zone} />
              <InfoBox label="نوع المشكلة" value={issue.type} />
              <InfoBox label="مستوى الخطورة" value={severityText(issue.severity)} />
            </div>

            {/* Description */}
            <div className="mt-4">
              <div className="text-right text-[13px] text-[#6B7280] font-sans font-semibold">
                وصف المشكلة
              </div>
              <div className="mt-2 bg-white rounded-[12px] border border-black/10 p-4 text-right text-[14px] text-[#2F2F2F] leading-relaxed">
                {issue.description}
              </div>
            </div>

            {/* Location */}
            <div className="mt-4 grid grid-cols-1 lg:grid-cols-2 gap-3">
              <div className="bg-white rounded-[12px] border border-black/10 p-4">
                <div className="text-right text-[13px] text-[#6B7280] font-sans font-semibold">
                  الموقع (وصف + GPS)
                </div>
                <div className="mt-2 text-right text-[14px] text-[#2F2F2F] font-sans font-semibold">
                  {issue.locationText}
                </div>

                {issue.gps ? (
                  <div className="mt-2 text-right text-[12px] text-[#6B7280]">
                    GPS: {issue.gps.lat}, {issue.gps.lng}
                  </div>
                ) : (
                  <div className="mt-2 text-right text-[12px] text-[#6B7280]">لا يوجد GPS</div>
                )}
              </div>

              <div className="bg-white rounded-[12px] border border-black/10 p-4">
                <div className="text-right text-[13px] text-[#6B7280] font-sans font-semibold">
                  خريطة (Placeholder)
                </div>
                <div className="mt-2 h-[140px] rounded-[10px] bg-[#E9E6E0] border border-black/5 grid place-items-center text-[#6B7280] text-[12px]">
                  لاحقاً: خريطة مصغّرة + Marker لموقع البلاغ
                </div>
              </div>
            </div>

            {/* Images */}
            <div className="mt-4">
              <div className="text-right text-[13px] text-[#6B7280] font-sans font-semibold">
                الصور ({issue.images.length})
              </div>

              {issue.images.length === 0 ? (
                <div className="mt-2 bg-white rounded-[12px] border border-black/10 p-4 text-right text-[#6B7280] text-[13px]">
                  لا يوجد صور حالياً (سيتم عرض الصور الحقيقية من البلاغ لاحقاً).
                </div>
              ) : (
                <div className="mt-2 grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-3">
                  {issue.images.map((src, idx) => (
                    <button
                      key={src + idx}
                      type="button"
                      className="bg-white rounded-[12px] border border-black/10 overflow-hidden hover:opacity-95 transition"
                      onClick={() => window.open(src, "_blank")}
                      aria-label={`فتح الصورة ${idx + 1}`}
                    >
                      <img src={src} alt={`issue-${idx + 1}`} className="w-full h-[110px] object-cover" />
                    </button>
                  ))}
                </div>
              )}
            </div>

            {/* Actions */}
            <div className="mt-5 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
              <div className="flex flex-col sm:flex-row gap-2 w-full sm:w-auto">
                <button
                  type="button"
                  onClick={() => setForwardOpen(true)}
                  className="h-[44px] px-4 rounded-[12px] bg-white border border-black/10 text-[#2F2F2F] font-sans font-semibold text-[14px] hover:opacity-95"
                >
                  تحويل لمشرف آخر
                </button>

                <button
                  type="button"
                  onClick={() => setRejectOpen(true)}
                  className="h-[44px] px-4 rounded-[12px] bg-white border border-[#C86E5D]/40 text-[#C86E5D] font-sans font-semibold text-[14px] hover:opacity-95"
                >
                  رفض البلاغ
                </button>
              </div>

              <button
                type="button"
                onClick={onConvertToTask}
                className="h-[44px] px-6 rounded-[12px] bg-[#60778E] text-white font-sans font-semibold text-[14px] shadow-[0_2px_0_rgba(0,0,0,0.15)] hover:opacity-95 w-full sm:w-auto"
              >
                تحويل البلاغ إلى مهمة
              </button>
            </div>
          </Card>

          {/* Forward Modal */}
          <Modal
            open={forwardOpen}
            title="تحويل البلاغ لمشرف آخر"
            onClose={() => {
              setForwardOpen(false);
              setNote("");
              setSelectedSupervisorId("");
            }}
            footer={
              <>
                <button
                  type="button"
                  className="h-[40px] px-4 rounded-[10px] bg-white border border-black/10 font-sans font-semibold"
                  onClick={() => {
                    setForwardOpen(false);
                    setNote("");
                    setSelectedSupervisorId("");
                  }}
                >
                  إلغاء
                </button>
                <button
                  type="button"
                  className="h-[40px] px-4 rounded-[10px] bg-[#60778E] text-white font-sans font-semibold disabled:opacity-50"
                  onClick={submitForwardToSupervisor}
                  disabled={!selectedSupervisorId}
                >
                  تأكيد التحويل
                </button>
              </>
            }
          >
            <div className="text-right text-[13px] text-[#6B7280] font-sans font-semibold">اختاري المشرف</div>
            <select
              className="mt-2 w-full h-[44px] rounded-[12px] bg-white border border-black/10 px-3 text-right"
              value={selectedSupervisorId}
              onChange={(e) => setSelectedSupervisorId(e.target.value)}
            >
              <option value="">— اختر مشرف —</option>
              {supervisors.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.name}
                </option>
              ))}
            </select>

            <div className="mt-4 text-right text-[13px] text-[#6B7280] font-sans font-semibold">ملاحظة (اختياري)</div>
            <textarea
              className="mt-2 w-full min-h-[90px] rounded-[12px] bg-white border border-black/10 p-3 text-right"
              value={note}
              onChange={(e) => setNote(e.target.value)}
              placeholder="اكتبي سبب التحويل أو تفاصيل إضافية..."
            />
          </Modal>

          {/* Reject Modal */}
          <Modal
            open={rejectOpen}
            title="رفض البلاغ"
            onClose={() => {
              setRejectOpen(false);
              setRejectReason("");
            }}
            footer={
              <>
                <button
                  type="button"
                  className="h-[40px] px-4 rounded-[10px] bg-white border border-black/10 font-sans font-semibold"
                  onClick={() => {
                    setRejectOpen(false);
                    setRejectReason("");
                  }}
                >
                  إلغاء
                </button>
                <button
                  type="button"
                  className="h-[40px] px-4 rounded-[10px] bg-[#C86E5D] text-white font-sans font-semibold disabled:opacity-50"
                  onClick={submitReject}
                  disabled={!rejectReason.trim()}
                >
                  تأكيد الرفض
                </button>
              </>
            }
          >
            <div className="text-right text-[13px] text-[#6B7280] font-sans font-semibold">سبب الرفض</div>
            <textarea
              className="mt-2 w-full min-h-[110px] rounded-[12px] bg-white border border-black/10 p-3 text-right"
              value={rejectReason}
              onChange={(e) => setRejectReason(e.target.value)}
              placeholder="مثال: بلاغ مكرر / معلومات ناقصة / خارج نطاق البلدية..."
            />
          </Modal>

          <div className="mt-6 text-right text-[12px] text-[#6B7280]">
            {/* TODO backend:
              GET /issues/:id
              POST /issues/:id/convert-to-task
              POST /issues/:id/forward
              POST /issues/:id/reject
            */}
          </div>
        </div>
      </div>
    </div>
  );
}

/* ---------- Helpers ---------- */

function severityText(sev: Severity) {
  switch (sev) {
    case "low":
      return "منخفضة";
    case "medium":
      return "متوسطة";
    case "high":
      return "عالية";
    case "critical":
      return "حرجة";
    default:
      return "";
  }
}

/* ---------- Small UI ---------- */

function Card({ children, className = "" }: { children: React.ReactNode; className?: string }) {
  return (
    <div
      className={[
        "bg-[#F3F1ED] rounded-[16px] shadow-[0_2px_0_rgba(0,0,0,0.08)] border border-black/10 p-4 sm:p-5",
        className,
      ].join(" ")}
    >
      {children}
    </div>
  );
}

function InfoBox({ label, value }: { label: string; value: string }) {
  return (
    <div className="bg-white rounded-[12px] border border-black/10 p-4">
      <div className="text-right text-[12px] text-[#6B7280] font-sans font-semibold">{label}</div>
      <div className="mt-2 text-right text-[15px] text-[#2F2F2F] font-sans font-semibold">{value}</div>
    </div>
  );
}

function SeverityBadge({ severity }: { severity: Severity }) {
  const map: Record<Severity, { label: string; bg: string; text: string }> = {
    low: { label: "خطورة منخفضة", bg: "#E5E7EB", text: "#2F2F2F" },
    medium: { label: "خطورة متوسطة", bg: "#F3E7C8", text: "#2F2F2F" },
    high: { label: "خطورة عالية", bg: "#F2D3CD", text: "#2F2F2F" },
    critical: { label: "حرج", bg: "#C86E5D", text: "#FFFFFF" },
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

function StatusBadge({ status }: { status: IssueStatus }) {
  const map: Record<IssueStatus, { label: string; bg: string; text: string }> = {
    new: { label: "جديد", bg: "#E5E7EB", text: "#2F2F2F" },
    reviewing: { label: "قيد المراجعة", bg: "#F3F1ED", text: "#2F2F2F" },
    converted: { label: "تم تحويله لمهمة", bg: "#8FA36A", text: "#FFFFFF" },
    forwarded: { label: "تم تحويله لمشرف", bg: "#60778E", text: "#FFFFFF" },
    rejected: { label: "مرفوض", bg: "#C86E5D", text: "#FFFFFF" },
    closed: { label: "مغلق", bg: "#6B7280", text: "#FFFFFF" },
  };
  const s = map[status];
  return (
    <span
      className="text-[12px] px-3 py-2 rounded-full border border-black/10 font-sans font-semibold"
      style={{ backgroundColor: s.bg, color: s.text }}
    >
      {s.label}
    </span>
  );
}

function Modal({
  open,
  title,
  children,
  footer,
  onClose,
}: {
  open: boolean;
  title: string;
  children: React.ReactNode;
  footer?: React.ReactNode;
  onClose: () => void;
}) {
  if (!open) return null;

  return (
    <div className="fixed inset-0 z-[60]">
      <button
        type="button"
        className="absolute inset-0 bg-black/40"
        onClick={onClose}
        aria-label="إغلاق"
      />

      <div className="absolute inset-0 p-4 grid place-items-center">
        <div className="w-full max-w-[520px] bg-[#F3F1ED] rounded-[16px] border border-black/10 shadow-[0_12px_30px_rgba(0,0,0,0.18)] overflow-hidden">
          <div className="px-4 py-3 border-b border-black/10 flex items-center justify-between">
            <button
              type="button"
              onClick={onClose}
              className="h-[34px] px-3 rounded-[10px] bg-white border border-black/10 font-sans font-semibold text-[13px]"
            >
              إغلاق
            </button>
            <div className="text-right font-sans font-semibold text-[16px] text-[#2F2F2F]">{title}</div>
          </div>

          <div className="p-4">{children}</div>

          {footer ? (
            <div className="px-4 py-3 border-t border-black/10 flex items-center justify-end gap-2">
              {footer}
            </div>
          ) : null}
        </div>
      </div>
    </div>
  );
}
