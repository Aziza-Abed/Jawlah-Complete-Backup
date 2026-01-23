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
  Download,
  CalendarDays
} from "lucide-react";
import GlassCard from "../components/UI/GlassCard";

export default function AdminAuditLogs() {
  const [logs, setLogs] = useState<AuditLog[]>([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [searchTerm, setSearchTerm] = useState("");
  const [actionFilter, setActionFilter] = useState("all");
  const [startDate, setStartDate] = useState("");
  const [endDate, setEndDate] = useState("");
  const [exporting, setExporting] = useState(false);

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

  // Export logs to CSV
  const exportToCSV = async () => {
    try {
      setExporting(true);
      // Fetch all logs for export (up to 10000)
      const data = await getAuditLogs(1, 10000, {
        action: actionFilter === "all" ? undefined : actionFilter,
        username: searchTerm || undefined,
        startDate: startDate || undefined,
        endDate: endDate || undefined
      });

      const logsToExport = data.items || [];
      if (logsToExport.length === 0) {
        alert("لا توجد سجلات للتصدير");
        return;
      }

      // Create CSV content
      const headers = ["المستخدم", "اسم المستخدم", "العملية", "الكيان", "التفاصيل", "عنوان IP", "المتصفح", "التاريخ والوقت"];
      const rows = logsToExport.map(log => [
        log.userFullName || "",
        log.username,
        log.action,
        log.entityName,
        log.details?.replace(/,/g, ";") || "",
        log.ipAddress || "",
        log.userAgent?.split(" ")[0] || "",
        new Date(log.createdAt).toLocaleString("ar-EG")
      ]);

      // Add BOM for Excel compatibility with Arabic
      const BOM = "\uFEFF";
      const csvContent = BOM + [
        headers.join(","),
        ...rows.map(row => row.map(cell => `"${cell}"`).join(","))
      ].join("\n");

      // Download file
      const blob = new Blob([csvContent], { type: "text/csv;charset=utf-8;" });
      const url = URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      const dateStr = new Date().toISOString().split("T")[0];
      link.download = `سجلات_النشاط_${dateStr}.csv`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(url);
    } catch (err) {
      console.error("Failed to export logs", err);
      alert("فشل تصدير السجلات");
    } finally {
      setExporting(false);
    }
  };

  // Clear all filters
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

  if (loading && page === 1) {
    return (
      <div className="h-full w-full flex items-center justify-center">
         <div className="flex flex-col items-center gap-4">
             <div className="w-12 h-12 border-4 border-primary/30 border-t-primary rounded-full animate-spin"></div>
             <p className="text-text-secondary font-medium">جاري تحميل السجلات...</p>
         </div>
      </div>
    );
  }

  return (
    <div className="space-y-8 pb-10">
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-6 animate-fade-in">
          <div className="flex flex-col items-end">
            <h1 className="text-3xl font-extrabold text-text-primary">
                سجلات النشاط
            </h1>
            <p className="text-right text-text-secondary mt-2 font-medium">تتبع جميع التغييرات والعمليات التي تمت على النظام</p>
          </div>

          <div className="flex items-center gap-4">
            {/* Export Button */}
            <button
              onClick={exportToCSV}
              disabled={exporting || logs.length === 0}
              className="flex items-center gap-2 bg-secondary hover:bg-secondary-dark disabled:opacity-50 disabled:cursor-not-allowed text-white px-4 py-3 rounded-xl transition-all shadow-lg shadow-secondary/20 hover:shadow-secondary/40 active:scale-95 font-bold text-sm"
            >
              {exporting ? (
                <div className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
              ) : (
                <Download size={18} />
              )}
              <span>تصدير CSV</span>
            </button>

            <GlassCard className="!p-0 !bg-primary/5 !border-primary/10 flex items-center overflow-hidden backdrop-blur-md">
              <div className="px-6 py-3 border-l border-primary/10">
                  <p className="text-xs text-text-muted font-bold mb-0.5">إجمالي السجلات</p>
                  <p className="text-lg font-bold text-text-primary text-center">{totalCount}</p>
              </div>
              <div className="px-4 py-3 bg-primary/10 text-primary">
                  <History size={24} />
              </div>
            </GlassCard>
          </div>
        </div>

        <GlassCard>
            <form onSubmit={handleSearch} className="space-y-4">
                {/* Row 1: Search + Action Filter */}
                <div className="flex flex-col lg:flex-row-reverse gap-4">
                    <div className="flex-1 relative group">
                        <input
                            type="text"
                            placeholder="بحث باسم المستخدم..."
                            value={searchTerm}
                            onChange={(e) => setSearchTerm(e.target.value)}
                            className="glass-input w-full h-12 pr-11 text-right focus:bg-primary/5 text-text-primary !bg-primary/5 border-primary/10"
                        />
                        <Search className="absolute right-4 top-1/2 -translate-y-1/2 text-text-muted group-focus-within:text-primary transition-colors" size={20} />
                    </div>

                    <div className="relative">
                        <select
                            value={actionFilter}
                            onChange={(e) => {
                                setActionFilter(e.target.value);
                                setPage(1);
                            }}
                            className="glass-input h-12 w-full sm:w-[200px] text-right appearance-none cursor-pointer focus:bg-primary/5 text-text-primary [&>option]:text-black pr-10 !bg-primary/5 border-primary/10"
                        >
                            {activities.map(a => (
                                <option key={a.id} value={a.id}>{a.name}</option>
                            ))}
                        </select>
                        <Filter size={16} className="absolute top-1/2 -translate-y-1/2 right-3 text-text-muted pointer-events-none" />
                    </div>
                </div>

                {/* Row 2: Date Range + Buttons */}
                <div className="flex flex-col sm:flex-row-reverse gap-4 items-end">
                    {/* Date Range Section */}
                    <div className="flex flex-row-reverse gap-3 items-center flex-1">
                        <div className="flex items-center gap-2 text-text-muted">
                            <CalendarDays size={18} />
                            <span className="text-sm font-medium">الفترة:</span>
                        </div>
                        <div className="flex flex-row-reverse gap-2 flex-1">
                            <div className="relative flex-1">
                                <input
                                    type="date"
                                    value={startDate}
                                    onChange={(e) => setStartDate(e.target.value)}
                                    className="glass-input h-12 w-full text-right focus:bg-primary/5 text-text-primary cursor-pointer [color-scheme:light] !bg-primary/5 border-primary/10"
                                    placeholder="من تاريخ"
                                />
                                <span className="absolute left-3 top-1/2 -translate-y-1/2 text-xs text-text-muted pointer-events-none">من</span>
                            </div>
                            <div className="relative flex-1">
                                <input
                                    type="date"
                                    value={endDate}
                                    onChange={(e) => setEndDate(e.target.value)}
                                    className="glass-input h-12 w-full text-right focus:bg-primary/5 text-text-primary cursor-pointer [color-scheme:light] !bg-primary/5 border-primary/10"
                                    placeholder="إلى تاريخ"
                                />
                                <span className="absolute left-3 top-1/2 -translate-y-1/2 text-xs text-text-muted pointer-events-none">إلى</span>
                            </div>
                        </div>
                    </div>

                    {/* Buttons */}
                    <div className="flex gap-2">
                        <button
                            type="submit"
                            className="bg-primary hover:bg-primary-dark text-white px-6 rounded-xl transition-all shadow-lg shadow-primary/20 hover:shadow-primary/40 active:scale-95 font-bold text-sm h-12"
                        >
                            تصفية
                        </button>
                        {(searchTerm || actionFilter !== "all" || startDate || endDate) && (
                            <button
                                type="button"
                                onClick={clearFilters}
                                className="bg-primary/10 hover:bg-primary/20 text-text-primary px-4 rounded-xl transition-all active:scale-95 font-medium text-sm h-12 border border-primary/10"
                            >
                                مسح الفلاتر
                            </button>
                        )}
                    </div>
                </div>
            </form>
        </GlassCard>

        <GlassCard noPadding className="overflow-hidden !bg-background-paper shadow-xl border-primary/5">
          <div className="overflow-x-auto">
            <table className="w-full text-right" dir="rtl">
              <thead className="bg-primary/5 border-b border-primary/10 text-text-muted text-xs uppercase tracking-wider">
                <tr>
                  <th className="px-6 py-5 font-bold">المستخدم</th>
                  <th className="px-6 py-5 font-bold">العملية</th>
                  <th className="px-6 py-5 font-bold">التفاصيل</th>
                  <th className="px-6 py-5 font-bold">الوقت والتاريخ</th>
                  <th className="px-6 py-5 font-bold text-center">معلومات تقنية</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-primary/5">
                {logs.map((log) => (
                  <tr key={log.auditLogId} className="hover:bg-primary/5 transition-colors group">
                    <td className="px-6 py-4">
                      <div className="flex items-center gap-3 flex-row">
                        <div className="w-10 h-10 rounded-full bg-primary/10 border border-primary/10 flex items-center justify-center text-primary">
                          <User size={20} />
                        </div>
                        <div className="flex flex-col text-right">
                          <span className="font-bold text-text-primary text-sm">{log.userFullName || log.username}</span>
                          <span className="text-[10px] text-text-muted font-mono italic">@{log.username}</span>
                        </div>
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      <div className="flex items-center gap-2 justify-start">
                        <span className={`px-2.5 py-1 rounded-lg text-[10px] font-extrabold border ${
                            log.action.includes("Create") ? "bg-secondary/20 text-secondary border-secondary/30" :
                            log.action.includes("Update") ? "bg-primary/20 text-primary border-primary/30" :
                            log.action.includes("Delete") ? "bg-accent/20 text-accent border-accent/30" :
                            "bg-text-muted/10 text-text-muted border-text-muted/30"
                        }`}>
                            {log.action}
                        </span>
                        <Activity size={12} className="text-text-muted opacity-50" />
                      </div>
                    </td>
                    <td className="px-6 py-4 max-w-[300px]">
                      <div className="flex items-start gap-2 justify-start">
                        <p className="text-xs text-text-secondary leading-relaxed text-right">{log.details}</p>
                        <Info size={12} className="text-text-muted mt-1 shrink-0 opacity-50" />
                      </div>
                    </td>
                    <td className="px-6 py-4">
                        <div className="flex flex-col items-start gap-1">
                            <span className="text-xs font-bold text-text-primary">{new Date(log.createdAt).toLocaleDateString('ar-EG')}</span>
                            <span className="text-[10px] text-text-muted font-medium flex items-center gap-1">
                                {new Date(log.createdAt).toLocaleTimeString('ar-EG', { hour: '2-digit', minute: '2-digit' })}
                                <Calendar size={10} />
                            </span>
                        </div>
                    </td>
                    <td className="px-6 py-4">
                        <div className="flex items-center justify-center gap-3">
                            <div className="flex items-center gap-1 text-[10px] text-text-muted bg-primary/5 border border-primary/5 px-2 py-1 rounded-lg" title={log.ipAddress}>
                                <Globe size={10} className="text-primary" />
                                <span className="font-mono">{log.ipAddress?.split(',')[0]}</span>
                            </div>
                            <div className="flex items-center gap-1 text-[10px] text-text-muted bg-primary/5 border border-primary/5 px-2 py-1 rounded-lg" title={log.userAgent}>
                                <Monitor size={10} className="text-secondary" />
                                <span className="max-w-[50px] truncate font-mono">{log.userAgent?.split(' ')[0]}</span>
                            </div>
                        </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {logs.length === 0 && (
            <div className="p-16 text-center text-text-muted font-sans flex flex-col items-center">
              <History size={48} className="mx-auto mb-4 opacity-20" />
              لا توجد سجلات مطابقة للبحث
            </div>
          )}

          {/* Pagination */}
          {totalCount > 20 && (
              <div className="bg-primary/5 px-6 py-4 border-t border-primary/10 flex items-center justify-between">
                  <div className="flex items-center gap-2">
                       <button 
                         onClick={() => setPage(p => Math.max(1, p - 1))}
                         disabled={page === 1}
                         className="p-2 rounded-lg border border-primary/10 text-text-primary hover:bg-primary/10 disabled:opacity-30 disabled:cursor-not-allowed transition-all"
                       >
                           <ChevronRight size={18} />
                       </button>
                       <span className="text-xs font-bold text-text-secondary mx-2">صفحة {page} من {Math.ceil(totalCount / 20)}</span>
                       <button 
                         onClick={() => setPage(p => p + 1)}
                         disabled={page >= Math.ceil(totalCount / 20)}
                         className="p-2 rounded-lg border border-primary/10 text-text-primary hover:bg-primary/10 disabled:opacity-30 disabled:cursor-not-allowed transition-all"
                       >
                           <ChevronLeft size={18} />
                       </button>
                  </div>
                  <span className="text-xs text-text-muted font-medium">عرض {logs.length} من {totalCount} سجل</span>
              </div>
          )}
        </GlassCard>
    </div>
  );
}
