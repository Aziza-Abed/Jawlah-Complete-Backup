import React, { useEffect, useState } from "react";
import { MapPin, User, Pin } from "lucide-react";
import { getDashboardOverview } from "../api/dashboard";
import type { DashboardOverview } from "../types/dashboard";

type StatChip = {
  label: string;
  value: number;
  bg: string;
  text: string;
};

type ActivityItem = {
  id: string;
  time: string;
  text: string;
  icon: "pin" | "user" | "map";
};

const Dashboard: React.FC = () => {
  const [overview, setOverview] = useState<DashboardOverview | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    const fetchData = async () => {
      try {
        const data = await getDashboardOverview();
        setOverview(data);
      } catch (err) {
        setError("فشل في تحميل البيانات");
      } finally {
        setLoading(false);
      }
    };
    fetchData();
  }, []);

  // Calculate values from API data or use defaults
  const workersTotal = overview?.workers.total ?? 0;
  const workersChips: StatChip[] = [
    { label: "غياب", value: overview?.workers.notCheckedIn ?? 0, bg: "#C86E5D", text: "#FFFFFF" },
    { label: "حضور", value: overview?.workers.checkedIn ?? 0, bg: "#8FA36A", text: "#FFFFFF" },
  ];

  const tasksTotal = (overview?.tasks.pending ?? 0) + (overview?.tasks.inProgress ?? 0) + (overview?.tasks.completedToday ?? 0);
  const tasksChips: StatChip[] = [
    { label: "قيد التنفيذ", value: overview?.tasks.inProgress ?? 0, bg: "#F5B300", text: "#1F2937" },
    { label: "مكتملة", value: overview?.tasks.completedToday ?? 0, bg: "#C86E5D", text: "#FFFFFF" },
    { label: "معلقة", value: overview?.tasks.pending ?? 0, bg: "#8FA36A", text: "#FFFFFF" },
  ];

  const activities: ActivityItem[] = [
    {
      id: "a1",
      time: "09:15 ص",
      text: "آخر مهمة أُسندت للعامل",
      icon: "pin",
    },
    {
      id: "a2",
      time: "09:12 ص",
      text: "أبو عمار سجّل دخول",
      icon: "user",
    },
    {
      id: "a3",
      time: "09:05 ص",
      text: "آخر منطقة تم تحديثها",
      icon: "map",
    },
  ];

  if (loading) {
    return (
      <div className="h-full w-full bg-[#D9D9D9] flex items-center justify-center">
        <div className="text-[#2F2F2F]">جاري التحميل...</div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="h-full w-full bg-[#D9D9D9] flex items-center justify-center">
        <div className="text-red-500">{error}</div>
      </div>
    );
  }

  return (
    <div className="h-full w-full bg-[#D9D9D9] overflow-auto">
      <div className="p-4 sm:p-6 md:p-8">
        <div className="max-w-[980px] mx-auto">
          <h1 className="text-right font-sans font-semibold text-[20px] sm:text-[22px] text-[#2F2F2F] mb-4">
            لوحة التحكم
          </h1>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
            <StatCard
              title="إجمالي العمال"
              total={workersTotal}
              donut={{
                parts: [
                  { value: workersChips[0].value, color: workersChips[0].bg },
                  { value: workersChips[1].value, color: workersChips[1].bg },
                ],
              }}
              chips={workersChips}
            />

            <StatCard
              title="المهام الحالية"
              total={tasksTotal}
              donut={{
                parts: [
                  { value: tasksChips[0].value, color: tasksChips[0].bg },
                  { value: tasksChips[1].value, color: tasksChips[1].bg },
                  { value: tasksChips[2].value, color: tasksChips[2].bg },
                ],
              }}
              chips={tasksChips}
            />
          </div>

          <div className="mt-6 grid grid-cols-1 lg:grid-cols-2 gap-6">
            <CardShell>
              <div className="flex items-center gap-2">
                <Pin size={18} className="text-[#7895B2]" />
                <h2 className="text-right font-sans font-semibold text-[18px] text-[#2F2F2F]">
                    آخر الأنشطة:
                </h2>
              </div>

              <div className="mt-4 space-y-5">
                {activities.map((a) => (
                  <div key={a.id} className="flex items-start gap-3">
                    <div className="mt-1 shrink-0">
                      {a.icon === "pin" && (
                        <PinIcon className="text-[#7895B2]" />
                      )}
                      {a.icon === "user" && (
                        <User size={18} className="text-[#7895B2]" />
                      )}
                      {a.icon === "map" && (
                        <MapPin size={18} className="text-[#7895B2]" />
                      )}
                    </div>

                    <div className="flex-1">
                      <div className="text-right text-[12px] text-[#9CA3AF]">
                        {a.time}
                      </div>
                      <div className="text-right text-[14px] sm:text-[15px] text-[#2F2F2F]">
                        {a.text}
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            </CardShell>

            <CardShell>
              <h2 className="text-right font-sans font-semibold text-[18px] text-[#2F2F2F]">
                مواقع العمال والمهام:
              </h2>

              <div className="mt-4 rounded-[14px] overflow-hidden bg-white border border-black/10">
                <iframe
                  title="map"
                  className="w-full h-[290px] sm:h-[320px] md:h-[360px]"
                  src="https://www.openstreetmap.org/export/embed.html?bbox=35.1799%2C31.8850%2C35.2600%2C31.9400&layer=mapnik"
                />
              </div>
            </CardShell>
          </div>
        </div>
      </div>
    </div>
  );
};

export default Dashboard;

/* ---------- Components ---------- */

function CardShell({ children }: { children: React.ReactNode }) {
  return (
    <div className="bg-[#F3F1ED] rounded-[16px] shadow-[0_2px_0_rgba(0,0,0,0.08)] border border-black/10 p-5 sm:p-6">
      {children}
    </div>
  );
}

function StatCard({
  title,
  total,
  donut,
  chips,
}: {
  title: string;
  total: number;
  donut: { parts: { value: number; color: string }[] };
  chips: StatChip[];
}) {
  return (
    <CardShell>
      <div className="flex items-center justify-between gap-4">
        {/* Text on the right */}
        <div className="flex-1">
          <div className="text-right text-[14px] sm:text-[15px] text-[#2F2F2F]">
            {title} :{" "}
            <span className="font-semibold text-[#2F2F2F]">{total}</span>
          </div>

          <div className="mt-3 flex items-center justify-end gap-3 flex-wrap">
            {chips.map((c) => (
              <Chip
                key={c.label}
                value={c.value}
                label={c.label}
                bg={c.bg}
                text={c.text}
              />
            ))}
          </div>
        </div>

        {/* Donut on the left */}
        <div className="shrink-0">
          <Donut
            size={54}
            stroke={10}
            parts={donut.parts}
            background="#E5E7EB"
          />
        </div>
      </div>
    </CardShell>
  );
}

function Chip({
  value,
  label,
  bg,
  text,
}: {
  value: number;
  label: string;
  bg: string;
  text: string;
}) {
  return (
    <div className="flex flex-col items-center justify-center">
      <div
        className="min-w-[44px] px-3 py-1 rounded-full text-center text-[12px] font-semibold"
        style={{ backgroundColor: bg, color: text }}
      >
        {value}
      </div>
      <div className="mt-1 text-[11px] text-[#2F2F2F]">{label}</div>
    </div>
  );
}

function Donut({
  size,
  stroke,
  parts,
  background,
}: {
  size: number;
  stroke: number;
  parts: { value: number; color: string }[];
  background: string;
}) {
  const r = (size - stroke) / 2;
  const c = 2 * Math.PI * r;
  const total = parts.reduce((acc, p) => acc + p.value, 0) || 1;

  let offset = 0;

  return (
    <svg width={size} height={size} viewBox={`0 0 ${size} ${size}`}>
      <circle
        cx={size / 2}
        cy={size / 2}
        r={r}
        fill="none"
        stroke={background}
        strokeWidth={stroke}
      />
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
            strokeLinecap="butt"
            strokeDasharray={dashArray}
            strokeDashoffset={dashOffset}
            transform={`rotate(-90 ${size / 2} ${size / 2})`}
          />
        );
      })}
      <circle cx={size / 2} cy={size / 2} r={r - stroke / 2} fill="#F3F1ED" />
    </svg>
  );
}

function PinIcon({ className }: { className?: string }) {
  return (
    <svg
      width="18"
      height="18"
      viewBox="0 0 24 24"
      className={className}
      fill="none"
      aria-hidden="true"
    >
      <path
        d="M12 22s7-4.5 7-11a7 7 0 1 0-14 0c0 6.5 7 11 7 11Z"
        stroke="currentColor"
        strokeWidth="1.6"
      />
      <path
        d="M12 13.5a2.5 2.5 0 1 0 0-5 2.5 2.5 0 0 0 0 5Z"
        stroke="currentColor"
        strokeWidth="1.6"
      />
    </svg>
  );
}
