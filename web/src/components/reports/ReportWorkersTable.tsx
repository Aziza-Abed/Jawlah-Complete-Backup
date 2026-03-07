import type { WorkerRow } from "./types";

type ReportWorkersTableProps = {
  columns: string[];
  rows: WorkerRow[];
};

export default function ReportWorkersTable({ columns, rows }: ReportWorkersTableProps) {
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
            <td className="px-8 py-5 text-[14px] font-black text-[#2F2F2F] group-hover:text-[#7895B2]">{r.name}</td>
            <td className="px-8 py-5">
                <span className={`px-3 py-1 rounded-full text-[10px] font-black border tracking-wider uppercase ${r.presence === 'حضور' ? 'bg-[#8FA36A]/10 text-[#8FA36A] border-[#8FA36A]/20' : 'bg-[#C86E5D]/10 text-[#C86E5D] border-[#C86E5D]/20'}`}>
                    {r.presence}
                </span>
            </td>
            <td className="px-8 py-5 text-[13px] font-black text-[#2F2F2F]">{r.lastSeen}</td>
            <td className="px-8 py-5 text-[14px] font-black text-[#7895B2]">{r.activeTasks}</td>
            <td className="px-8 py-5 text-[14px] font-black text-[#8FA36A]">{r.doneTasks}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
