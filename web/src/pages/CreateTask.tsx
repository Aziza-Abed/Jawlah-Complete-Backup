import React, { useEffect, useMemo, useState } from "react";
import { useLocation } from "react-router-dom";

// TODO backend: re-enable APIs when backend is available
// import { getWorkers } from "../api/users";
// import { getZones } from "../api/zones";
// import { createTask } from "../api/tasks";

import type { UserResponse } from "../types/user";
import type { ZoneResponse } from "../types/zone";
import type { TaskPriority } from "../types/task";

import { MapContainer, TileLayer, Marker, Polygon, useMap, useMapEvents } from "react-leaflet";
import L from "leaflet";
import "leaflet/dist/leaflet.css";

type PriorityOption = { value: TaskPriority; label: string };

const priorityOptions: PriorityOption[] = [
  { value: "Low", label: "منخفضة" },
  { value: "Medium", label: "متوسطة" },
  { value: "High", label: "عالية" },
];

type LatLng = { lat: number; lng: number };

// Shape coming from IssueDetails navigate state
type FromIssue = {
  issueId?: string;
  title?: string;
  description?: string;
  zoneId?: number | string;
  zone?: string;
  locationText?: string;
  severity?: "low" | "medium" | "high" | "critical";
  type?: string;
  images?: string[];
  gps?: { lat: number; lng: number };
};

/* -------------------- NEW: GeoJSON Types + Converter -------------------- */
/**
 * GeoJSON returned from backend (as partner said):
 * - Polygon: coordinates: [ [ [lng,lat], [lng,lat], ... ] ]  (outer ring first)
 * - MultiPolygon: coordinates: [ [ [ [lng,lat], ... ] ] , ... ]
 */
type GeoJSONPolygon = {
  type: "Polygon";
  coordinates: number[][][]; // [ring][point][lng,lat]
};

type GeoJSONMultiPolygon = {
  type: "MultiPolygon";
  coordinates: number[][][][]; // [poly][ring][point][lng,lat]
};

type GeoJSONBoundary = GeoJSONPolygon | GeoJSONMultiPolygon;

function geoJsonToLatLngs(geo?: GeoJSONBoundary | null): LatLng[] | null {
  if (!geo) return null;

  if (geo.type === "Polygon") {
    const outer = geo.coordinates?.[0];
    if (!outer || outer.length < 3) return null;
    return outer.map(([lng, lat]) => ({ lat, lng }));
  }

  if (geo.type === "MultiPolygon") {
    // take first polygon's outer ring
    const outer = geo.coordinates?.[0]?.[0];
    if (!outer || outer.length < 3) return null;
    return outer.map(([lng, lat]) => ({ lat, lng }));
  }

  return null;
}
/* ----------------------------------------------------------------------- */

const markerIcon = new L.Icon({
  iconUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png",
  iconRetinaUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png",
  shadowUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png",
  iconSize: [25, 41],
  iconAnchor: [12, 41],
});

function pointInPolygon(point: LatLng, polygon: LatLng[]) {
  // Ray-casting algorithm
  const x = point.lng;
  const y = point.lat;

  let inside = false;
  for (let i = 0, j = polygon.length - 1; i < polygon.length; j = i++) {
    const xi = polygon[i].lng;
    const yi = polygon[i].lat;
    const xj = polygon[j].lng;
    const yj = polygon[j].lat;

    const intersect = yi > y !== yj > y && x < ((xj - xi) * (y - yi)) / (yj - yi + 0.0) + xi;

    if (intersect) inside = !inside;
  }
  return inside;
}

function polygonBounds(poly: LatLng[]) {
  const lats = poly.map((p) => p.lat);
  const lngs = poly.map((p) => p.lng);
  const minLat = Math.min(...lats);
  const maxLat = Math.max(...lats);
  const minLng = Math.min(...lngs);
  const maxLng = Math.max(...lngs);
  return L.latLngBounds([minLat, minLng], [maxLat, maxLng]);
}

export default function CreateTask() {
  const location = useLocation();
  const fromIssue = (location.state as any)?.fromIssue as FromIssue | undefined;

  const [employees, setEmployees] = useState<UserResponse[]>([]);
  const [zones, setZones] = useState<ZoneResponse[]>([]);
  const [loading, setLoading] = useState(true);

  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");

  const [startAt, setStartAt] = useState("");
  const [endAt, setEndAt] = useState("");

  const [priority, setPriority] = useState<TaskPriority | "">("");

  const [employeeIds, setEmployeeIds] = useState<string[]>([]);
  const [employeesPickerOpen, setEmployeesPickerOpen] = useState(false);

  const [zoneId, setZoneId] = useState("");

  // Map location (optional) — enabled only after selecting a zone
  const [pickedLocation, setPickedLocation] = useState<LatLng | null>(null);

  const [mapError, setMapError] = useState("");

  const mapSeverityToPriority = (sev?: FromIssue["severity"]): TaskPriority | "" => {
    if (!sev) return "";
    if (sev === "critical" || sev === "high") return "High";
    if (sev === "medium") return "Medium";
    return "Low";
  };

  const isMapEnabled = Boolean(zoneId);

  useEffect(() => {
    const fetchData = async () => {
      try {
        // TODO backend: enable loading workers and zones
        // const [workersData, zonesData] = await Promise.all([getWorkers(), getZones()]);
        // setEmployees(workersData);
        // setZones(zonesData);

        // Temporary front-end placeholders (development only)
        setEmployees([
          { userId: 101, fullName: "أحمد علي" } as UserResponse,
          { userId: 102, fullName: "محمد سليم" } as UserResponse,
          { userId: 103, fullName: "سارة حسن" } as UserResponse,
          { userId: 104, fullName: "يزن محمود" } as UserResponse,
          { userId: 105, fullName: "لينا أحمد" } as UserResponse,
        ]);

        // Mock zones (dev) — you can keep boundaryPoints OR replace with GeoJSON
        setZones([
          {
            zoneId: 1,
            zoneName: "المنطقة 1",
            // ✅ Example GeoJSON Polygon (lng,lat)
            boundary: {
              type: "Polygon",
              coordinates: [
                [
                  [35.2020, 31.9055],
                  [35.2120, 31.9055],
                  [35.2120, 31.9115],
                  [35.2020, 31.9115],
                  [35.2020, 31.9055],
                ],
              ],
            },
          } as any,
          {
            zoneId: 2,
            zoneName: "المنطقة 2",
            boundaryPoints: [
              { lat: 31.8965, lng: 35.2005 },
              { lat: 31.9035, lng: 35.2005 },
              { lat: 31.9035, lng: 35.2125 },
              { lat: 31.8965, lng: 35.2125 },
            ],
          } as any,
          {
            zoneId: 3,
            zoneName: "المنطقة 3",
            boundaryPoints: [
              { lat: 31.9050, lng: 35.2140 },
              { lat: 31.9120, lng: 35.2140 },
              { lat: 31.9120, lng: 35.2260 },
              { lat: 31.9050, lng: 35.2260 },
            ],
          } as any,
          {
            zoneId: 4,
            zoneName: "المنطقة 4",
            boundaryPoints: [
              { lat: 31.8970, lng: 35.2140 },
              { lat: 31.9040, lng: 35.2140 },
              { lat: 31.9040, lng: 35.2260 },
              { lat: 31.8970, lng: 35.2260 },
            ],
          } as any,
          {
            zoneId: 5,
            zoneName: "المنطقة 5",
            boundaryPoints: [
              { lat: 31.8885, lng: 35.2020 },
              { lat: 31.8955, lng: 35.2020 },
              { lat: 31.8955, lng: 35.2140 },
              { lat: 31.8885, lng: 35.2140 },
            ],
          } as any,
        ]);
      } catch (err) {
        setError("فشل في تحميل البيانات");
      } finally {
        setLoading(false);
      }
    };
    fetchData();
  }, []);

  // Prefill when coming from issue
  useEffect(() => {
    if (!fromIssue) return;

    if (fromIssue.title) setTitle(fromIssue.title);

    const parts: string[] = [];
    if (fromIssue.description) parts.push(fromIssue.description);
    if (fromIssue.type) parts.push(`نوع المشكلة: ${fromIssue.type}`);
    if (fromIssue.locationText) parts.push(`الموقع: ${fromIssue.locationText}`);
    if (fromIssue.issueId) parts.push(`(محوّلة من بلاغ رقم: ${fromIssue.issueId})`);

    const finalDesc = parts.filter(Boolean).join("\n");
    if (finalDesc) setDescription(finalDesc);

    const mapped = mapSeverityToPriority(fromIssue.severity);
    if (mapped) setPriority(mapped);

    if (fromIssue.zoneId !== undefined && fromIssue.zoneId !== null && String(fromIssue.zoneId).trim() !== "") {
      setZoneId(String(fromIssue.zoneId));
    }

    if (fromIssue.gps?.lat !== undefined && fromIssue.gps?.lng !== undefined) {
      setPickedLocation({ lat: fromIssue.gps.lat, lng: fromIssue.gps.lng });
    }
  }, [fromIssue]);

  // If we only received zone name, match it after zones are loaded
  useEffect(() => {
    if (!fromIssue) return;
    if (zoneId) return;
    if (!fromIssue.zone) return;
    if (zones.length === 0) return;

    const exact = zones.find((z) => (z as any).zoneName === fromIssue.zone);
    if (exact) {
      setZoneId(String((exact as any).zoneId));
      return;
    }

    const relaxed = zones.find(
      (z) =>
        String((z as any).zoneName).includes(fromIssue.zone as string) ||
        String(fromIssue.zone as string).includes((z as any).zoneName)
    );
    if (relaxed) {
      setZoneId(String((relaxed as any).zoneId));
    }
  }, [fromIssue, zones, zoneId]);

  const selectedZone = useMemo(() => {
    if (!zoneId) return null;
    return zones.find((z) => String((z as any).zoneId) === String(zoneId)) as any;
  }, [zones, zoneId]);

  // ✅ UPDATED: Now supports GeoJSON boundary first, then boundaryPoints fallback
  const zoneBoundary: LatLng[] | null = useMemo(() => {
    if (!selectedZone) return null;

    // Option A (Preferred): GeoJSON boundary/geometry (Polygon/MultiPolygon)
    // backend ممكن يسميها: boundary أو geometry — خلينا ندعم الاثنين بدون تخريب
    const maybeGeo = (selectedZone.boundary ?? selectedZone.geometry) as GeoJSONBoundary | undefined;
    const fromGeo = geoJsonToLatLngs(maybeGeo);
    if (fromGeo && fromGeo.length >= 3) return fromGeo;

    // Option B: boundaryPoints (array of lat/lng)
    if (Array.isArray(selectedZone.boundaryPoints) && selectedZone.boundaryPoints.length >= 3) {
      return selectedZone.boundaryPoints as LatLng[];
    }

    return null;
  }, [selectedZone]);

  // Clear map pin if zone changes (prevents mismatched zone/location)
  useEffect(() => {
    setPickedLocation(null);
    setMapError("");
  }, [zoneId]);

  const selectedEmployeesLabel = useMemo(() => {
    if (employeeIds.length === 0) return "";
    const names = employeeIds
      .map((id) => employees.find((e) => String((e as any).userId) === String(id))?.fullName)
      .filter(Boolean) as string[];

    if (names.length === 0) return "";
    if (names.length <= 2) return names.join("، ");
    return `${names.slice(0, 2).join("، ")} (+${names.length - 2})`;
  }, [employeeIds, employees]);

  const validate = () => {
    if (!title.trim()) return "الرجاء إدخال عنوان المهمة.";
    if (!description.trim()) return "الرجاء إدخال وصف المهمة.";
    if (!priority) return "الرجاء اختيار الأولوية.";
    if (employeeIds.length === 0) return "الرجاء اختيار موظف واحد على الأقل.";
    if (!zoneId) return "الرجاء اختيار المنطقة.";
    if (!startAt) return "الرجاء تحديد موعد البدء.";
    if (!endAt) return "الرجاء تحديد موعد الانتهاء.";

    const startMs = new Date(startAt).getTime();
    const endMs = new Date(endAt).getTime();
    if (!Number.isFinite(startMs) || !Number.isFinite(endMs)) return "صيغة التاريخ/الوقت غير صحيحة.";
    if (endMs <= startMs) return "موعد الانتهاء يجب أن يكون بعد موعد البدء.";

    return "";
  };

  const toggleEmployee = (id: string) => {
    setEmployeeIds((prev) => (prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id]));
  };

  const clearEmployees = () => setEmployeeIds([]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setSuccess("");

    const v = validate();
    if (v) {
      setError(v);
      return;
    }

    setSubmitting(true);
    try {
      const locationPayload = pickedLocation ? { lat: pickedLocation.lat, lng: pickedLocation.lng } : undefined;

      // TODO backend: enable createTask API call
      // await createTask({
      //   title,
      //   description,
      //   assignedToUserIds: employeeIds.map((id) => Number(id)),
      //   zoneId: Number(zoneId),
      //   priority: priority as TaskPriority,
      //   startAt,
      //   endAt,
      //   location: locationPayload,
      //   issueId: fromIssue?.issueId ? String(fromIssue.issueId) : undefined,
      // });

      await new Promise((r) => setTimeout(r, 600));

      setSuccess("تم إنشاء المهمة بنجاح");

      setTitle("");
      setDescription("");
      setStartAt("");
      setEndAt("");
      setPriority("");
      setEmployeeIds([]);
      setZoneId("");
      setPickedLocation(null);
      setEmployeesPickerOpen(false);
      setMapError("");
    } catch (err) {
      setError("فشل في إنشاء المهمة");
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) {
    return (
      <div className="h-full w-full bg-[#D9D9D9] flex items-center justify-center">
        <div className="text-[#2F2F2F]">جاري التحميل...</div>
      </div>
    );
  }

  return (
    <div className="h-full w-full bg-[#D9D9D9] overflow-auto" dir="rtl">
      <div className="p-4 sm:p-6 md:p-8">
        <div className="max-w-[980px] mx-auto">
          <h1 className="text-right font-sans font-semibold text-[20px] sm:text-[22px] text-[#2F2F2F] mb-6">
            تعيين مهمة جديدة
          </h1>

          {fromIssue?.issueId && (
            <div className="mb-4 p-3 bg-[#F3F1ED] border border-black/10 rounded-[10px] text-right text-[#2F2F2F]">
              تم تعبئة الحقول تلقائيًا من البلاغ: <span className="font-semibold">#{fromIssue.issueId}</span>
            </div>
          )}

          {error && <div className="mb-4 p-3 bg-red-100 text-red-700 rounded-[10px] text-right">{error}</div>}

          {success && (
            <div className="mb-4 p-3 bg-green-100 text-green-700 rounded-[10px] text-right">{success}</div>
          )}

          <form onSubmit={handleSubmit} className="w-full">
            <div className="grid grid-cols-1 gap-6">
              <FieldRow label="عنوان المهمة">
                <Input
                  value={title}
                  onChange={(e) => setTitle(e.target.value)}
                  placeholder="اسم المهمة"
                  required
                  disabled={submitting}
                />
              </FieldRow>

              <FieldRow label="وصف المهمة">
                <Textarea
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  placeholder="يرجى كتابة وصف مختصر للمهمة..."
                  required
                  disabled={submitting}
                />
              </FieldRow>

              <FieldRow label="موعد البدء">
                <DateTimeInput value={startAt} onChange={setStartAt} disabled={submitting} />
              </FieldRow>

              <FieldRow label="موعد الانتهاء">
                <DateTimeInput value={endAt} onChange={setEndAt} disabled={submitting} />
              </FieldRow>

              <FieldRow label="الأولوية">
                <Select
                  value={priority}
                  onChange={(v) => setPriority(v as TaskPriority)}
                  placeholder="اختر درجة الأهمية..."
                  options={priorityOptions.map((p) => ({ value: p.value, label: p.label }))}
                  required
                  disabled={submitting}
                />
              </FieldRow>

              <FieldRow label="الموظفون">
                <MultiSelectEmployees
                  disabled={submitting}
                  open={employeesPickerOpen}
                  onToggleOpen={() => setEmployeesPickerOpen((s) => !s)}
                  onClose={() => setEmployeesPickerOpen(false)}
                  selectedLabel={selectedEmployeesLabel}
                  selectedIds={employeeIds}
                  employees={employees}
                  onToggleEmployee={toggleEmployee}
                  onClear={clearEmployees}
                />
              </FieldRow>

              <FieldRow label="المنطقة (مطلوب)">
                <Select
                  value={zoneId}
                  onChange={setZoneId}
                  placeholder="حدد منطقة المهمة"
                  options={zones.map((z) => ({
                    value: String((z as any).zoneId),
                    label: (z as any).zoneName,
                  }))}
                  required
                  disabled={submitting}
                />
              </FieldRow>

              <FieldRow label="تحديد الموقع داخل المنطقة (اختياري)">
                <MapPicker
                  disabled={submitting || !isMapEnabled}
                  value={pickedLocation}
                  onChange={(v) => {
                    setMapError("");
                    setPickedLocation(v);
                  }}
                  isEnabled={isMapEnabled}
                  boundary={zoneBoundary}
                  onOutsideClick={() => setMapError("الرجاء اختيار نقطة داخل حدود المنطقة المحددة فقط.")}
                />

                {mapError ? (
                  <div className="mt-3 text-right text-[12px] font-sans font-semibold text-red-600">{mapError}</div>
                ) : null}
              </FieldRow>
            </div>

            <div className="mt-10 flex justify-center">
              <button
                type="submit"
                disabled={submitting}
                className="w-[220px] h-[56px] rounded-[10px] bg-[#60778E] text-white font-sans font-semibold text-[18px] shadow-[0_2px_0_rgba(0,0,0,0.15)] hover:opacity-95 active:opacity-90 disabled:opacity-50"
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

function FieldRow({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="grid grid-cols-1 md:grid-cols-[200px_1fr] items-start gap-3 md:gap-4">
      <div className="text-right font-sans font-semibold text-[#2F2F2F] text-[16px] sm:text-[17px] md:text-[18px] pt-2">
        {label}
      </div>

      <div className="flex">
        <div className="w-full max-w-[560px]">{children}</div>
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
}: {
  value: string;
  onChange: (e: React.ChangeEvent<HTMLInputElement>) => void;
  placeholder?: string;
  required?: boolean;
  disabled?: boolean;
}) {
  return (
    <input
      value={value}
      onChange={onChange}
      placeholder={placeholder}
      required={required}
      disabled={disabled}
      className="w-full h-[44px] rounded-[10px] bg-[#F3F1ED] border border-black/10 px-4 text-right outline-none focus:ring-2 focus:ring-black/10 disabled:opacity-50"
    />
  );
}

function Textarea({
  value,
  onChange,
  placeholder,
  required,
  disabled,
}: {
  value: string;
  onChange: (e: React.ChangeEvent<HTMLTextAreaElement>) => void;
  placeholder?: string;
  required?: boolean;
  disabled?: boolean;
}) {
  return (
    <textarea
      value={value}
      onChange={onChange}
      placeholder={placeholder}
      required={required}
      disabled={disabled}
      rows={3}
      className="w-full min-h-[78px] rounded-[10px] bg-[#F3F1ED] border border-black/10 px-4 py-3 text-right outline-none resize-none focus:ring-2 focus:ring-black/10 disabled:opacity-50"
    />
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
      <span className="text-[#9CA3AF] text-[14px]">سنة-شهر-يوم — ساعة:دقيقة</span>

      <input
        type="datetime-local"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        disabled={disabled}
        className="bg-transparent outline-none text-right disabled:opacity-50"
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
}: {
  value: string;
  onChange: (v: string) => void;
  placeholder: string;
  options: { value: string; label: string }[];
  required?: boolean;
  disabled?: boolean;
}) {
  return (
    <div className="relative">
      <select
        value={value}
        onChange={(e) => onChange(e.target.value)}
        required={required}
        disabled={disabled}
        className={[
          "w-full h-[44px] rounded-[10px] bg-[#F3F1ED] border border-black/10 px-4",
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

      <div className="pointer-events-none absolute left-4 top-1/2 -translate-y-1/2 text-[#60778E]">
        <ChevronDown />
      </div>
    </div>
  );
}

function MultiSelectEmployees({
  disabled,
  open,
  onToggleOpen,
  onClose,
  selectedLabel,
  selectedIds,
  employees,
  onToggleEmployee,
  onClear,
}: {
  disabled?: boolean;
  open: boolean;
  onToggleOpen: () => void;
  onClose: () => void;
  selectedLabel: string;
  selectedIds: string[];
  employees: UserResponse[];
  onToggleEmployee: (id: string) => void;
  onClear: () => void;
}) {
  return (
    <div className="relative">
      <button
        type="button"
        disabled={disabled}
        onClick={onToggleOpen}
        className={[
          "w-full h-[44px] rounded-[10px] bg-[#F3F1ED] border border-black/10 px-4",
          "flex items-center justify-between gap-3",
          "outline-none focus:ring-2 focus:ring-black/10",
          "disabled:opacity-50",
        ].join(" ")}
      >
        <span className={selectedIds.length ? "text-[#111827] text-[14px]" : "text-[#9CA3AF] text-[14px]"}>
          {selectedIds.length ? selectedLabel : "اختر الموظفين..."}
        </span>
        <span className="text-[#60778E]">
          <ChevronDown />
        </span>
      </button>

      {open ? (
        <>
          <button type="button" className="fixed inset-0 z-40 cursor-default" onClick={onClose} aria-label="close" />
          <div className="absolute z-50 mt-2 w-full rounded-[12px] bg-white border border-black/10 shadow-[0_12px_30px_rgba(0,0,0,0.08)] overflow-hidden">
            <div className="p-3 flex items-center justify-between gap-3 border-b border-black/5">
              <div className="text-right text-[13px] font-sans font-semibold text-[#2F2F2F]">
                اختر الموظفين ({selectedIds.length})
              </div>
              <button
                type="button"
                onClick={onClear}
                className="text-[12px] font-sans font-semibold text-[#60778E] hover:opacity-80"
              >
                مسح
              </button>
            </div>

            <div className="max-h-[220px] overflow-auto p-2">
              {employees.map((e) => {
                const id = String((e as any).userId ?? "");
                const name = (e as any).fullName ?? "—";
                const checked = selectedIds.includes(id);

                return (
                  <label
                    key={id}
                    className="w-full flex items-center justify-between gap-3 px-3 py-2 rounded-[10px] hover:bg-[#F3F1ED] cursor-pointer"
                  >
                    <span className="text-right text-[14px] text-[#2F2F2F]">{name}</span>
                    <input
                      type="checkbox"
                      checked={checked}
                      onChange={() => onToggleEmployee(id)}
                      className="w-4 h-4 accent-[#60778E]"
                    />
                  </label>
                );
              })}
            </div>

            <div className="p-3 border-t border-black/5">
              <button
                type="button"
                onClick={onClose}
                className="w-full h-[40px] rounded-[10px] bg-[#60778E] text-white font-sans font-semibold text-[14px] hover:opacity-95"
              >
                تم
              </button>
            </div>
          </div>
        </>
      ) : null}
    </div>
  );
}

function MapPicker({
  disabled,
  value,
  onChange,
  isEnabled,
  boundary,
  onOutsideClick,
}: {
  disabled?: boolean;
  value: LatLng | null;
  onChange: (v: LatLng | null) => void;
  isEnabled: boolean;
  boundary: LatLng[] | null;
  onOutsideClick: () => void;
}) {
  const fallbackCenter = { lat: 31.905, lng: 35.205 };
  const center = value ?? (boundary && boundary.length ? boundary[0] : fallbackCenter);

  return (
    <div className="w-full">
      <div className="rounded-[14px] overflow-hidden bg-white border border-black/10">
        <div className={["h-[260px] sm:h-[320px] md:h-[360px]", disabled ? "opacity-60" : ""].join(" ")}>
          <MapContainer center={center} zoom={14} className="h-full w-full" scrollWheelZoom>
            <TileLayer attribution="&copy; OpenStreetMap" url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png" />

            {boundary && boundary.length >= 3 ? (
              <>
                <Polygon
                  positions={boundary.map((p) => [p.lat, p.lng])}
                  pathOptions={{ color: "#60778E", weight: 2, fillColor: "#60778E", fillOpacity: 0.12 }}
                />
                <FitToBoundary boundary={boundary} />
              </>
            ) : null}

            <ClickToPickWithBoundary
              disabled={disabled}
              boundary={boundary}
              onPick={(p) => onChange(p)}
              onOutsideClick={onOutsideClick}
            />

            {value ? <Marker position={value} icon={markerIcon} /> : null}
          </MapContainer>
        </div>
      </div>

      <div className="mt-3 flex items-center justify-between gap-3 flex-wrap">
        <div className="text-right text-[12px] text-[#6B7280]">
          {!isEnabled ? (
            "اختاري المنطقة أولًا لتفعيل الخريطة."
          ) : boundary && boundary.length >= 3 ? (
            value ? (
              <>
                تم تحديد الموقع:{" "}
                <span className="font-sans font-semibold text-[#2F2F2F]">
                  {value.lat.toFixed(5)}, {value.lng.toFixed(5)}
                </span>
              </>
            ) : (
              "اضغطي داخل حدود المنطقة لتحديد موقع المهمة."
            )
          ) : (
            "حدود المنطقة غير متوفرة حالياً."
          )}
        </div>

        <button
          type="button"
          disabled={disabled || !value}
          onClick={() => onChange(null)}
          className="h-[36px] px-4 rounded-[10px] bg-[#F3F1ED] border border-black/10 text-[#2F2F2F] text-[13px] font-sans font-semibold hover:opacity-90 disabled:opacity-50"
        >
          مسح التحديد
        </button>
      </div>
    </div>
  );
}

function FitToBoundary({ boundary }: { boundary: LatLng[] }) {
  const map = useMap();
  useEffect(() => {
    if (!boundary || boundary.length < 3) return;
    const b = polygonBounds(boundary);
    map.fitBounds(b, { padding: [18, 18] });
  }, [boundary, map]);
  return null;
}

function ClickToPickWithBoundary({
  disabled,
  boundary,
  onPick,
  onOutsideClick,
}: {
  disabled?: boolean;
  boundary: LatLng[] | null;
  onPick: (v: LatLng) => void;
  onOutsideClick: () => void;
}) {
  useMapEvents({
    click: (e) => {
      if (disabled) return;

      const p = { lat: e.latlng.lat, lng: e.latlng.lng };

      if (boundary && boundary.length >= 3) {
        const ok = pointInPolygon(p, boundary);
        if (!ok) {
          onOutsideClick();
          return;
        }
      }
      onPick(p);
    },
  });
  return null;
}

function ChevronDown() {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" aria-hidden="true">
      <path d="M6 9l6 6 6-6" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}
