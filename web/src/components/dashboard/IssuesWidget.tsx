import { MessageSquare, CheckCircle2, Circle } from "lucide-react";

interface IssuesWidgetProps {
  reportedToday: number;
  unresolved: number;
}

export default function IssuesWidget({ reportedToday, unresolved }: IssuesWidgetProps) {
  const stats = [
    {
      label: "جديدة",
      value: reportedToday,
      icon: <Circle size={14} className="text-[#F3C668]" />,
      color: "bg-[#F3E7C8]/20",
    },
    {
      label: "مفتوحة",
      value: unresolved,
      icon: <MessageSquare size={14} className="text-[#7895B2]" />,
      color: "bg-[#7895B2]/10",
    },
    {
      label: "مغلقة اليوم",
      value: Math.max(0, reportedToday - 2), // Mocking some progress for demo logic if no backend field
      icon: <CheckCircle2 size={14} className="text-[#8FA36A]" />,
      color: "bg-[#8FA36A]/10",
    },
  ];

  return (
    <div className="bg-white rounded-[16px] p-6 shadow-[0_4px_20px_rgba(0,0,0,0.04)] h-full">
      <div className="flex items-center justify-between mb-8">
        <h2 className="text-[17px] font-bold text-[#2F2F2F]">ملخص البلاغات</h2>
        <MessageSquare size={18} className="text-[#6B7280]/40" />
      </div>

      <div className="grid grid-cols-3 gap-4">
        {stats.map((stat, idx) => (
          <div key={idx} className="flex flex-col items-center">
            <div className={`w-full py-4 rounded-[12px] ${stat.color} flex flex-col items-center justify-center gap-2 border border-black/5`}>
              <span className="text-2xl font-black text-[#2F2F2F]">{stat.value}</span>
              <div className="flex items-center gap-1.5 text-[11px] font-bold text-[#6B7280]">
                {stat.icon}
                {stat.label}
              </div>
            </div>
          </div>
        ))}
      </div>

      <div className="mt-8 p-4 rounded-[12px] bg-[#f9f8f5] border border-black/5 text-right">
        <p className="text-[12px] text-[#6B7280] leading-relaxed">
            البلاغات يتم استقبالها من المواطنين والفرق الميدانية، وتتم مراجعتها من قبلكم للمتابعة.
        </p>
      </div>
    </div>
  );
}
