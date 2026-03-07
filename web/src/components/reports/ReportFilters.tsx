import { Calendar, ChevronDown, Filter } from "lucide-react";
import type { FiltersDraft, PeriodPreset, TaskStatus } from "./types";

type ReportFiltersProps = {
  draft: FiltersDraft;
  setDraft: React.Dispatch<React.SetStateAction<FiltersDraft>>;
  onApply: () => void;
  onReset: () => void;
  showStatusFilter: boolean;
};

export default function ReportFilters({
  draft,
  setDraft,
  onApply,
  onReset,
  showStatusFilter,
}: ReportFiltersProps) {
  return (
    <div className="bg-white rounded-[24px] p-6 shadow-[0_4px_25px_rgba(0,0,0,0.03)] border border-black/5">
      <div className="flex flex-col xl:flex-row gap-8 items-end">
        <div className="flex-1 grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6 w-full">
          <Select
            label="الفترة الزمنية"
            value={draft.period}
            onChange={(v) => setDraft((p) => ({ ...p, period: v as PeriodPreset }))}
            options={[
              { value: "daily", label: "يومي" },
              { value: "weekly", label: "أسبوعي" },
              { value: "monthly", label: "شهري" },
              { value: "yearly", label: "سنوي" },
              { value: "custom", label: "مخصص" },
            ]}
          />

          <Select
            label="حالة المهمة"
            value={draft.status}
            onChange={(v) => setDraft((p) => ({ ...p, status: v as TaskStatus }))}
            options={[
              { value: "all", label: "الكل" },
              { value: "pending", label: "معلقة" },
              { value: "in_progress", label: "قيد التنفيذ" },
              { value: "underreview", label: "قيد المراجعة" },
              { value: "completed", label: "مكتملة" },
            ]}
            disabled={!showStatusFilter}
          />

          {draft.period === "custom" ? (
            <div className="grid grid-cols-2 gap-3 animate-in fade-in slide-in-from-top-2 duration-300">
               <DateField
                label="من تاريخ"
                value={draft.from}
                onChange={(v) => setDraft((p) => ({ ...p, from: v }))}
              />
              <DateField
                label="إلى تاريخ"
                value={draft.to}
                onChange={(v) => setDraft((p) => ({ ...p, to: v }))}
              />
            </div>
          ) : (
            <div className="flex flex-col justify-end">
               <span className="text-[10px] text-[#AFAFAF] font-black uppercase tracking-widest mb-2 text-right">ملاحظة الفلاتر</span>
               <p className="text-[12px] font-bold text-[#6B7280] text-right">يتم تطبيق الفلترة على كافة المخططات والجداول أدناه.</p>
            </div>
          )}
        </div>

        <div className="flex items-center gap-3 w-full xl:w-auto">
          <button
            type="button"
            onClick={onReset}
            className="flex-1 xl:flex-none h-[52px] px-8 rounded-[18px] bg-[#F9F8F6] text-[#6B7280] hover:bg-[#F3F1ED] transition-all font-black text-[14px]"
          >
            إعادة تعيين
          </button>
          <button
            type="button"
            onClick={onApply}
            className="flex-1 xl:flex-none h-[52px] px-10 rounded-[18px] bg-[#7895B2] hover:bg-[#647e99] text-white font-black shadow-lg shadow-[#7895B2]/20 transition-all active:scale-95 flex items-center justify-center gap-3 group"
          >
            <Filter size={18} className="group-hover:rotate-12 transition-transform" />
            تطبيق الفلترة
          </button>
        </div>
      </div>
    </div>
  );
}

/* ---------- Private Sub-components ---------- */

function Select({
  label,
  value,
  onChange,
  options,
  disabled,
}: {
  label: string;
  value: string;
  onChange: (v: string) => void;
  options: { value: string; label: string }[];
  disabled?: boolean;
}) {
  return (
    <div className="w-full relative">
      <div className="text-right text-[10px] text-[#AFAFAF] font-black uppercase tracking-widest mb-2">
        {label}
      </div>
      <div className="relative">
        <select
          value={value}
          onChange={(e) => onChange(e.target.value)}
          disabled={disabled}
          className={[
            "w-full h-[52px] pr-6 pl-12 bg-[#F9F8F6] rounded-[18px] text-[14px] font-black text-[#2F2F2F] border-0 outline-none focus:ring-4 focus:ring-[#7895B2]/10 transition-all appearance-none cursor-pointer",
            disabled ? "opacity-40 cursor-not-allowed" : "",
          ].join(" ")}
        >
          {options.map((o) => (
            <option key={o.value} value={o.value}>
              {o.label}
            </option>
          ))}
        </select>

        <div className="pointer-events-none absolute left-4 top-1/2 -translate-y-1/2 text-[#AFAFAF]">
          <ChevronDown size={18} />
        </div>
      </div>
    </div>
  );
}

function DateField({
  label,
  value,
  onChange,
}: {
  label: string;
  value: string;
  onChange: (v: string) => void;
}) {
  return (
    <div className="w-full">
      <div className="text-right text-[10px] text-[#AFAFAF] font-black uppercase tracking-widest mb-2">
        {label}
      </div>
      <div className="relative">
        <input
            type="date"
            value={value}
            onChange={(e) => onChange(e.target.value)}
            className="w-full h-[52px] pr-6 pl-12 bg-[#F9F8F6] rounded-[18px] text-[14px] font-black text-[#2F2F2F] border-0 outline-none focus:ring-4 focus:ring-[#7895B2]/10 transition-all text-right"
        />
        <Calendar size={18} className="absolute left-4 top-1/2 -translate-y-1/2 text-[#AFAFAF] pointer-events-none" />
      </div>
    </div>
  );
}
