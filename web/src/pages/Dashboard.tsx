import React, { useEffect, useState } from "react";
import { MapPin, User, Pin, AlertTriangle, AlertCircle } from "lucide-react";
// import { getDashboardOverview } from "../api/dashboard";
import type { DashboardOverview } from "../types/dashboard";
// import { apiClient } from "../api/client";

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

type IssuesSummary = {
  new: number;
  open: number;
  closedToday: number;
};

type WarningItem = {
  id: string;
  text: string;
  severity: "high" | "medium" | "low";
};

const Dashboard: React.FC = () => {
  const [overview, setOverview] = useState<DashboardOverview | null>(null);
  const [issuesSummary, setIssuesSummary] = useState<IssuesSummary | null>(null);
  const [warnings, setWarnings] = useState<WarningItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [activities, setActivities] = useState<ActivityItem[]>([]);

  useEffect(() => {
    const fetchData = async () => {
      try {
        // TODO backend: re-enable dashboard overview when backend is available
        // const data = await getDashboardOverview();
        // setOverview(data);

        // TODO backend: load issues summary (new/open/closedToday)
        // Suggested endpoint: GET /issues/summary?range=today
        // setIssuesSummary(summary);

        // TODO backend: load warning signals (overdue tasks, no-checkin workers, stale issues)
        // Suggested endpoint: GET /dashboard/warnings?range=today
        // setWarnings(warningsFromApi);

        // TODO backend: re-enable audit logs when backend is available
        // const auditResponse = await apiClient.get("/audit?count=3");
        // map logs -> activities

        // Temporary front-end placeholders (development only)
        setOverview({
          workers: { total: 24, checkedIn: 18, notCheckedIn: 6 },
          tasks: { pending: 5, inProgress: 7, completedToday: 3 },
        } as DashboardOverview);

        setIssuesSummary({
          new: 4,
          open: 9,
          closedToday: 2,
        });

        setWarnings([
          { id: "w1", text: "5 مهام معلقة من أمس", severity: "high" },
          { id: "w2", text: "3 عمال لم يسجلوا حضورهم", severity: "medium" },
          { id: "w3", text: "بلاغ بدون معالجة منذ 4 ساعات", severity: "high" },
        ]);

        setActivities([
          { id: "1", time: "10:15 ص", text: "تم تعيين مهمة جديدة", icon: "pin" },
          { id: "2", time: "11:05 ص", text: "تم تسجيل دخول عامل", icon: "user" },
          { id: "3", time: "12:20 م", text: "تم تحديث منطقة على الخريطة", icon: "map" },
        ]);
      } catch (err) {
        setError("فشل في تحميل البيانات");
      } finally {
        setLoading(false);
      }
    };
    fetchData();
  }, []);

  const workersTotal = overview?.workers.total ?? 0;
  const workersChips: StatChip[] = [
    { label: "غياب", value: overview?.workers.notCheckedIn ?? 0, bg: "#C86E5D", text: "#FFFFFF" },
    { label: "حضور", value: overview?.workers.checkedIn ?? 0, bg: "#8FA36A", text: "#FFFFFF" },
  ];

  const tasksTotal =
    (overview?.tasks.pending ?? 0) +
    (overview?.tasks.inProgress ?? 0) +
    (overview?.tasks.completedToday ?? 0);

  const tasksChips: StatChip[] = [
    { label: "قيد التنفيذ", value: overview?.tasks.inProgress ?? 0, bg: "#F5B300", text: "#1F2937" },
    { label: "مكتملة", value: overview?.tasks.completedToday ?? 0, bg: "#C86E5D", text: "#FFFFFF" },
    { label: "معلقة", value: overview?.tasks.pending ?? 0, bg: "#8FA36A", text: "#FFFFFF" },
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
    <div className="h-full w-full bg-gradient-to-br from-[#E2E8F0] to-[#D9D9D9] overflow-auto">
      <div className="p-4 sm:p-6 md:p-8">
        <div className="max-w-[1100px] mx-auto">
          <h1 className="text-right font-sans font-bold text-[22px] sm:text-[24px] text-[#2F2F2F] mb-6">
            لوحة التحكم
          </h1>

          {/* Stats */}
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

          {/* Replaced Map with Issues + Warnings */}
          <div className="mt-6 grid grid-cols-1 lg:grid-cols-2 gap-6">
            <CardShell>
              <div className="flex items-center gap-2">
                <AlertCircle size={18} className="text-[#7895B2]" />
                <h2 className="text-right font-sans font-semibold text-[18px] text-[#2F2F2F]">
                  ملخص البلاغات:
                </h2>
              </div>

              <div className="mt-5 grid grid-cols-3 gap-3">
                <SummaryBox label="جديدة" value={issuesSummary?.new ?? 0} bg="#F5B300" text="#1F2937" />
                <SummaryBox label="مفتوحة" value={issuesSummary?.open ?? 0} bg="#C86E5D" text="#FFFFFF" />
                <SummaryBox label="مغلقة اليوم" value={issuesSummary?.closedToday ?? 0} bg="#8FA36A" text="#FFFFFF" />
              </div>

              <div className="mt-4 text-right text-[12px] text-[#6B7280]">
                {/* TODO backend: issues summary should reflect real-time counts */}
              </div>
            </CardShell>

            <CardShell>
              <div className="flex items-center gap-2">
                <AlertTriangle size={18} className="text-[#C86E5D]" />
                <h2 className="text-right font-sans font-semibold text-[18px] text-[#2F2F2F]">
                  تنبيهات اليوم:
                </h2>
              </div>

              <div className="mt-4 space-y-3">
                {warnings.length === 0 ? (
                  <div className="text-right text-[14px] text-[#6B7280]">لا توجد تنبيهات حالياً.</div>
                ) : (
                  warnings.map((w) => (
                    <WarningRow key={w.id} text={w.text} severity={w.severity} />
                  ))
                )}
              </div>

              <div className="mt-4 text-right text-[12px] text-[#6B7280]">
                {/* TODO backend: compute warnings from overdue tasks, missing check-ins, and stale issues */}
              </div>
            </CardShell>
          </div>

          {/* Recent Activity */}
          <div className="mt-6">
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
                      {a.icon === "pin" && <PinIcon className="text-[#7895B2]" />}
                      {a.icon === "user" && <User size={18} className="text-[#7895B2]" />}
                      {a.icon === "map" && <MapPin size={18} className="text-[#7895B2]" />}
                    </div>

                    <div className="flex-1">
                      <div className="text-right text-[12px] text-[#9CA3AF]">{a.time}</div>
                      <div className="text-right text-[14px] sm:text-[15px] text-[#2F2F2F]">
                        {a.text}
                      </div>
                    </div>
                  </div>
                ))}
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
    <div className="bg-white/60 backdrop-blur-sm rounded-[24px] shadow-[0_10px_25px_rgba(0,0,0,0.03)] border border-white/50 p-6 sm:p-8 transition-all duration-300">
      {children}
    </div>
  );
}

function SummaryBox({
  label,
  value,
  bg,
  text,
}: {
  label: string;
  value: number;
  bg: string;
  text: string;
}) {
  return (
    <div className="rounded-[16px] bg-white/70 border border-black/5 p-4">
      <div className="flex flex-col items-center justify-center">
        <div
          className="min-w-[54px] px-3 py-1 rounded-full text-center text-[13px] font-semibold"
          style={{ backgroundColor: bg, color: text }}
        >
          {value}
        </div>
        <div className="mt-2 text-[12px] text-[#2F2F2F] font-sans font-semibold">{label}</div>
      </div>
    </div>
  );
}

function WarningRow({ text, severity }: { text: string; severity: "high" | "medium" | "low" }) {
  const styles =
    severity === "high"
      ? "bg-red-50 border-red-200 text-red-700"
      : severity === "medium"
      ? "bg-amber-50 border-amber-200 text-amber-800"
      : "bg-slate-50 border-slate-200 text-slate-700";

  return (
    <div className={["w-full rounded-[14px] border p-3", styles].join(" ")}>
      <div className="text-right font-sans font-semibold text-[14px]">{text}</div>
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
        <div className="flex-1">
          <div className="text-right text-[14px] sm:text-[15px] text-[#2F2F2F]">
            {title} : <span className="font-semibold text-[#2F2F2F]">{total}</span>
          </div>

          <div className="mt-3 flex items-center justify-end gap-3 flex-wrap">
            {chips.map((c) => (
              <Chip key={c.label} value={c.value} label={c.label} bg={c.bg} text={c.text} />
            ))}
          </div>
        </div>

        <div className="shrink-0">
          <Donut size={54} stroke={10} parts={donut.parts} background="#E5E7EB" />
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
    <svg width="18" height="18" viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
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
