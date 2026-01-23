// src/pages/TaskDetails.tsx
import React, { useEffect, useMemo, useState } from "react";
import { useParams } from "react-router-dom";
import { MapContainer, TileLayer, Marker, Popup } from "react-leaflet";
import L from "leaflet";
import { getTask, approveTask, rejectTask } from "../api/tasks";
import type { TaskResponse } from "../types/task";
import "leaflet/dist/leaflet.css";

// Fix Leaflet marker icons
const TaskLocationIcon = L.divIcon({
  className: "custom-marker",
  html: `<div class="relative flex items-center justify-center w-10 h-10">
    <div class="absolute w-full h-full bg-blue-500 rounded-full animate-ping opacity-30"></div>
    <div class="relative w-6 h-6 bg-blue-600 rounded-full border-3 border-white shadow-lg flex items-center justify-center">
      <svg width="14" height="14" viewBox="0 0 24 24" fill="white" stroke="white" stroke-width="2">
        <path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z"></path>
        <circle cx="12" cy="10" r="3" fill="white"></circle>
      </svg>
    </div>
  </div>`,
  iconSize: [40, 40],
  iconAnchor: [20, 20]
});

type Priority = "Low" | "Medium" | "High";

type TaskStatus =
  | "open"
  | "in_progress"
  | "pending_approval"
  | "approved"
  | "rejected";

type TaskUpdate = {
  id: string;
  createdAt: string;
  statusSnapshot: "in_progress" | "completed";
  progressPercent: number;
  note?: string;
  images: string[];
  locationText?: string;
  gps?: { lat: number; lng: number };
};

type TaskDTO = {
  id: string;
  title: string;
  description: string;
  dueDate?: string;
  priority: Priority;
  status: TaskStatus;

  assignedWorkerName: string;
  zoneName?: string;

  lastLocationText?: string;
  lastGps?: { lat: number; lng: number };

  updates: TaskUpdate[];
  lastRejectReason?: string;
};

export default function TaskDetails() {
  const { id } = useParams();

  const [task, setTask] = useState<TaskDTO | null>(null);
  const [loading, setLoading] = useState(true);
  const [err, setErr] = useState("");

  const [busyAction, setBusyAction] = useState<"approve" | "reject" | "">("");
  const [rejectOpen, setRejectOpen] = useState(false);
  const [rejectReason, setRejectReason] = useState("");

  useEffect(() => {
    const run = async () => {
      try {
        setLoading(true);
        setErr("");
        const data = await apiGetTaskDetails(String(id));
        setTask(data);
      } catch {
        setErr("فشل في تحميل بيانات المهمة");
      } finally {
        setLoading(false);
      }
    };
    run();
  }, [id]);

  const latestUpdate = useMemo(() => {
    if (!task?.updates?.length) return null;
    return [...task.updates].sort(
      (a, b) => +new Date(b.createdAt) - +new Date(a.createdAt)
    )[0];
  }, [task]);

  const progress = useMemo(() => {
    if (latestUpdate) return clamp(latestUpdate.progressPercent, 0, 100);
    if (task?.status === "approved") return 100;
    return 0;
  }, [latestUpdate, task?.status]);

  const canDecide = task?.status === "pending_approval";

  const onApprove = async () => {
    if (!task) return;
    try {
      setBusyAction("approve");
      setErr("");
      await apiApproveTask(task.id);
      const updated = await apiGetTaskDetails(task.id);
      setTask(updated);
    } catch {
      setErr("فشل في اعتماد المهمة");
    } finally {
      setBusyAction("");
    }
  };

  const onOpenReject = () => {
    setErr("");
    setRejectReason("");
    setRejectOpen(true);
  };

  const onConfirmReject = async () => {
    if (!task) return;
    if (!rejectReason.trim()) {
      setErr("سبب الرفض مطلوب");
      return;
    }
    try {
      setBusyAction("reject");
      setErr("");
      await apiRejectTask(task.id, rejectReason.trim());
      setRejectOpen(false);
      const updated = await apiGetTaskDetails(task.id);
      setTask(updated);
    } catch {
      setErr("فشل في رفض المهمة");
    } finally {
      setBusyAction("");
    }
  };

  if (loading) {
    return (
      <div className="h-full w-full bg-background grid place-items-center">
        <div className="text-[#2F2F2F] font-sans font-semibold">
          جاري التحميل...
        </div>
      </div>
    );
  }

  if (!task) {
    return (
      <div className="h-full w-full bg-background grid place-items-center">
        <div className="text-[#C86E5D] font-sans font-semibold">
          {err || "لا توجد بيانات"}
        </div>
      </div>
    );
  }

  return (
    <div className="h-full w-full bg-background overflow-auto">
      <div className="p-4 sm:p-6 md:p-8">
        <div className="max-w-[1100px] mx-auto">
          <div className="flex items-start justify-between gap-3">
            <div className="text-right">
              <h1 className="font-sans font-semibold text-[20px] sm:text-[22px] text-[#2F2F2F]">
                تفاصيل المهمة
              </h1>
              <div className="mt-1 text-[12px] text-[#6B7280]">
                #{task.id} •{" "}
                {task.dueDate ? `موعد التنفيذ: ${formatDate(task.dueDate)}` : "بدون موعد محدد"}
              </div>
            </div>

            <StatusBadge status={task.status} />
          </div>

          {err && (
            <div className="mt-4 p-3 bg-red-100 text-red-700 rounded-[10px] text-right">
              {err}
            </div>
          )}

          <Card className="mt-4">
            <div className="flex items-center justify-between gap-3">
              <div className="text-right font-sans font-semibold text-[18px] text-[#2F2F2F]">
                {task.title}
              </div>
              <PriorityBadge priority={task.priority} />
            </div>

            <div className="mt-3 grid grid-cols-1 sm:grid-cols-3 gap-3">
              <InfoBox label="الموظف (العامل)" value={task.assignedWorkerName} />
              <InfoBox label="المنطقة (Zone)" value={task.zoneName || "—"} />
              <InfoBox label="نسبة الإنجاز (آخر تحديث)" value={`${progress}%`} />
            </div>

            <div className="mt-4">
              <div className="text-right text-[13px] text-[#6B7280] font-sans font-semibold">
                وصف المهمة
              </div>
              <div className="mt-2 bg-white rounded-[12px] border border-black/10 p-4 text-right text-[14px] text-[#2F2F2F] leading-relaxed">
                {task.description}
              </div>
            </div>

            <div className="mt-4 grid grid-cols-1 lg:grid-cols-2 gap-3">
              <div className="bg-white rounded-[12px] border border-black/10 p-4">
                <div className="text-right text-[13px] text-[#6B7280] font-sans font-semibold">
                  آخر موقع مُرسل من العامل
                </div>

                <div className="mt-2 text-right text-[14px] text-[#2F2F2F] font-sans font-semibold">
                  {latestUpdate?.locationText || task.lastLocationText || "—"}
                </div>

                {(latestUpdate?.gps || task.lastGps) && (
                  <div className="mt-2 text-right text-[12px] text-[#6B7280]">
                    GPS: {(latestUpdate?.gps || task.lastGps)!.lat},{" "}
                    {(latestUpdate?.gps || task.lastGps)!.lng}
                  </div>
                )}
              </div>

              <div className="bg-white rounded-[12px] border border-black/10 p-4">
                <div className="text-right text-[13px] text-[#6B7280] font-sans font-semibold mb-2">
                  موقع المهمة على الخريطة
                </div>
                {(latestUpdate?.gps || task.lastGps) ? (
                  <div className="h-[180px] rounded-[10px] overflow-hidden border border-black/5">
                    <MapContainer
                      center={[(latestUpdate?.gps || task.lastGps)!.lat, (latestUpdate?.gps || task.lastGps)!.lng]}
                      zoom={15}
                      className="w-full h-full"
                      zoomControl={false}
                    >
                      <TileLayer
                        attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
                        url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
                      />
                      <Marker
                        position={[(latestUpdate?.gps || task.lastGps)!.lat, (latestUpdate?.gps || task.lastGps)!.lng]}
                        icon={TaskLocationIcon}
                      >
                        <Popup>
                          <div dir="rtl" className="text-right font-sans p-1">
                            <div className="font-bold text-[#2F2F2F] text-sm mb-1">
                              آخر موقع للعامل
                            </div>
                            <div className="text-[10px] text-[#6B7280]">
                              {(latestUpdate?.gps || task.lastGps)!.lat.toFixed(5)}, {(latestUpdate?.gps || task.lastGps)!.lng.toFixed(5)}
                            </div>
                            {latestUpdate?.locationText || task.lastLocationText ? (
                              <div className="text-[10px] text-[#6B7280] mt-1">
                                {latestUpdate?.locationText || task.lastLocationText}
                              </div>
                            ) : null}
                          </div>
                        </Popup>
                      </Marker>
                    </MapContainer>
                  </div>
                ) : (
                  <div className="h-[140px] rounded-[10px] bg-[#E9E6E0] border border-black/5 grid place-items-center text-[#6B7280] text-[12px]">
                    لا يوجد موقع GPS متاح حالياً
                  </div>
                )}
              </div>
            </div>

            <div className="mt-5">
              <div className="flex items-center justify-between gap-3">
                <div className="text-right text-[13px] text-[#6B7280] font-sans font-semibold">
                  سجل تحديثات العامل
                </div>

                <div className="text-[12px] text-[#6B7280]">
                  عدد التحديثات:{" "}
                  <span className="font-semibold text-[#2F2F2F]">
                    {task.updates.length}
                  </span>
                </div>
              </div>

              {task.updates.length === 0 ? (
                <div className="mt-2 bg-white rounded-[12px] border border-black/10 p-4 text-right text-[#6B7280] text-[13px]">
                  لا يوجد تحديثات بعد.
                </div>
              ) : (
                <div className="mt-3 space-y-3">
                  {[...task.updates]
                    .sort(
                      (a, b) => +new Date(b.createdAt) - +new Date(a.createdAt)
                    )
                    .map((u, idx) => (
                      <div
                        key={u.id}
                        className="bg-white rounded-[14px] border border-black/10 p-4"
                      >
                        <div className="flex items-start justify-between gap-3">
                          <div className="text-right">
                            <div className="flex items-center justify-end gap-2 flex-wrap">
                              <span className="text-[12px] text-[#6B7280]">
                                {formatDateTime(u.createdAt)}
                              </span>
                              <span className="text-[12px] text-[#6B7280]">•</span>
                              <span className="text-[12px] font-sans font-semibold text-[#2F2F2F]">
                                تحديث #{task.updates.length - idx}
                              </span>
                            </div>

                            <div className="mt-2 flex items-center justify-end gap-2 flex-wrap">
                              <SnapBadge snap={u.statusSnapshot} />
                              <span className="text-[12px] text-[#6B7280]">
                                نسبة الإنجاز:
                              </span>
                              <span className="text-[13px] font-sans font-semibold text-[#2F2F2F]">
                                {clamp(u.progressPercent, 0, 100)}%
                              </span>
                            </div>
                          </div>

                          <MiniProgressRing value={clamp(u.progressPercent, 0, 100)} />
                        </div>

                        {u.note && (
                          <div className="mt-3">
                            <div className="text-right text-[12px] text-[#6B7280] font-sans font-semibold">
                              ملاحظة
                            </div>
                            <div className="mt-2 text-right text-[13px] text-[#2F2F2F] leading-relaxed">
                              {u.note}
                            </div>
                          </div>
                        )}

                        <div className="mt-4">
                          <div className="text-right text-[12px] text-[#6B7280] font-sans font-semibold">
                            الصور ({u.images.length})
                          </div>

                          {u.images.length === 0 ? (
                            <div className="mt-2 text-right text-[12px] text-[#C86E5D]">
                              لا توجد صور لهذا التحديث.
                            </div>
                          ) : (
                            <div className="mt-2 grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-3">
                              {u.images.map((src, i) => (
                                <button
                                  key={src + i}
                                  type="button"
                                  className="rounded-[12px] border border-black/10 overflow-hidden hover:opacity-95 transition"
                                  onClick={() => window.open(src, "_blank")}
                                  aria-label={`Open image ${i + 1}`}
                                >
                                  <img
                                    src={src}
                                    alt={`update-${u.id}-${i + 1}`}
                                    className="w-full h-[110px] object-cover"
                                  />
                                </button>
                              ))}
                            </div>
                          )}
                        </div>

                        {(u.locationText || u.gps) && (
                          <div className="mt-4 pt-4 border-t border-black/10">
                            <div className="text-right text-[12px] text-[#6B7280] font-sans font-semibold">
                              الموقع
                            </div>
                            <div className="mt-2 text-right text-[13px] text-[#2F2F2F]">
                              {u.locationText || "—"}
                            </div>
                            {u.gps && (
                              <div className="mt-1 text-right text-[12px] text-[#6B7280]">
                                GPS: {u.gps.lat}, {u.gps.lng}
                              </div>
                            )}
                          </div>
                        )}
                      </div>
                    ))}
                </div>
              )}
            </div>

            {task.status === "rejected" && task.lastRejectReason && (
              <div className="mt-5 bg-[#F2D3CD] rounded-[12px] border border-black/10 p-4 text-right">
                <div className="text-[13px] font-sans font-semibold text-[#2F2F2F]">
                  سبب الرفض
                </div>
                <div className="mt-2 text-[13px] text-[#2F2F2F] leading-relaxed">
                  {task.lastRejectReason}
                </div>
              </div>
            )}

            <div className="mt-6 flex items-center justify-end gap-2 flex-wrap">
              <button
                type="button"
                onClick={onOpenReject}
                disabled={!canDecide || busyAction !== ""}
                className={[
                  "h-[44px] px-4 rounded-[12px] font-sans font-semibold text-[14px] border",
                  canDecide
                    ? "bg-white text-[#2F2F2F] border-black/10 hover:opacity-95"
                    : "bg-white/60 text-[#6B7280] border-black/10 cursor-not-allowed",
                ].join(" ")}
              >
                رفض
              </button>

              <button
                type="button"
                onClick={onApprove}
                disabled={!canDecide || busyAction !== ""}
                className={[
                  "h-[44px] px-6 rounded-[12px] font-sans font-semibold text-[14px] shadow-[0_2px_0_rgba(0,0,0,0.15)]",
                  canDecide
                    ? "bg-[#60778E] text-white hover:opacity-95"
                    : "bg-[#60778E]/50 text-white cursor-not-allowed",
                ].join(" ")}
              >
                {busyAction === "approve" ? "..." : "اعتماد المهمة"}
              </button>
            </div>

            {!canDecide && (
              <div className="mt-3 text-right text-[12px] text-[#6B7280]">
                {task.status === "in_progress"
                  ? "المهمة قيد التنفيذ."
                  : task.status === "approved"
                  ? "تم اعتماد المهمة."
                  : task.status === "rejected"
                  ? "تم رفض المهمة."
                  : "بانتظار تحديث من العامل."}
              </div>
            )}
          </Card>
        </div>
      </div>

      {rejectOpen && (
        <CenterModal onClose={() => (busyAction ? null : setRejectOpen(false))}>
          <div className="text-right">
            <div className="text-[16px] font-sans font-semibold text-[#2F2F2F]">
              رفض المهمة
            </div>

            <div className="mt-2 text-[12px] text-[#6B7280]">
              اكتب سبب الرفض (إجباري)
            </div>

            <textarea
              value={rejectReason}
              onChange={(e) => setRejectReason(e.target.value)}
              rows={4}
              className="mt-3 w-full rounded-[12px] bg-white border border-black/10 px-4 py-3 text-right outline-none focus:ring-2 focus:ring-black/10"
              placeholder="..."
              disabled={busyAction !== ""}
            />

            <div className="mt-4 flex items-center justify-between gap-2">
              <button
                type="button"
                onClick={() => setRejectOpen(false)}
                disabled={busyAction !== ""}
                className="h-[42px] px-4 rounded-[12px] bg-white border border-black/10 text-[#2F2F2F] font-sans font-semibold text-[14px]"
              >
                إلغاء
              </button>

              <button
                type="button"
                onClick={onConfirmReject}
                disabled={busyAction !== ""}
                className="h-[42px] px-5 rounded-[12px] bg-[#C86E5D] text-white font-sans font-semibold text-[14px] shadow-[0_2px_0_rgba(0,0,0,0.15)] hover:opacity-95"
              >
                {busyAction === "reject" ? "..." : "تأكيد الرفض"}
              </button>
            </div>
          </div>
        </CenterModal>
      )}
    </div>
  );
}

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

function PriorityBadge({ priority }: { priority: Priority }) {
  const map: Record<Priority, { label: string; bg: string; text: string }> = {
    Low: { label: "أولوية منخفضة", bg: "#E5E7EB", text: "#2F2F2F" },
    Medium: { label: "أولوية متوسطة", bg: "#F3E7C8", text: "#2F2F2F" },
    High: { label: "أولوية عالية", bg: "#F2D3CD", text: "#2F2F2F" },
  };
  const p = map[priority];
  return (
    <span
      className="text-[12px] px-3 py-1.5 rounded-full border border-black/10 font-sans font-semibold"
      style={{ backgroundColor: p.bg, color: p.text }}
    >
      {p.label}
    </span>
  );
}

function StatusBadge({ status }: { status: TaskStatus }) {
  const map: Record<TaskStatus, { label: string; bg: string; text: string }> = {
    open: { label: "مفتوحة", bg: "#E5E7EB", text: "#2F2F2F" },
    in_progress: { label: "قيد التنفيذ", bg: "#F3F1ED", text: "#2F2F2F" },
    pending_approval: { label: "بانتظار الاعتماد", bg: "#60778E", text: "#FFFFFF" },
    approved: { label: "معتمدة", bg: "#8FA36A", text: "#FFFFFF" },
    rejected: { label: "مرفوضة", bg: "#C86E5D", text: "#FFFFFF" },
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

function SnapBadge({ snap }: { snap: TaskUpdate["statusSnapshot"] }) {
  const isDone = snap === "completed";
  return (
    <span
      className="text-[11px] px-2 py-1 rounded-full border border-black/10 font-sans font-semibold"
      style={{
        backgroundColor: isDone ? "#8FA36A" : "#E5E7EB",
        color: isDone ? "#fff" : "#2F2F2F",
      }}
    >
      {isDone ? "مكتملة" : "قيد التنفيذ"}
    </span>
  );
}

function MiniProgressRing({ value }: { value: number }) {
  const size = 44;
  const stroke = 6;
  const r = (size - stroke) / 2;
  const c = 2 * Math.PI * r;
  const v = clamp(value, 0, 100);
  const dash = (c * v) / 100;

  return (
    <svg width={size} height={size} viewBox={`0 0 ${size} ${size}`} aria-label={`Progress ${v}%`}>
      <circle cx={size / 2} cy={size / 2} r={r} fill="none" stroke="#E5E7EB" strokeWidth={stroke} />
      <circle
        cx={size / 2}
        cy={size / 2}
        r={r}
        fill="none"
        stroke="#60778E"
        strokeWidth={stroke}
        strokeDasharray={`${dash} ${c - dash}`}
        transform={`rotate(-90 ${size / 2} ${size / 2})`}
        strokeLinecap="round"
      />
      <text
        x="50%"
        y="55%"
        textAnchor="middle"
        fontSize="11"
        fill="#2F2F2F"
        fontFamily="Cairo, sans-serif"
        fontWeight={700}
      >
        {v}%
      </text>
    </svg>
  );
}

function CenterModal({ children, onClose }: { children: React.ReactNode; onClose: () => void }) {
  return (
    <div className="fixed inset-0 z-[60] flex items-center justify-center p-4">
      <button
        type="button"
        className="absolute inset-0 bg-black/40"
        onClick={onClose}
        aria-label="Close"
      />
      <div className="relative w-full max-w-[560px] bg-[#F3F1ED] rounded-[16px] shadow-2xl border border-black/10 p-5">
        {children}
      </div>
    </div>
  );
}

function clamp(n: number, a: number, b: number) {
  return Math.max(a, Math.min(b, n));
}

function formatDate(d: string) {
  return d.slice(0, 10);
}

function formatDateTime(d: string) {
  const dt = new Date(d);
  if (Number.isNaN(+dt)) return d;
  const yyyy = dt.getFullYear();
  const mm = String(dt.getMonth() + 1).padStart(2, "0");
  const dd = String(dt.getDate()).padStart(2, "0");
  const hh = String(dt.getHours()).padStart(2, "0");
  const mi = String(dt.getMinutes()).padStart(2, "0");
  return `${yyyy}-${mm}-${dd} ${hh}:${mi}`;
}

// Map backend status to frontend status
const mapBackendStatus = (status: string): TaskStatus => {
  switch (status) {
    case "Pending": return "open";
    case "InProgress": return "in_progress";
    case "Completed": return "pending_approval";
    case "Approved": return "approved";
    case "Rejected": return "rejected";
    default: return "open";
  }
};

// Map backend priority
const mapBackendPriority = (priority: string): Priority => {
  if (priority === "Urgent") return "High";
  return priority as Priority;
};

// Convert backend TaskResponse to frontend TaskDTO
const mapBackendTaskToDTO = (task: TaskResponse): TaskDTO => {
  // Create updates array from task photos if available
  const updates: TaskUpdate[] = [];
  if (task.photos && task.photos.length > 0) {
    updates.push({
      id: "completion-" + task.taskId,
      createdAt: task.completedAt || task.startedAt || task.createdAt,
      statusSnapshot: task.status === "Completed" || task.status === "Approved" ? "completed" : "in_progress",
      progressPercent: task.progressPercentage || (task.status === "Approved" ? 100 : task.status === "Completed" ? 100 : 50),
      note: task.completionNotes || task.progressNotes,
      images: task.photos,
      locationText: task.locationDescription,
      gps: task.latitude && task.longitude ? { lat: task.latitude, lng: task.longitude } : undefined,
    });
  }

  return {
    id: task.taskId.toString(),
    title: task.title,
    description: task.description,
    dueDate: task.dueDate?.split("T")[0],
    priority: mapBackendPriority(task.priority),
    status: mapBackendStatus(task.status),
    assignedWorkerName: task.assignedToUserName || "غير محدد",
    zoneName: task.zoneName,
    lastLocationText: task.locationDescription,
    lastGps: task.latitude && task.longitude ? { lat: task.latitude, lng: task.longitude } : undefined,
    updates: updates,
    lastRejectReason: task.rejectionReason || (task.status === "Rejected" ? "تم رفض المهمة" : undefined),
  };
};

async function apiGetTaskDetails(taskId: string): Promise<TaskDTO> {
  const task = await getTask(Number(taskId));
  return mapBackendTaskToDTO(task);
}

async function apiApproveTask(taskId: string) {
  await approveTask(Number(taskId), "تمت الموافقة على المهمة");
  return true;
}

async function apiRejectTask(taskId: string, reason: string) {
  await rejectTask(Number(taskId), reason);
  return true;
}
