import React, { useEffect, useState } from "react";
import { getWorkers } from "../api/users";
import { getZones } from "../api/zones";
import { createTask } from "../api/tasks";
import type { UserResponse } from "../types/user";
import type { ZoneResponse } from "../types/zone";
import type { TaskPriority } from "../types/task";

type PriorityOption = { value: TaskPriority; label: string };

const priorityOptions: PriorityOption[] = [
  { value: "Low", label: "منخفضة" },
  { value: "Medium", label: "متوسطة" },
  { value: "High", label: "عالية" },
  { value: "Urgent", label: "عاجلة" },
];

export default function CreateTask() {
  const [employees, setEmployees] = useState<UserResponse[]>([]);
  const [zones, setZones] = useState<ZoneResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [dueDate, setDueDate] = useState("");
  const [priority, setPriority] = useState<TaskPriority | "">("");
  const [employeeId, setEmployeeId] = useState("");
  const [zoneId, setZoneId] = useState("");

  useEffect(() => {
    const fetchData = async () => {
      try {
        const [workersData, zonesData] = await Promise.all([
          getWorkers(),
          getZones(),
        ]);
        setEmployees(workersData);
        setZones(zonesData);
      } catch (err) {
        setError("فشل في تحميل البيانات");
      } finally {
        setLoading(false);
      }
    };
    fetchData();
  }, []);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setSuccess("");
    setSubmitting(true);

    try {
      await createTask({
        title,
        description,
        assignedToUserId: parseInt(employeeId),
        zoneId: zoneId ? parseInt(zoneId) : undefined,
        priority: priority as TaskPriority,
        dueDate: dueDate || undefined,
      });

      setSuccess("تم إنشاء المهمة بنجاح");
      // Reset form
      setTitle("");
      setDescription("");
      setDueDate("");
      setPriority("");
      setEmployeeId("");
      setZoneId("");
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
    <div className="h-full w-full bg-[#D9D9D9] overflow-auto">
      <div className="p-4 sm:p-6 md:p-8">
        <div className="max-w-[980px] mx-auto">
          <h1 className="text-right font-sans font-semibold text-[20px] sm:text-[22px] text-[#2F2F2F] mb-6">
            تعيين مهمة جديدة
          </h1>

          {error && (
            <div className="mb-4 p-3 bg-red-100 text-red-700 rounded-[10px] text-right">
              {error}
            </div>
          )}

          {success && (
            <div className="mb-4 p-3 bg-green-100 text-green-700 rounded-[10px] text-right">
              {success}
            </div>
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

              <FieldRow label="موعد التنفيذ">
                <DateInput value={dueDate} onChange={setDueDate} disabled={submitting} />
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

              <FieldRow label="الموظف">
                <Select
                  value={employeeId}
                  onChange={setEmployeeId}
                  placeholder="اسم الموظف..."
                  options={employees.map((e) => ({
                    value: e.userId.toString(),
                    label: e.fullName,
                  }))}
                  required
                  disabled={submitting}
                />
              </FieldRow>

              <FieldRow label="الموقع">
                <Select
                  value={zoneId}
                  onChange={setZoneId}
                  placeholder="حدد موقع المهمة"
                  options={zones.map((z) => ({
                    value: z.zoneId.toString(),
                    label: z.zoneName,
                  }))}
                  disabled={submitting}
                />
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

function FieldRow({
  label,
  children,
}: {
  label: string;
  children: React.ReactNode;
}) {
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

function DateInput({
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
      <span className="text-[#9CA3AF] text-[14px]">Month Day\Year</span>

      <input
        type="date"
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
