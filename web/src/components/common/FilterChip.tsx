// Reusable filter chip used across Tasks, Issues, MyWorkers
export default function FilterChip({ active, onClick, label, count }: { active: boolean; onClick: () => void; label: string; count: number }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={[
        "h-[34px] px-3 rounded-[10px] font-sans font-semibold text-[13px] border flex items-center gap-2 transition-all",
        active ? "bg-[#7895B2] text-white border-black/10" : "bg-white text-[#2F2F2F] border-black/10 hover:opacity-95",
      ].join(" ")}
    >
      <span>{label}</span>
      <span className={["min-w-[20px] h-[20px] px-1 rounded-full text-[11px] font-bold grid place-items-center", active ? "bg-white/20" : "bg-black/5"].join(" ")}>
        {count}
      </span>
    </button>
  );
}
