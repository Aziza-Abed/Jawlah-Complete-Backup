import React, { useMemo, useState } from "react";

type Priority = "منخفضة" | "متوسطة" | "عالية";

type EmployeeOption = { id: string; name: string };
type LocationOption = { id: string; name: string };

export default function CreateTask() {
  // TODO: Replace with API data from backend
  const employees: EmployeeOption[] = useMemo(
    () => [
      { id: "e1", name: "أبو عمار" },
      { id: "e2", name: "محمد" },
      { id: "e3", name: "أحمد" },
    ],
    []
  );

  // TODO: Replace with API data from backend (zones/locations)
  const locations: LocationOption[] = useMemo(
    () => [
      { id: "z1", name: "دوار المنارة" },
      { id: "z2", name: "شارع الإرسال" },
      { id: "z3", name: "البلدة القديمة" },
    ],
    []
  );

  const priorities: Priority[] = ["منخفضة", "متوسطة", "عالية"];

  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [dueDate, setDueDate] = useState("");
  const [priority, setPriority] = useState<Priority | "">("");
  const [employeeId, setEmployeeId] = useState("");
  const [locationId, setLocationId] = useState("");

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();

    // TODO: Send payload to backend
    // NOTE: Suggested payload shape:
    // {
    //   title,
    //   description,
    //   dueDate,
    //   priority,
    //   employeeId,
    //   locationId
    // }
    console.log({
      title,
      description,
      dueDate,
      priority,
      employeeId,
      locationId,
    });
  };

  return (
    <div className="h-full w-full bg-[#D9D9D9] overflow-auto">
      <div className="p-4 sm:p-6 md:p-8">
        <div className="max-w-[980px] mx-auto">
          <h1 className="text-right font-sans font-semibold text-[20px] sm:text-[22px] text-[#2F2F2F] mb-6">
            تعيين مهمة جديدة
          </h1>

          <form onSubmit={handleSubmit} className="w-full">
            <div className="grid grid-cols-1 gap-6">
              <FieldRow label="عنوان المهمة">
                <Input
                  value={title}
                  onChange={(e) => setTitle(e.target.value)}
                  placeholder="اسم المهمة"
                />
              </FieldRow>

              <FieldRow label="وصف المهمة">
                <Textarea
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  placeholder="يرجى كتابة وصف مختصر للمهمة..."
                />
              </FieldRow>

              <FieldRow label="موعد التنفيذ">
                <DateInput value={dueDate} onChange={setDueDate} />
              </FieldRow>

              <FieldRow label="الأولوية">
                <Select
                  value={priority}
                  onChange={(v) => setPriority(v as Priority)}
                  placeholder="اختر درجة الأهمية..."
                  options={priorities.map((p) => ({ value: p, label: p }))}
                />
              </FieldRow>

              <FieldRow label="الموظف">
                <Select
                  value={employeeId}
                  onChange={setEmployeeId}
                  placeholder="اسم الموظف..."
                  options={employees.map((e) => ({
                    value: e.id,
                    label: e.name,
                  }))}
                />
              </FieldRow>

              <FieldRow label="الموقع">
                <Select
                  value={locationId}
                  onChange={setLocationId}
                  placeholder="حدد موقع المهمة"
                  options={locations.map((z) => ({
                    value: z.id,
                    label: z.name,
                  }))}
                />
              </FieldRow>
            </div>

            <div className="mt-10 flex justify-center">
              <button
                type="submit"
                className="w-[220px] h-[56px] rounded-[10px] bg-[#60778E] text-white font-sans font-semibold text-[18px] shadow-[0_2px_0_rgba(0,0,0,0.15)] hover:opacity-95 active:opacity-90"
              >
                تعيين المهمة
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
}: {
  value: string;
  onChange: (e: React.ChangeEvent<HTMLInputElement>) => void;
  placeholder?: string;
}) {
  return (
    <input
      value={value}
      onChange={onChange}
      placeholder={placeholder}
      className="w-full h-[44px] rounded-[10px] bg-[#F3F1ED] border border-black/10 px-4 text-right outline-none focus:ring-2 focus:ring-black/10"
    />
  );
}

function Textarea({
  value,
  onChange,
  placeholder,
}: {
  value: string;
  onChange: (e: React.ChangeEvent<HTMLTextAreaElement>) => void;
  placeholder?: string;
}) {
  return (
    <textarea
      value={value}
      onChange={onChange}
      placeholder={placeholder}
      rows={3}
      className="w-full min-h-[78px] rounded-[10px] bg-[#F3F1ED] border border-black/10 px-4 py-3 text-right outline-none resize-none focus:ring-2 focus:ring-black/10"
    />
  );
}

function DateInput({
  value,
  onChange,
}: {
  value: string;
  onChange: (v: string) => void;
}) {
  return (
    <div className="w-full h-[44px] rounded-[10px] bg-[#F3F1ED] border border-black/10 px-4 flex items-center justify-between gap-3">
      <span className="text-[#9CA3AF] text-[14px]">Month Day\Year</span>

      <input
        type="date"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        className="bg-transparent outline-none text-right"
      />
    </div>
  );
}

function Select({
  value,
  onChange,
  placeholder,
  options,
}: {
  value: string;
  onChange: (v: string) => void;
  placeholder: string;
  options: { value: string; label: string }[];
}) {
  return (
    <div className="relative">
      <select
        value={value}
        onChange={(e) => onChange(e.target.value)}
        className={[
          "w-full h-[44px] rounded-[10px] bg-[#F3F1ED] border border-black/10 px-4",
          "text-right outline-none focus:ring-2 focus:ring-black/10",
          value ? "text-[#111827]" : "text-[#9CA3AF]",
          "appearance-none",
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
