import React, { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { MapContainer, TileLayer, Marker, Popup } from "react-leaflet";
import L from "leaflet";
import "leaflet/dist/leaflet.css";
import { getIssue, updateIssueStatus, forwardIssue, createTaskFromIssue } from "../api/issues";
import { getMyWorkers } from "../api/users";
import type { UserResponse } from "../types/user";
import { getDepartments } from "../api/departments";
import type { IssueResponse } from "../types/issue";
import type { Department } from "../api/departments";
import { mapSeverity, mapStatus, mapTypeToArabic, type Severity, type DisplayIssueStatus } from "../utils/issueDisplay";

const IssueLocationIcon = L.divIcon({
  className: "custom-marker",
  html: `<div class="relative flex items-center justify-center w-10 h-10">
    <div class="absolute w-full h-full bg-red-500 rounded-full animate-ping opacity-30"></div>
    <div class="relative w-6 h-6 bg-red-600 rounded-full border-3 border-white shadow-lg flex items-center justify-center">
      <svg width="14" height="14" viewBox="0 0 24 24" fill="white" stroke="white" stroke-width="2">
        <path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z"></path>
        <circle cx="12" cy="10" r="3" fill="white"></circle>
      </svg>
    </div>
  </div>`,
  iconSize: [40, 40],
  iconAnchor: [20, 20]
});

type IssueDTO = {
  id: string; // رقم البلاغ
  title: string; // عنوان البلاغ
  description: string; // وصف المشكلة

  type: string; // نوع المشكلة
  severity: Severity;

  reporterName: string; // اسم العامل المُبلّغ
  zone: string; // Zone
  locationText: string; // وصف الموقع
  gps?: { lat: number; lng: number }; // optional

  reportedAt: string; // تاريخ ووقت الإبلاغ
  images: string[]; // روابط الصور

  status: DisplayIssueStatus;
};



// Convert backend IssueResponse to frontend IssueDTO
const mapIssueResponseToDTO = (issue: IssueResponse): IssueDTO => {
  const date = new Date(issue.reportedAt);
  const formattedDate = `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")} ${String(date.getHours()).padStart(2, "0")}:${String(date.getMinutes()).padStart(2, "0")}`;

  return {
    id: `i-${issue.issueId}`,
    title: issue.title,
    description: issue.description,
    type: mapTypeToArabic(issue.type),
    severity: mapSeverity(issue.severity),
    reporterName: issue.reportedByName || "غير محدد",
    zone: issue.zoneName || "غير محدد",
    locationText: issue.locationDescription || "غير محدد",
    gps: issue.latitude && issue.longitude ? { lat: issue.latitude, lng: issue.longitude } : undefined,
    reportedAt: formattedDate,
    images: issue.photos || [],
    status: mapStatus(issue.status),
  };
};

export default function IssueDetails() {
  const { id } = useParams(); // /issues/:id
  const [issue, setIssue] = useState<IssueDTO | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const [departments, setDepartments] = useState<Department[]>([]);
  const [rawIssue, setRawIssue] = useState<IssueResponse | null>(null);

  // Modals
  const [forwardOpen, setForwardOpen] = useState(false);
  const [rejectOpen, setRejectOpen] = useState(false);
  const [convertOpen, setConvertOpen] = useState(false);

  // Convert modal state
  const [workers, setWorkers] = useState<UserResponse[]>([]);
  const [convertAssignee, setConvertAssignee] = useState("");
  const [convertPriority, setConvertPriority] = useState<"Low" | "Medium" | "High" | "Urgent">("Medium");
  const [convertDueDate, setConvertDueDate] = useState("");
  const [convertRequiresPhoto, setConvertRequiresPhoto] = useState(true);
  const [convertSubmitting, setConvertSubmitting] = useState(false);
  const [convertedTaskId, setConvertedTaskId] = useState<number | null>(null);

  // Fetch issue details and departments
  useEffect(() => {
    const fetchInitialData = async () => {
      try {
        setLoading(true);
        setError("");

        const numericId = id?.startsWith("i-") ? id.slice(2) : id;
        if (!numericId) throw new Error("Invalid issue ID");

        const [issueData, deptData, workerData] = await Promise.all([
          getIssue(Number(numericId)),
          getDepartments(true).catch(() => []),
          getMyWorkers().catch(() => [])
        ]);

        const dto = mapIssueResponseToDTO(issueData);
        setIssue(dto);
        setRawIssue(issueData);
        setDepartments(deptData);
        setWorkers(workerData);
      } catch (err) {
        console.error("Failed to fetch data:", err);
        setError("فشل تحميل بيانات الصفحة");
      } finally {
        setLoading(false);
      }
    };
    fetchInitialData();
  }, [id]);

  const [selectedDepartmentId, setSelectedDepartmentId] = useState<string>("");
  const [note, setNote] = useState<string>("");
  const [rejectReason, setRejectReason] = useState<string>("");

  const submitConvertToTask = async () => {
    if (!issue || !convertAssignee) return;

    const numericId = id?.startsWith("i-") ? id.slice(2) : id;
    if (!numericId) return;

    try {
      setConvertSubmitting(true);
      const result = await createTaskFromIssue(Number(numericId), {
        assignedToUserId: Number(convertAssignee),
        priority: convertPriority,
        dueDate: convertDueDate || undefined,
        requiresPhotoProof: convertRequiresPhoto,
      });

      setConvertedTaskId(result.taskId);
      setIssue({ ...issue, status: "converted" });
      setConvertOpen(false);
    } catch (err) {
      console.error("Failed to convert issue to task:", err);
      alert("فشل في تحويل البلاغ إلى مهمة");
    } finally {
      setConvertSubmitting(false);
    }
  };

  const submitForwardToDepartment = async () => {
    if (!issue) return;
    if (!selectedDepartmentId) return;

    try {
      const numericId = id?.startsWith("i-") ? id.slice(2) : id;
      if (!numericId) return;

      const updatedIssue = await forwardIssue(Number(numericId), {
        departmentId: Number(selectedDepartmentId),
        notes: note || undefined,
      });

      const dto = mapIssueResponseToDTO(updatedIssue);
      setIssue({ ...dto, status: "forwarded" });
      setRawIssue(updatedIssue);
      setForwardOpen(false);
      setNote("");
      setSelectedDepartmentId("");
    } catch (err) {
      console.error("Failed to forward issue:", err);
      alert("فشل في تحويل البلاغ");
    }
  };

  const submitReject = async () => {
    if (!issue) return;
    if (!rejectReason.trim()) return;

    try {
      const numericId = id?.startsWith("i-") ? id.slice(2) : id;
      if (!numericId) return;

      // Update status to Resolved when rejecting
      await updateIssueStatus(Number(numericId), {
        status: "Resolved",
        resolutionNotes: rejectReason
      });

      setIssue({ ...issue, status: "closed" });
      setRejectOpen(false);
      setRejectReason("");
    } catch (err) {
      console.error("Failed to reject issue:", err);
      alert("فشل في رفض البلاغ");
    }
  };

  if (loading) {
    return (
      <div className="h-full w-full bg-[#F3F1ED] flex items-center justify-center">
        <div className="flex flex-col items-center gap-4">
          <div className="w-12 h-12 border-4 border-[#7895B2]/30 border-t-[#7895B2] rounded-full animate-spin"></div>
          <p className="text-[#6B7280] font-medium">جاري تحميل البيانات...</p>
        </div>
      </div>
    );
  }

  if (!issue) {
    return (
      <div className="h-full w-full bg-[#F3F1ED] grid place-items-center">
        <div className="text-[#C86E5D] font-sans font-semibold">{error || "فشل في تحميل البيانات"}</div>
      </div>
    );
  }

  return (
    <div className="h-full w-full bg-[#F3F1ED] overflow-auto">
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
                  موقع البلاغ على الخريطة
                </div>
                {issue.gps ? (
                  <div className="mt-2 h-[180px] rounded-[10px] overflow-hidden border border-black/5">
                    <MapContainer
                      center={[issue.gps.lat, issue.gps.lng]}
                      zoom={15}
                      className="w-full h-full"
                      zoomControl={false}
                    >
                      <TileLayer
                        attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
                        url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
                      />
                      <Marker position={[issue.gps.lat, issue.gps.lng]} icon={IssueLocationIcon}>
                        <Popup>
                          <div dir="rtl" className="text-right font-sans p-1">
                            <div className="font-bold text-[#2F2F2F] text-sm mb-1">موقع البلاغ</div>
                            <div className="text-[10px] text-[#6B7280]">
                              {issue.gps.lat.toFixed(5)}, {issue.gps.lng.toFixed(5)}
                            </div>
                            {issue.locationText && issue.locationText !== "غير محدد" && (
                              <div className="text-[10px] text-[#6B7280] mt-1">{issue.locationText}</div>
                            )}
                          </div>
                        </Popup>
                      </Marker>
                    </MapContainer>
                  </div>
                ) : (
                  <div className="mt-2 h-[140px] rounded-[10px] bg-[#E9E6E0] border border-black/5 grid place-items-center text-[#6B7280] text-[12px]">
                    لا يوجد موقع GPS متاح لهذا البلاغ
                  </div>
                )}
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

            {/* forwarding info */}
            {rawIssue?.forwardedToDepartmentName && (
              <div className="mt-4 bg-[#7895B2]/10 rounded-[12px] border border-[#7895B2]/20 p-4">
                <div className="text-right text-[13px] text-[#7895B2] font-sans font-semibold mb-2">
                  معلومات التحويل
                </div>
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                  <InfoBox label="القسم المحوّل إليه" value={rawIssue.forwardedToDepartmentName} />
                  <InfoBox label="تاريخ التحويل" value={rawIssue.forwardedAt ? new Date(rawIssue.forwardedAt).toLocaleString('ar-PS') : 'غير محدد'} />
                </div>
                {rawIssue.forwardingNotes && (
                  <div className="mt-2 bg-white rounded-[12px] border border-black/10 p-3 text-right text-[13px] text-[#2F2F2F]">
                    {rawIssue.forwardingNotes}
                  </div>
                )}
              </div>
            )}

            {/* Actions */}
            <div className="mt-5 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
              <div className="flex flex-col sm:flex-row gap-2 w-full sm:w-auto">
                <button
                  type="button"
                  onClick={() => setForwardOpen(true)}
                  className="h-[44px] px-4 rounded-[12px] bg-white border border-black/10 text-[#2F2F2F] font-sans font-semibold text-[14px] hover:opacity-95"
                >
                  تحويل لقسم آخر
                </button>

                <button
                  type="button"
                  onClick={() => setRejectOpen(true)}
                  className="h-[44px] px-4 rounded-[12px] bg-white border border-[#C86E5D]/40 text-[#C86E5D] font-sans font-semibold text-[14px] hover:opacity-95"
                >
                  رفض البلاغ
                </button>
              </div>

              {issue.status === "converted" ? (
                <div className="h-[44px] px-6 rounded-[12px] bg-[#8FA36A]/20 border border-[#8FA36A]/40 text-[#5a7040] font-sans font-semibold text-[14px] flex items-center w-full sm:w-auto justify-center">
                  {convertedTaskId ? `تم إنشاء مهمة #${convertedTaskId}` : "تم التحويل إلى مهمة"}
                </div>
              ) : (
                <button
                  type="button"
                  onClick={() => setConvertOpen(true)}
                  className="h-[44px] px-6 rounded-[12px] bg-[#7895B2] text-white font-sans font-semibold text-[14px] shadow-[0_2px_0_rgba(0,0,0,0.15)] hover:opacity-95 w-full sm:w-auto"
                >
                  تحويل البلاغ إلى مهمة
                </button>
              )}
            </div>
          </Card>

          {/* Forward Modal */}
          <Modal
            open={forwardOpen}
            title="تحويل البلاغ لقسم آخر"
            onClose={() => {
              setForwardOpen(false);
              setNote("");
              setSelectedDepartmentId("");
            }}
            footer={
              <>
                <button
                  type="button"
                  className="h-[40px] px-4 rounded-[10px] bg-white border border-black/10 font-sans font-semibold"
                  onClick={() => {
                    setForwardOpen(false);
                    setNote("");
                    setSelectedDepartmentId("");
                  }}
                >
                  إلغاء
                </button>
                <button
                  type="button"
                  className="h-[40px] px-4 rounded-[10px] bg-[#7895B2] text-white font-sans font-semibold disabled:opacity-50"
                  onClick={submitForwardToDepartment}
                  disabled={!selectedDepartmentId}
                >
                  تأكيد التحويل
                </button>
              </>
            }
          >
            <div className="text-right text-[13px] text-[#6B7280] font-sans font-semibold">اختر القسم</div>
            <select
              className="mt-2 w-full h-[44px] rounded-[12px] bg-white border border-black/10 px-3 text-right"
              value={selectedDepartmentId}
              onChange={(e) => setSelectedDepartmentId(e.target.value)}
            >
              <option value="">— اختر قسم —</option>
              {departments.map((d) => (
                <option key={d.departmentId} value={d.departmentId}>
                  {d.name}
                </option>
              ))}
            </select>

            <div className="mt-4 text-right text-[13px] text-[#6B7280] font-sans font-semibold">ملاحظة (اختياري)</div>
            <textarea
              className="mt-2 w-full min-h-[90px] rounded-[12px] bg-white border border-black/10 p-3 text-right"
              value={note}
              onChange={(e) => setNote(e.target.value)}
              placeholder="اكتب سبب التحويل أو تفاصيل إضافية..."
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

          {/* Convert to Task Modal */}
          <Modal
            open={convertOpen}
            title="تحويل البلاغ إلى مهمة"
            onClose={() => {
              setConvertOpen(false);
              setConvertAssignee("");
              setConvertPriority("Medium");
              setConvertDueDate("");
              setConvertRequiresPhoto(true);
            }}
            footer={
              <>
                <button
                  type="button"
                  className="h-[40px] px-4 rounded-[10px] bg-white border border-black/10 font-sans font-semibold"
                  onClick={() => setConvertOpen(false)}
                >
                  إلغاء
                </button>
                <button
                  type="button"
                  className="h-[40px] px-4 rounded-[10px] bg-[#7895B2] text-white font-sans font-semibold disabled:opacity-50"
                  onClick={submitConvertToTask}
                  disabled={!convertAssignee || convertSubmitting}
                >
                  {convertSubmitting ? "جاري التحويل..." : "تأكيد التحويل"}
                </button>
              </>
            }
          >
            <div className="text-right text-[13px] text-[#6B7280] font-sans font-semibold">تعيين إلى عامل *</div>
            <select
              className="mt-2 w-full h-[44px] rounded-[12px] bg-white border border-black/10 px-3 text-right"
              value={convertAssignee}
              onChange={(e) => setConvertAssignee(e.target.value)}
            >
              <option value="">— اختر عاملاً —</option>
              {workers.map((w) => (
                <option key={w.userId} value={w.userId}>
                  {w.fullName || w.username}
                </option>
              ))}
            </select>

            <div className="mt-4 text-right text-[13px] text-[#6B7280] font-sans font-semibold">الأولوية</div>
            <select
              className="mt-2 w-full h-[44px] rounded-[12px] bg-white border border-black/10 px-3 text-right"
              value={convertPriority}
              onChange={(e) => setConvertPriority(e.target.value as typeof convertPriority)}
            >
              <option value="Low">منخفضة</option>
              <option value="Medium">متوسطة</option>
              <option value="High">عالية</option>
              <option value="Urgent">عاجلة</option>
            </select>

            <div className="mt-4 text-right text-[13px] text-[#6B7280] font-sans font-semibold">تاريخ الاستحقاق (اختياري)</div>
            <input
              type="date"
              className="mt-2 w-full h-[44px] rounded-[12px] bg-white border border-black/10 px-3 text-right"
              value={convertDueDate}
              onChange={(e) => setConvertDueDate(e.target.value)}
              min={new Date().toISOString().split("T")[0]}
            />

            <div className="mt-4 flex items-center justify-end gap-3 flex-row-reverse">
              <label className="text-right text-[13px] text-[#6B7280] font-sans font-semibold" htmlFor="requires-photo">
                تتطلب إثبات صوري
              </label>
              <input
                id="requires-photo"
                type="checkbox"
                checked={convertRequiresPhoto}
                onChange={(e) => setConvertRequiresPhoto(e.target.checked)}
                className="w-4 h-4 accent-[#7895B2]"
              />
            </div>
          </Modal>

          <div className="mt-6" />
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

function StatusBadge({ status }: { status: DisplayIssueStatus }) {
  const map: Record<string, { label: string; bg: string; text: string }> = {
    new: { label: "جديد", bg: "#E5E7EB", text: "#2F2F2F" },
    reviewing: { label: "قيد المراجعة", bg: "#F3F1ED", text: "#2F2F2F" },
    converted: { label: "تم تحويله لمهمة", bg: "#8FA36A", text: "#FFFFFF" },
    forwarded: { label: "تم تحويله لمشرف", bg: "#7895B2", text: "#FFFFFF" },
    rejected: { label: "مرفوض", bg: "#C86E5D", text: "#FFFFFF" },
    closed: { label: "مغلق", bg: "#6B7280", text: "#FFFFFF" },
  };
  const s = map[status] || { label: status, bg: "#E5E7EB", text: "#2F2F2F" };
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
