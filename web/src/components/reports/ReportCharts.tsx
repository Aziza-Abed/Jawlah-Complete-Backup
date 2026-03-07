import type { SeriesPoint, LegendItem, DonutPart } from "./types";

/* ---------- Legend ---------- */

export function Legend({ items }: { items: LegendItem[] }) {
  return (
    <div className="flex items-center gap-4 flex-wrap justify-end">
      {items.map((it) => (
        <div key={it.label} className="flex items-center gap-1.5">
          <div className="text-[10px] font-bold text-[#6B7280] uppercase">{it.label}</div>
          <span className="w-2.5 h-2.5 rounded-full shadow-sm" style={{ backgroundColor: it.color }} />
        </div>
      ))}
    </div>
  );
}

/* ---------- Bar Chart (Grouped) ---------- */

export function BarChartGrouped({
  points,
  colors,
}: {
  points: SeriesPoint[];
  colors: { a: string; b: string; c?: string };
}) {
  const maxY = Math.max(1, ...points.flatMap((p) => [p.a, p.b, p.c ?? 0]));

  return (
    <div className="w-full bg-[#7895B2]/5 rounded-xl border border-[#7895B2]/5 p-4 overflow-hidden relative">
      <div className="absolute inset-0 bg-[url('https://grainy-gradients.vercel.app/noise.svg')] opacity-10"></div>
      <div className="h-[200px] sm:h-[240px] w-full flex items-end gap-3 relative z-10">
        {points.map((p) => {
          const aH = (p.a / maxY) * 100;
          const bH = (p.b / maxY) * 100;
          const cH = p.c != null ? (p.c / maxY) * 100 : 0;

          return (
            <div key={p.label} className="flex-1 min-w-[24px] h-full flex flex-col justify-end items-center group">
              <div className="h-full w-full flex items-end justify-center gap-1">
                <div className="w-[8px] sm:w-[10px] rounded-t-full transition-all duration-500 group-hover:scale-y-110 opacity-90" style={{ height: `${aH}%`, backgroundColor: colors.a }} />
                <div className="w-[8px] sm:w-[10px] rounded-t-full transition-all duration-500 group-hover:scale-y-110 opacity-90" style={{ height: `${bH}%`, backgroundColor: colors.b }} />
                {p.c != null && (
                  <div className="w-[8px] sm:w-[10px] rounded-t-full transition-all duration-500 group-hover:scale-y-110 opacity-90" style={{ height: `${cH}%`, backgroundColor: colors.c }} />
                )}
              </div>
              <div className="mt-3 text-[10px] font-bold text-[#6B7280] group-hover:text-white transition-colors">{p.label}</div>
            </div>
          );
        })}
      </div>
    </div>
  );
}

/* ---------- Horizontal Bars ---------- */

export function HorizontalBars({
  items,
}: {
  items: { label: string; value: number; color: string }[];
}) {
  const max = Math.max(1, ...items.map((x) => x.value));

  return (
    <div className="w-full bg-[#7895B2]/5 rounded-xl border border-[#7895B2]/5 p-5">
      <div className="space-y-4">
        {items.map((it) => {
          const w = Math.round((it.value / max) * 100);
          return (
            <div key={it.label} className="flex items-center gap-4">
              <div className="w-[32px] text-right text-xs font-bold font-sans text-[#2F2F2F]">{it.value}</div>
              <div className="flex-1 h-[6px] bg-[#7895B2]/10 rounded-full overflow-hidden">
                <div className="h-full rounded-full shadow-[0_0_8px_rgba(255,255,255,0.2)]" style={{ width: `${w}%`, backgroundColor: it.color }} />
              </div>
              <div className="w-[100px] text-right text-xs font-bold text-[#6B7280] truncate">{it.label}</div>
            </div>
          );
        })}
      </div>
    </div>
  );
}

/* ---------- Donut with Legend ---------- */

export function DonutWithLegend({ parts }: { parts: DonutPart[] }) {
  return (
    <div className="flex items-center justify-between gap-4">
      <div className="shrink-0 relative">
        <Donut size={100} stroke={12} parts={parts} background="rgba(255,255,255,0.05)" />
        <div className="absolute inset-0 flex items-center justify-center pointer-events-none">
            <span className="text-xs font-bold text-white/20">KPI</span>
        </div>
      </div>

      <div className="flex-1 space-y-3">
        {parts.map((p) => (
          <div key={p.label} className="flex items-center justify-between gap-2 border-b border-white/5 pb-2 last:border-0 last:pb-0">
            <div className="text-xs font-sans font-bold text-[#6B7280]">{p.value}%</div>
            <div className="flex items-center gap-2">
              <div className="text-xs font-bold text-[#6B7280]">{p.label}</div>
              <span className="w-2.5 h-2.5 rounded-full shadow-sm" style={{ backgroundColor: p.color }} />
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

/* ---------- Donut (SVG) ---------- */

function Donut({
  size,
  stroke,
  parts,
  background,
}: {
  size: number;
  stroke: number;
  parts: DonutPart[];
  background: string;
}) {
  const r = (size - stroke) / 2;
  const c = 2 * Math.PI * r;
  const total = parts.reduce((acc, p) => acc + p.value, 0) || 1;

  let offset = 0;

  return (
    <svg width={size} height={size} viewBox={`0 0 ${size} ${size}`}>
      <circle cx={size / 2} cy={size / 2} r={r} fill="none" stroke={background} strokeWidth={stroke} />
      {parts.map((p, idx) => {
        const frac = p.value / total;
        const dash = c * frac;
        const dashArray = `${dash} ${c - dash}`;
        const dashOffset = c * (1 - offset);
        offset += frac;

        return (
          <circle
            key={idx}
            cx={size / 2}
            cy={size / 2}
            r={r}
            fill="none"
            stroke={p.color}
            strokeWidth={stroke}
            strokeLinecap="round"
            strokeDasharray={dashArray}
            strokeDashoffset={dashOffset}
            transform={`rotate(-90 ${size / 2} ${size / 2})`}
            className="transition-all duration-1000 ease-out"
          />
        );
      })}
    </svg>
  );
}
