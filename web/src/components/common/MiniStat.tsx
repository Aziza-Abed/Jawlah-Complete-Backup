// Reusable stat card used across Tasks, Issues, MyWorkers, LocationHistory
export default function MiniStat({ title, value, color }: { title: string; value: string; color?: string }) {
  return (
    <div className="bg-[#F3F1ED] rounded-[14px] border border-black/10 shadow-[0_2px_0_rgba(0,0,0,0.06)] p-4">
      <div className="text-right text-[12px] text-[#6B7280] font-sans font-semibold">{title}</div>
      <div className="mt-2 text-right text-[18px] text-[#2F2F2F] font-sans font-bold" style={color ? { color } : undefined}>{value}</div>
    </div>
  );
}
