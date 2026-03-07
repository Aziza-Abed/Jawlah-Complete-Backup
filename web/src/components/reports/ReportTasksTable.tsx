import type { TaskRow } from "./types";

type ReportTasksTableProps = {
  columns: string[];
  rows: TaskRow[];
};

export default function ReportTasksTable({ columns, rows }: ReportTasksTableProps) {
  return (
    <table className="w-full text-right">
      <thead>
        <tr className="border-b border-black/5 bg-[#F9F8F6]/50">
          {columns.map((c) => (
            <th key={c} className="px-8 py-5 text-[11px] font-black text-[#AFAFAF] uppercase tracking-widest">
              {c}
            </th>
          ))}
        </tr>
      </thead>
      <tbody className="divide-y divide-black/5">
        {rows.map((r) => (
          <tr key={r.id} className="hover:bg-[#F9F8F6] transition-all group">
            <td className="px-8 py-5 text-[12px] font-black text-[#AFAFAF]">{r.id}</td>
            <td className="px-8 py-5 text-[14px] font-black text-[#2F2F2F] group-hover:text-[#7895B2]">{r.title}</td>
            <td className="px-8 py-5 text-[13px] font-bold text-[#6B7280]">{r.worker}</td>
            <td className="px-8 py-5 text-[13px] font-bold text-[#6B7280]">{r.zone}</td>
            <td className="px-8 py-5">
                <span className={`px-3 py-1 rounded-full text-[10px] font-black border tracking-wider uppercase ${
                    r.status === 'مكتملة' ? 'bg-[#8FA36A]/10 text-[#8FA36A] border-[#8FA36A]/20' :
                    r.status === 'قيد التنفيذ' ? 'bg-[#7895B2]/10 text-[#7895B2] border-[#7895B2]/20' :
                    r.status === 'قيد المراجعة' ? 'bg-[#A78BFA]/10 text-[#A78BFA] border-[#A78BFA]/20' :
                    r.status === 'معلقة' ? 'bg-[#C86E5D]/10 text-[#C86E5D] border-[#C86E5D]/20' :
                    r.status === 'مرفوضة' ? 'bg-red-500/10 text-red-600 border-red-500/20' : 'bg-[#7895B2]/5 text-[#AFAFAF] border-black/5'
                }`}>
                  {r.status}
                </span>
            </td>
            <td className="px-8 py-5 text-[13px] font-bold text-[#6B7280]">{r.priority}</td>
            <td className="px-8 py-5 text-[13px] font-black text-[#2F2F2F]">{r.dueDate}</td>
            <td className="px-8 py-5 text-[12px] font-bold text-[#AFAFAF]">{r.time}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
