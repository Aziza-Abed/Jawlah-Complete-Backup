import React, { useEffect, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { usePageTitle } from "../hooks/usePageTitle";
import { useToast } from "../contexts/ToastContext";
import { STORAGE_KEYS } from "../constants/storageKeys";
import { getWorkers, getMyWorkers } from "../api/users";
import { getZones } from "../api/zones";
import { getTeams, type Team } from "../api/teams";
import { createTask } from "../api/tasks";
import type { UserResponse } from "../types/user";
import type { ZoneResponse } from "../types/zone";
import type { TaskPriority } from "../types/task";
import LocationPicker from "../components/UI/LocationPicker";

type PriorityOption = { value: TaskPriority; label: string };


const priorityOptions: PriorityOption[] = [
  { value: "Low", label: "منخفضة" },
  { value: "Medium", label: "متوسطة" },
  { value: "High", label: "عالية" },
  { value: "Urgent", label: "عاجلة" },
];



// ✅ NEW: shape coming from IssueDetails navigate state
type FromIssue = {
  issueId?: string;
  title?: string;
  description?: string;
  zoneId?: number | string;
  zone?: string; // e.g. "المنطقة 4"
  locationText?: string;
  severity?: "low" | "medium" | "high" | "critical";
  type?: string;
  images?: string[];
  gps?: { lat: number; lng: number };
};

export default function CreateTask() {
  const location = useLocation();
  const navigate = useNavigate();
  const fromIssue = (location.state as { fromIssue?: FromIssue } | null)?.fromIssue;

  const [employees, setEmployees] = useState<UserResponse[]>([]);
  const [teams, setTeams] = useState<Team[]>([]);
  const [zones, setZones] = useState<ZoneResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");
  const { showToast } = useToast();
  usePageTitle("تعيين مهمة جديدة");

  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [startDate, setStartDate] = useState("");
  const [endDate, setEndDate] = useState("");
  const [priority, setPriority] = useState<TaskPriority | "">("");
  // Assignment type: "individual" or "team"
  const [assignmentType, setAssignmentType] = useState<"individual" | "team">("individual");
  const [employeeId, setEmployeeId] = useState("");
  const [teamId, setTeamId] = useState("");
  const [zoneId, setZoneId] = useState("");
  // New fields for mobile consistency
  const [latitude, setLatitude] = useState<number | null>(null);
  const [longitude, setLongitude] = useState<number | null>(null);
  const [locationDescription, setLocationDescription] = useState("");
  const [requiresPhotoProof, setRequiresPhotoProof] = useState(true);

  // ✅ NEW: helper to map severity -> priority
  const mapSeverityToPriority = (sev?: FromIssue["severity"]): TaskPriority | "" => {
    if (!sev) return "";
    if (sev === "critical") return "Urgent";
    if (sev === "high") return "High";
    if (sev === "medium") return "Medium";
    return "Low";
  };

  // Get user role to determine which workers to show
  const isAdmin = (): boolean => {
    try {
      const userStr = localStorage.getItem(STORAGE_KEYS.USER);
      if (userStr) {
        const user = JSON.parse(userStr);
        return user.role?.toLowerCase() === "admin";
      }
    } catch { /* ignore */ }
    return false;
  };

  useEffect(() => {
    const fetchData = async () => {
      try {
        // Admins see all workers, Supervisors see only their assigned workers
        const workersPromise = isAdmin() ? getWorkers() : getMyWorkers();
        const [workersData, teamsData, zonesData] = await Promise.all([
          workersPromise,
          getTeams(),
          getZones()
        ]);
        setEmployees(workersData);
        setTeams(teamsData);
        setZones(zonesData);
      } catch (err) {
        setError("فشل في تحميل البيانات");
      } finally {
        setLoading(false);
      }
    };
    fetchData();
  }, []);

  // ✅ NEW: Prefill form if coming from issue
  useEffect(() => {
    if (!fromIssue) return;

    // Title
    if (fromIssue.title) setTitle(fromIssue.title);

    // Description (we add location/type nicely without forcing)
    const parts: string[] = [];
    if (fromIssue.description) parts.push(fromIssue.description);

    if (fromIssue.type) parts.push(`نوع المشكلة: ${fromIssue.type}`);
    if (fromIssue.locationText) parts.push(`الموقع: ${fromIssue.locationText}`);
    if (fromIssue.issueId) parts.push(`(محوّلة من بلاغ رقم: ${fromIssue.issueId})`);

    const finalDesc = parts.filter(Boolean).join("\n");
    if (finalDesc) setDescription(finalDesc);

    // Priority
    const mapped = mapSeverityToPriority(fromIssue.severity);
    if (mapped) setPriority(mapped);

    // Zone (if zoneId exists use it directly)
    if (fromIssue.zoneId !== undefined && fromIssue.zoneId !== null && String(fromIssue.zoneId).trim() !== "") {
      setZoneId(String(fromIssue.zoneId));
    }

    // GPS coordinates from issue
    if (fromIssue.gps?.lat !== undefined && fromIssue.gps?.lng !== undefined) {
      setLatitude(fromIssue.gps.lat);
      setLongitude(fromIssue.gps.lng);
    }

    // Location description from issue
    if (fromIssue.locationText) {
      setLocationDescription(fromIssue.locationText);
    }
  }, [fromIssue]);

  // ✅ NEW: If we only received zone name, match it after zones are loaded
  useEffect(() => {
    if (!fromIssue) return;
    if (zoneId) return; // already set (zoneId came directly)
    if (!fromIssue.zone) return;
    if (zones.length === 0) return;

    // match by exact name first
    const exact = zones.find((z) => z.zoneName === fromIssue.zone);
    if (exact) {
      setZoneId(String(exact.zoneId));
      return;
    }

    // fallback: if issue has "المنطقة 4" match by inclusion of number / partial
    const relaxed = zones.find((z) => z.zoneName.includes(fromIssue.zone as string) || (fromIssue.zone as string).includes(z.zoneName));
    if (relaxed) {
      setZoneId(String(relaxed.zoneId));
    }
  }, [fromIssue, zones, zoneId]);

  // Field-level validation errors
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});

  const validateForm = (): boolean => {
    const errors: Record<string, string> = {};

    if (!title.trim()) {
      errors.title = "عنوان المهمة مطلوب";
    } else if (title.trim().length < 3) {
      errors.title = "العنوان يجب أن يكون 3 أحرف على الأقل";
    }

    if (!description.trim()) {
      errors.description = "وصف المهمة مطلوب";
    } else if (description.trim().length < 10) {
      errors.description = "الوصف يجب أن يكون 10 أحرف على الأقل";
    }

    if (!priority) {
      errors.priority = "يرجى اختيار الأولوية";
    }

    if (assignmentType === "individual" && !employeeId) {
      errors.employee = "يرجى اختيار موظف";
    }
    if (assignmentType === "team" && !teamId) {
      errors.team = "يرجى اختيار فريق";
    }

    if (startDate && endDate && new Date(endDate) < new Date(startDate)) {
      errors.endDate = "موعد الانتهاء يجب أن يكون بعد موعد البدء";
    }

    setFieldErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const clearFieldError = (field: string) => {
    setFieldErrors(prev => { const next = { ...prev }; delete next[field]; return next; });
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    if (!validateForm()) return;

    setSubmitting(true);

    try {
      await createTask({
        title,
        description,
        // Send either assignedToUserId or teamId based on assignment type
        assignedToUserId: assignmentType === "individual" ? parseInt(employeeId) : undefined,
        teamId: assignmentType === "team" ? parseInt(teamId) : undefined,
        zoneId: zoneId ? parseInt(zoneId) : undefined,
        priority: priority as TaskPriority,
        scheduledAt: startDate || undefined,
        dueDate: endDate || undefined,
        latitude: latitude || undefined,
        longitude: longitude || undefined,
        locationDescription: locationDescription || undefined,

        requiresPhotoProof,
      });

      showToast(assignmentType === "team"
        ? "تم إنشاء المهمة الجماعية بنجاح - سيتم إشعار جميع أعضاء الفريق"
        : "تم إنشاء المهمة بنجاح");

      navigate("/tasks");
    } catch (err) {
      setError("فشل في إنشاء المهمة");
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) {
    return (
      <div className="h-full w-full bg-[#F3F1ED] flex items-center justify-center">
        <div className="text-[#2F2F2F]">جاري التحميل...</div>
      </div>
    );
  }

  return (
    <div className="h-full w-full bg-[#F3F1ED] overflow-auto">
      <div className="p-4 sm:p-6 md:p-8">
        <div className="max-w-[980px] mx-auto">
          <h1 className="text-right font-black text-[28px] text-[#2F2F2F] tracking-tight mb-6">
            تعيين مهمة جديدة
          </h1>

          {fromIssue?.issueId && (
            <div className="mb-4 p-3 bg-[#F3F1ED] border border-black/10 rounded-[10px] text-right text-[#2F2F2F]">
              تم تعبئة الحقول تلقائيًا من البلاغ: <span className="font-semibold">#{fromIssue.issueId}</span>
            </div>
          )}

          {error && (
            <div className="mb-4 p-3 bg-red-100 text-red-700 rounded-[10px] text-right">
              {error}
            </div>
          )}

          <form onSubmit={handleSubmit} className="w-full">
            <div className="grid grid-cols-1 gap-6">
              <FieldRow label="عنوان المهمة" error={fieldErrors.title}>
                <Input
                  value={title}
                  onChange={(e) => { setTitle(e.target.value); clearFieldError("title"); }}
                  placeholder="اسم المهمة"
                  required
                  minLength={3}
                  maxLength={200}
                  disabled={submitting}
                  hasError={!!fieldErrors.title}
                />
              </FieldRow>

              <FieldRow label="وصف المهمة" error={fieldErrors.description}>
                <Textarea
                  value={description}
                  onChange={(e) => { setDescription(e.target.value); clearFieldError("description"); }}
                  placeholder="يرجى كتابة وصف مختصر للمهمة..."
                  required
                  minLength={10}
                  maxLength={1000}
                  disabled={submitting}
                  hasError={!!fieldErrors.description}
                />
              </FieldRow>

              <FieldRow label="موعد البدء">
                <DateTimeInput value={startDate} onChange={setStartDate} disabled={submitting} />
              </FieldRow>

              <FieldRow label="موعد الانتهاء" error={fieldErrors.endDate}>
                <DateTimeInput value={endDate} onChange={(v) => { setEndDate(v); clearFieldError("endDate"); }} disabled={submitting} />
              </FieldRow>

              <FieldRow label="الأولوية" error={fieldErrors.priority}>
                <Select
                  value={priority}
                  onChange={(v) => { setPriority(v as TaskPriority); clearFieldError("priority"); }}
                  placeholder="اختر درجة الأهمية..."
                  options={priorityOptions.map((p) => ({ value: p.value, label: p.label }))}
                  required
                  disabled={submitting}
                  hasError={!!fieldErrors.priority}
                />
              </FieldRow>

              {/* Assignment Type Toggle */}
              <FieldRow label="نوع التعيين">
                <div className="flex gap-4 items-center justify-end">
                  <label className="flex items-center gap-2 cursor-pointer">
                    <span className={assignmentType === "individual" ? "text-[#2F2F2F] font-semibold" : "text-[#6B7280]"}>
                      موظف فردي
                    </span>
                    <input
                      type="radio"
                      name="assignmentType"
                      checked={assignmentType === "individual"}
                      onChange={() => {
                        setAssignmentType("individual");
                        setTeamId("");
                      }}
                      disabled={submitting}
                      className="w-4 h-4 text-[#7895B2] focus:ring-[#7895B2]"
                    />
                  </label>
                  <label className={`flex items-center gap-2 ${teams.length > 0 ? "cursor-pointer" : "cursor-not-allowed opacity-50"}`}>
                    <span className={assignmentType === "team" ? "text-[#2F2F2F] font-semibold" : "text-[#6B7280]"}>
                      فريق عمل
                    </span>
                    <input
                      type="radio"
                      name="assignmentType"
                      checked={assignmentType === "team"}
                      onChange={() => {
                        setAssignmentType("team");
                        setEmployeeId("");
                      }}
                      disabled={submitting || teams.length === 0}
                      className="w-4 h-4 text-[#7895B2] focus:ring-[#7895B2]"
                    />
                  </label>
                </div>
              </FieldRow>

              {/* Individual Worker Selection */}
              {assignmentType === "individual" && (
                <FieldRow label="الموظف" error={fieldErrors.employee}>
                  <Select
                    value={employeeId}
                    onChange={(v) => { setEmployeeId(v); clearFieldError("employee"); }}
                    placeholder="اسم الموظف..."
                    options={employees.map((e) => ({
                      value: e.userId.toString(),
                      label: e.fullName,
                    }))}
                    required
                    disabled={submitting}
                    hasError={!!fieldErrors.employee}
                  />
                </FieldRow>
              )}

              {/* Team Selection */}
              {assignmentType === "team" && (
                <FieldRow label="الفريق" error={fieldErrors.team}>
                  <Select
                    value={teamId}
                    onChange={(v) => { setTeamId(v); clearFieldError("team"); }}
                    placeholder="اختر الفريق..."
                    options={teams.map((t) => ({
                      value: t.teamId.toString(),
                      label: `${t.name} (${t.membersCount} أعضاء)`,
                    }))}
                    required
                    disabled={submitting}
                    hasError={!!fieldErrors.team}
                  />
                  {teamId && (
                    <p className="mt-2 text-xs text-[#6B7280] text-right">
                      سيتم إشعار جميع أعضاء الفريق بالمهمة. أول من يقوم بإرسال المهمة سيتم اعتماده.
                    </p>
                  )}
                </FieldRow>
              )}

              <FieldRow label="المنطقة">
                <Select
                  value={zoneId}
                  onChange={setZoneId}
                  placeholder="حدد منطقة المهمة"
                  options={zones.map((z) => ({
                    value: z.zoneId.toString(),
                    label: z.zoneName,
                  }))}
                  disabled={submitting}
                />
              </FieldRow>



              {/* Map Location Picker - Full Width */}
              <div className="space-y-3">
                <div className="text-right font-sans font-semibold text-[#2F2F2F] text-[16px] sm:text-[17px] md:text-[18px]">
                  موقع المهمة على الخريطة
                </div>
                <LocationPicker
                  latitude={latitude}
                  longitude={longitude}
                  onLocationChange={(lat, lng) => {
                    if (lat === 0 && lng === 0) {
                      setLatitude(null);
                      setLongitude(null);
                    } else {
                      setLatitude(lat);
                      setLongitude(lng);
                    }
                  }}
                  onLocationNameChange={(name) => {
                    // Auto-fill location description when selecting from search
                    setLocationDescription(name);
                  }}
                  disabled={submitting}
                />
                <p className="text-xs text-[#6B7280] text-right">
                  ابحث عن الموقع أو انقر على الخريطة لتحديد موقع المهمة (مسافة التحذير: 100م، الرفض التلقائي: 500م)
                </p>
              </div>

              <FieldRow label="وصف الموقع">
                <Input
                  value={locationDescription}
                  onChange={(e) => setLocationDescription(e.target.value)}
                  placeholder="مثال: بجانب مسجد الحي، شارع الملك فهد..."
                  disabled={submitting}
                  maxLength={200}
                />
              </FieldRow>



              <FieldRow label="يتطلب صورة إثبات">
                <label className="flex items-center gap-3 cursor-pointer justify-end">
                  <span className="text-[#2F2F2F]">
                    {requiresPhotoProof ? "نعم، يجب إرفاق صورة" : "لا، غير مطلوب"}
                  </span>
                  <input
                    type="checkbox"
                    checked={requiresPhotoProof}
                    onChange={(e) => setRequiresPhotoProof(e.target.checked)}
                    disabled={submitting}
                    className="w-5 h-5 rounded border-black/10 text-[#7895B2] focus:ring-[#7895B2]"
                  />
                </label>
              </FieldRow>
            </div>

            <div className="mt-10 flex justify-center">
              <button
                type="submit"
                disabled={submitting}
                className="w-[220px] h-[56px] rounded-[10px] bg-[#7895B2] text-white font-sans font-semibold text-[18px] shadow-[0_2px_0_rgba(0,0,0,0.15)] hover:opacity-95 active:opacity-90 disabled:opacity-50"
              >
                {submitting ? "جاري الإنشاء..." : "تعيين المهمة"}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}

/* ---------- UI building blocks ---------- */

function FieldRow({ label, error, children }: { label: string; error?: string; children: React.ReactNode }) {
  return (
    <div className="grid grid-cols-1 md:grid-cols-[200px_1fr] items-start gap-3 md:gap-4">
      <div className="text-right font-sans font-semibold text-[#2F2F2F] text-[16px] sm:text-[17px] md:text-[18px] pt-2">
        {label}
      </div>

      <div className="flex flex-col">
        <div className="w-full max-w-[560px]">{children}</div>
        {error && <p className="text-right text-red-500 text-[12px] mt-1 font-semibold max-w-[560px]">{error}</p>}
      </div>
    </div>
  );
}

function Input({
  value,
  onChange,
  placeholder,
  required,
  disabled,
  hasError,
  minLength,
  maxLength,
}: {
  value: string;
  onChange: (e: React.ChangeEvent<HTMLInputElement>) => void;
  placeholder?: string;
  required?: boolean;
  disabled?: boolean;
  hasError?: boolean;
  minLength?: number;
  maxLength?: number;
}) {
  return (
    <input
      value={value}
      onChange={onChange}
      placeholder={placeholder}
      required={required}
      disabled={disabled}
      minLength={minLength}
      maxLength={maxLength}
      className={`w-full h-[44px] rounded-[10px] bg-[#F3F1ED] border ${hasError ? "border-red-400" : "border-black/10"} px-4 text-right outline-none focus:ring-2 focus:ring-black/10 disabled:opacity-50`}
    />
  );
}

function Textarea({
  value,
  onChange,
  placeholder,
  required,
  disabled,
  hasError,
  minLength,
  maxLength,
}: {
  value: string;
  onChange: (e: React.ChangeEvent<HTMLTextAreaElement>) => void;
  placeholder?: string;
  required?: boolean;
  disabled?: boolean;
  hasError?: boolean;
  minLength?: number;
  maxLength?: number;
}) {
  return (
    <div className="relative">
      <textarea
        value={value}
        onChange={onChange}
        placeholder={placeholder}
        required={required}
        disabled={disabled}
        minLength={minLength}
        maxLength={maxLength}
        rows={3}
        className={`w-full min-h-[78px] rounded-[10px] bg-[#F3F1ED] border ${hasError ? "border-red-400" : "border-black/10"} px-4 py-3 text-right outline-none resize-none focus:ring-2 focus:ring-black/10 disabled:opacity-50`}
      />
      {maxLength && (
        <span className={`absolute bottom-2 left-3 text-[11px] ${value.length > maxLength * 0.9 ? "text-red-400" : "text-[#9CA3AF]"}`}>
          {value.length}/{maxLength}
        </span>
      )}
    </div>
  );
}

function DateTimeInput({
  value,
  onChange,
  disabled,
}: {
  value: string;
  onChange: (v: string) => void;
  disabled?: boolean;
}) {
  return (
    <div className="w-full h-[44px] rounded-[10px] bg-[#F3F1ED] border border-black/10 px-4 flex items-center justify-between gap-3">
      <span className="text-[#9CA3AF] text-[13px] font-bold">سنة-شهر-يوم — ساعة:دقيقة</span>

      <input
        type="datetime-local"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        disabled={disabled}
        className="bg-transparent outline-none text-right disabled:opacity-50 text-[14px] font-sans font-bold text-[#2F2F2F] flex-1"
      />
    </div>
  );
}

function Select({
  value,
  onChange,
  placeholder,
  options,
  required,
  disabled,
  hasError,
}: {
  value: string;
  onChange: (v: string) => void;
  placeholder: string;
  options: { value: string; label: string }[];
  required?: boolean;
  disabled?: boolean;
  hasError?: boolean;
}) {
  return (
    <div className="relative">
      <select
        value={value}
        onChange={(e) => onChange(e.target.value)}
        required={required}
        disabled={disabled}
        className={[
          `w-full h-[44px] rounded-[10px] bg-[#F3F1ED] border ${hasError ? "border-red-400" : "border-black/10"} px-4`,
          "text-right outline-none focus:ring-2 focus:ring-black/10",
          value ? "text-[#111827]" : "text-[#9CA3AF]",
          "appearance-none disabled:opacity-50",
        ].join(" ")}
      >
        <option value="">{placeholder}</option>
        {options.map((o) => (
          <option key={o.value} value={o.value}>
            {o.label}
          </option>
        ))}
      </select>

      <div className="pointer-events-none absolute left-4 top-1/2 -translate-y-1/2 text-[#7895B2]">
        <ChevronDown />
      </div>
    </div>
  );
}

function ChevronDown() {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" aria-hidden="true">
      <path
        d="M6 9l6 6 6-6"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}
