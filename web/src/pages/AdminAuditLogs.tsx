import { useEffect, useState } from "react";
import { getAuditLogs } from "../api/audit";
import type { AuditLog } from "../api/audit";
import {
  History,
  Search,
  User,
  Activity,
  Calendar,
  Info,
  Globe,
  Monitor,
  ChevronLeft,
  ChevronRight,
  Filter,
  CalendarDays
} from "lucide-react";

export default function AdminAuditLogs() {
  const [logs, setLogs] = useState<AuditLog[]>([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [searchTerm, setSearchTerm] = useState("");
  const [actionFilter, setActionFilter] = useState("all");
  const [startDate, setStartDate] = useState("");
  const [endDate, setEndDate] = useState("");

  useEffect(() => {
    fetchLogs();
  }, [page, actionFilter]);

  const fetchLogs = async () => {
    try {
      setLoading(true);
      const data = await getAuditLogs(page, 20, {
          action: actionFilter === "all" ? undefined : actionFilter,
          username: searchTerm || undefined,
          startDate: startDate || undefined,
          endDate: endDate || undefined
      });
      setLogs(data.items || []);
      setTotalCount(data.totalCount || 0);
    } catch (err) {
      console.error("Failed to fetch audit logs", err);
    } finally {
      setLoading(false);
    }
  };

  const handleSearch = (e: React.FormEvent) => {
      e.preventDefault();
      setPage(1);
      fetchLogs();
  };

  const clearFilters = () => {
    setSearchTerm("");
    setActionFilter("all");
    setStartDate("");
    setEndDate("");
    setPage(1);
  };

  const activities = [
      { id: "all", name: "كل العمليات" },
      { id: "Login", name: "تسجيل دخول" },
      { id: "Create", name: "إضافة بيانات" },
      { id: "Update", name: "تحديث بيانات" },
      { id: "Delete", name: "حذف بيانات" },
      { id: "Task", name: "عمليات المهام" }
  ];

  const getActionColor = (action: string) => {
    if (action.includes("Create")) return "bg-[#8FA36A]/20 text-[#8FA36A] border-[#8FA36A]/30";
    if (action.includes("Update")) return "bg-[#7895B2]/20 text-[#7895B2] border-[#7895B2]/30";
    if (action.includes("Delete")) return "bg-[#C86E5D]/20 text-[#C86E5D] border-[#C86E5D]/30";
    return "bg-[#6B7280]/10 text-[#6B7280] border-[#6B7280]/30";
  };

  if (loading && page === 1) {
    return (
      <div className="h-full w-full bg-[#F3F1ED] flex items-center justify-center">
        <div className="flex flex-col items-center gap-4">
          <div className="w-12 h-12 border-4 border-[#7895B2]/30 border-t-[#7895B2] rounded-full animate-spin"></div>
          <p className="text-[#6B7280] font-medium">جاري تحميل السجلات...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="h-full w-full bg-[#F3F1ED] overflow-auto">
      <div className="p-4 sm:p-6 md:p-8">
        <div className="max-w-[1200px] mx-auto space-y-6">
          {/* Header */}
          <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
            <div className="flex items-center gap-3">
              <div className="p-2.5 rounded-[12px] bg-[#7895B2]/20">
                <History size={22} className="text-[#7895B2]" />
              </div>
              <div>
                <h1 className="font-sans font-bold text-[22px] sm:text-[24px] text-[#2F2F2F]">
                  سجلات النشاط
                </h1>
                <p className="text-[13px] text-[#6B7280]">تتبع جميع التغييرات والعمليات على النظام</p>
              </div>
            </div>

            <div className="flex items-center gap-4">
              <div className="bg-white rounded-[16px] px-4 py-3 shadow-[0_4px_20px_rgba(0,0,0,0.04)] flex items-center gap-3">
                <div className="p-2 bg-[#7895B2]/10 text-[#7895B2] rounded-[10px]">
                  <History size={20} />
                </div>
                <div className="text-right">
                  <p className="text-[11px] text-[#6B7280] font-semibold">إجمالي السجلات</p>
                  <p className="text-[18px] font-bold text-[#2F2F2F]">{totalCount}</p>
                </div>
              </div>
            </div>
          </div>

          {/* Filters */}
          <div className="bg-white rounded-[16px] p-5 shadow-[0_4px_20px_rgba(0,0,0,0.04)]">
            <form onSubmit={handleSearch} className="space-y-4">
              <div className="flex flex-col lg:flex-row-reverse gap-4">
                <div className="flex-1 relative">
                  <input
                    type="text"
                    placeholder="بحث باسم المستخدم..."
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                    className="w-full h-[46px] pr-11 pl-4 bg-[#F3F1ED] rounded-[12px] text-right text-[14px] text-[#2F2F2F] placeholder:text-[#6B7280] border-0 outline-none focus:ring-2 focus:ring-[#7895B2]/30"
                  />
                  <Search className="absolute right-4 top-1/2 -translate-y-1/2 text-[#6B7280]" size={18} />
                </div>

                <div className="relative">
                  <select
                    value={actionFilter}
                    onChange={(e) => {
                      setActionFilter(e.target.value);
                      setPage(1);
                    }}
                    className="h-[46px] w-full sm:w-[200px] px-10 pr-4 bg-[#F3F1ED] rounded-[12px] text-right text-[14px] text-[#2F2F2F] border-0 outline-none focus:ring-2 focus:ring-[#7895B2]/30 cursor-pointer"
                  >
                    {activities.map(a => (
                      <option key={a.id} value={a.id}>{a.name}</option>
                    ))}
                  </select>
                  <Filter size={16} className="absolute top-1/2 -translate-y-1/2 left-4 text-[#6B7280] pointer-events-none" />
                </div>
              </div>

              <div className="flex flex-col sm:flex-row-reverse gap-4 items-end">
                <div className="flex flex-row-reverse gap-3 items-center flex-1">
                  <div className="flex items-center gap-2 text-[#6B7280]">
                    <CalendarDays size={18} />
                    <span className="text-[13px] font-medium">الفترة:</span>
                  </div>
                  <div className="flex flex-row-reverse gap-2 flex-1">
                    <div className="relative flex-1">
                      <input
                        type="date"
                        value={startDate}
                        onChange={(e) => setStartDate(e.target.value)}
                        className="h-[46px] w-full px-4 bg-[#F3F1ED] rounded-[12px] text-right text-[14px] text-[#2F2F2F] border-0 outline-none focus:ring-2 focus:ring-[#7895B2]/30 cursor-pointer [color-scheme:light]"
                      />
                      <span className="absolute left-3 top-1/2 -translate-y-1/2 text-[11px] text-[#6B7280] pointer-events-none">من</span>
                    </div>
                    <div className="relative flex-1">
                      <input
                        type="date"
                        value={endDate}
                        onChange={(e) => setEndDate(e.target.value)}
                        className="h-[46px] w-full px-4 bg-[#F3F1ED] rounded-[12px] text-right text-[14px] text-[#2F2F2F] border-0 outline-none focus:ring-2 focus:ring-[#7895B2]/30 cursor-pointer [color-scheme:light]"
                      />
                      <span className="absolute left-3 top-1/2 -translate-y-1/2 text-[11px] text-[#6B7280] pointer-events-none">إلى</span>
                    </div>
                  </div>
                </div>

                <div className="flex gap-2">
                  <button
                    type="submit"
                    className="bg-[#7895B2] hover:bg-[#6B87A3] text-white px-6 h-[46px] rounded-[12px] transition-all font-semibold text-[14px]"
                  >
                    تصفية
                  </button>
                  {(searchTerm || actionFilter !== "all" || startDate || endDate) && (
                    <button
                      type="button"
                      onClick={clearFilters}
                      className="bg-[#7895B2]/10 hover:bg-[#7895B2]/20 text-[#2F2F2F] px-4 h-[46px] rounded-[12px] transition-all font-semibold text-[14px] border border-[#7895B2]/20"
                    >
                      مسح الفلاتر
                    </button>
                  )}
                </div>
              </div>
            </form>
          </div>

          {/* Logs Table */}
          <div className="bg-white rounded-[16px] shadow-[0_4px_20px_rgba(0,0,0,0.04)] overflow-hidden">
            <div className="overflow-x-auto">
              <table className="w-full text-right" dir="rtl">
                <thead className="bg-[#F3F1ED] border-b border-[#E5E7EB] text-[#6B7280] text-[11px] uppercase tracking-wider">
                  <tr>
                    <th className="px-5 py-4 font-bold">المستخدم</th>
                    <th className="px-5 py-4 font-bold">العملية</th>
                    <th className="px-5 py-4 font-bold">التفاصيل</th>
                    <th className="px-5 py-4 font-bold">الوقت والتاريخ</th>
                    <th className="px-5 py-4 font-bold text-center">معلومات تقنية</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-[#F3F1ED]">
                  {logs.map((log) => (
                    <tr key={log.auditLogId} className="hover:bg-[#F3F1ED]/50 transition-colors">
                      <td className="px-5 py-4">
                        <div className="flex items-center gap-3 flex-row">
                          <div className="w-9 h-9 rounded-full bg-[#7895B2]/10 border border-[#7895B2]/10 flex items-center justify-center text-[#7895B2]">
                            <User size={18} />
                          </div>
                          <div className="text-right">
                            <span className="font-semibold text-[#2F2F2F] text-[13px] block">{log.userFullName || log.username}</span>
                            <span className="text-[10px] text-[#6B7280] font-sans">@{log.username}</span>
                          </div>
                        </div>
                      </td>
                      <td className="px-5 py-4">
                        <div className="flex items-center gap-2 justify-start">
                          <span className={`px-2 py-1 rounded-[8px] text-[10px] font-bold border ${getActionColor(log.action)}`}>
                            {log.action}
                          </span>
                          <Activity size={12} className="text-[#6B7280] opacity-50" />
                        </div>
                      </td>
                      <td className="px-5 py-4 max-w-[280px]">
                        <div className="flex items-start gap-2 justify-start">
                          <p className="text-[12px] text-[#6B7280] leading-relaxed text-right truncate">{log.details}</p>
                          <Info size={12} className="text-[#6B7280] mt-0.5 shrink-0 opacity-50" />
                        </div>
                      </td>
                      <td className="px-5 py-4">
                        <div className="flex flex-col items-start gap-1">
                          <span className="text-[12px] font-semibold text-[#2F2F2F]">{new Date(log.createdAt).toLocaleDateString('ar-EG')}</span>
                          <span className="text-[10px] text-[#6B7280] font-medium flex items-center gap-1">
                            {new Date(log.createdAt).toLocaleTimeString('ar-EG', { hour: '2-digit', minute: '2-digit' })}
                            <Calendar size={10} />
                          </span>
                        </div>
                      </td>
                      <td className="px-5 py-4">
                        <div className="flex items-center justify-center gap-2">
                          <div className="flex items-center gap-1 text-[10px] text-[#6B7280] bg-[#F3F1ED] px-2 py-1 rounded-[8px]" title={log.ipAddress}>
                            <Globe size={10} className="text-[#7895B2]" />
                            <span className="font-sans">{log.ipAddress?.split(',')[0]}</span>
                          </div>
                          <div className="flex items-center gap-1 text-[10px] text-[#6B7280] bg-[#F3F1ED] px-2 py-1 rounded-[8px]" title={log.userAgent}>
                            <Monitor size={10} className="text-[#8FA36A]" />
                            <span className="max-w-[50px] truncate font-sans">{log.userAgent?.split(' ')[0]}</span>
                          </div>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {logs.length === 0 && (
              <div className="p-12 text-center">
                <History size={48} className="mx-auto mb-4 text-[#7895B2]/20" />
                <p className="text-[#6B7280] font-medium">لا توجد سجلات مطابقة للبحث</p>
              </div>
            )}

            {/* Pagination */}
            {totalCount > 20 && (
              <div className="bg-[#F3F1ED] px-5 py-4 border-t border-[#E5E7EB] flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <button
                    onClick={() => setPage(p => Math.max(1, p - 1))}
                    disabled={page === 1}
                    className="p-2 rounded-[10px] border border-[#E5E7EB] bg-white text-[#2F2F2F] hover:bg-[#F3F1ED] disabled:opacity-30 disabled:cursor-not-allowed transition-all"
                  >
                    <ChevronRight size={18} />
                  </button>
                  <span className="text-[12px] font-semibold text-[#6B7280] mx-2">صفحة {page} من {Math.ceil(totalCount / 20)}</span>
                  <button
                    onClick={() => setPage(p => p + 1)}
                    disabled={page >= Math.ceil(totalCount / 20)}
                    className="p-2 rounded-[10px] border border-[#E5E7EB] bg-white text-[#2F2F2F] hover:bg-[#F3F1ED] disabled:opacity-30 disabled:cursor-not-allowed transition-all"
                  >
                    <ChevronLeft size={18} />
                  </button>
                </div>
                <span className="text-[12px] text-[#6B7280] font-medium">عرض {logs.length} من {totalCount} سجل</span>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
