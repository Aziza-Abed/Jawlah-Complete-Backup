import type { ZoneRow } from "./types";

type ReportZonesTableProps = {
  columns: string[];
  rows: ZoneRow[];
};

export default function ReportZonesTable({ columns, rows }: ReportZonesTableProps) {
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
            <td className="px-8 py-5 text-[14px] font-black text-[#2F2F2F] group-hover:text-[#7895B2]">{r.zone}</td>
            <td className="px-8 py-5 text-[13px] font-black text-[#6B7280]">{r.total}</td>
            <td className="px-8 py-5 text-[13px] font-black text-[#8FA36A]">{r.done}</td>
            <td className="px-8 py-5 text-[13px] font-black text-[#7895B2]">{r.inProgress}</td>
            <td className="px-8 py-5 text-[13px] font-black text-[#C86E5D]">{r.delayed}</td>
            <td className="px-8 py-5 text-[13px] font-black text-[#2F2F2F]">{r.rate}</td>
            <td className="px-8 py-5 text-[12px] font-bold text-[#AFAFAF]">{r.updatedAt}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
