import { useEffect, useMemo, useState } from "react";
import { Scale, Search, CheckCircle, XCircle, Eye, Filter, X, MapPin, Clock, User, FileText } from "lucide-react";
import { getPendingAppeals, approveAppeal, rejectAppeal } from "../api/appeals";
import type { AppealResponse } from "../types/appeal";

type FilterKey = "all" | "Pending" | "Approved" | "Rejected";

export default function AdminAppeals() {
  const [items, setItems] = useState<AppealResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [filter, setFilter] = useState<FilterKey>("Pending");
  const [q, setQ] = useState("");
  const [processing, setProcessing] = useState<number | null>(null);

  const [selectedAppeal, setSelectedAppeal] = useState<AppealResponse | null>(null);
  const [reviewNotes, setReviewNotes] = useState("");

  useEffect(() => {
    fetchAppeals();
  }, []);

  const fetchAppeals = async () => {
    try {
      setLoading(true);
      setError("");
      const appeals = await getPendingAppeals();
      setItems(appeals);
    } catch (err) {
      console.error("Failed to fetch appeals:", err);
      setError("فشل تحميل الطعون");
    } finally {
      setLoading(false);
    }
  };

  const handleApprove = async (appeal: AppealResponse) => {
    if (!confirm("هل أنت متأكد من الموافقة على هذا الطعن؟ سيتم إعادة المهمة لحالة مكتملة.")) return;

    try {
      setProcessing(appeal.appealId);
      await approveAppeal(appeal.appealId, reviewNotes || undefined);
      setItems(prev => prev.filter(item => item.appealId !== appeal.appealId));
      setSelectedAppeal(null);
      setReviewNotes("");
    } catch (err) {
      console.error("Failed to approve appeal:", err);
      alert("فشل في الموافقة على الطعن");
    } finally {
      setProcessing(null);
    }
  };

  const handleReject = async (appeal: AppealResponse) => {
    if (!reviewNotes.trim()) {
      alert("يرجى كتابة سبب الرفض");
      return;
    }
    if (!confirm("هل أنت متأكد من رفض هذا الطعن؟")) return;

    try {
      setProcessing(appeal.appealId);
      await rejectAppeal(appeal.appealId, reviewNotes);
      setItems(prev => prev.filter(item => item.appealId !== appeal.appealId));
      setSelectedAppeal(null);
      setReviewNotes("");
    } catch (err) {
      console.error("Failed to reject appeal:", err);
      alert("فشل في رفض الطعن");
    } finally {
      setProcessing(null);
    }
  };

  const counts = useMemo(() => {
    const base = { all: items.length, Pending: 0, Approved: 0, Rejected: 0 };
    for (const item of items) base[item.status]++;
    return base;
  }, [items]);

  const filtered = useMemo(() => {
    const query = q.trim().toLowerCase();
    const byStatus = filter === "all" ? items : items.filter((x) => x.status === filter);

    if (!query) return byStatus;

    return byStatus.filter((x) => {
      return (
        (x.workerName?.toLowerCase() || "").includes(query) ||
        (x.workerExplanation?.toLowerCase() || "").includes(query) ||
        (x.entityTitle?.toLowerCase() || "").includes(query) ||
        String(x.appealId).includes(query)
      );
    });
  }, [items, filter, q]);

  const formatDate = (dateStr: string) => {
    const date = new Date(dateStr);
    return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")} ${String(date.getHours()).padStart(2, "0")}:${String(date.getMinutes()).padStart(2, "0")}`;
  };

  const statusColors = {
    Pending: "bg-[#F5B300]/10 text-[#D4A100] border-[#F5B300]/30",
    Approved: "bg-[#8FA36A]/10 text-[#8FA36A] border-[#8FA36A]/30",
    Rejected: "bg-[#C86E5D]/10 text-[#C86E5D] border-[#C86E5D]/30",
  };

  const statusLabels = {
    Pending: "قيد المراجعة",
    Approved: "تمت الموافقة",
    Rejected: "مرفوض",
  };

  if (loading) {
    return (
      <div className="h-full w-full bg-[#F3F1ED] flex items-center justify-center">
        <div className="flex flex-col items-center gap-4">
          <div className="w-12 h-12 border-4 border-[#7895B2]/30 border-t-[#7895B2] rounded-full animate-spin"></div>
          <p className="text-[#6B7280] font-medium">جاري التحميل...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="h-full w-full bg-[#F3F1ED] overflow-auto">
      <div className="p-4 sm:p-6 md:p-8">
        <div className="max-w-[1400px] mx-auto space-y-6">
          {/* Header */}
          <div className="flex items-center justify-between">
            <div className="text-[#6B7280] text-[13px]">
              العدد الإجمالي: <span className="text-[#F5B300] font-bold">{counts.Pending}</span>
            </div>
            <div className="flex items-center gap-3">
              <div className="p-2.5 rounded-[12px] bg-[#7895B2]/20">
                <Scale size={22} className="text-[#7895B2]" />
              </div>
              <div>
                <h1 className="font-sans font-bold text-[22px] sm:text-[24px] text-[#2F2F2F]">
                  مركز المراجعة
                </h1>
                <p className="text-[13px] text-[#6B7280]">مراجعة طلبات التظلم والاعتراضات على المهام</p>
              </div>
            </div>
          </div>

          {/* Search & Filters */}
          <div className="bg-white rounded-[16px] p-4 shadow-[0_4px_20px_rgba(0,0,0,0.04)]">
            <div className="flex flex-col lg:flex-row gap-4">
              <div className="flex-1 relative">
                <Search className="absolute right-4 top-1/2 -translate-y-1/2 text-[#6B7280]" size={18} />
                <input
                  value={q}
                  onChange={(e) => setQ(e.target.value)}
                  placeholder="بحث (اسم العامل / عنوان المهمة / رقم الطعن)..."
                  className="w-full h-[46px] pr-11 pl-4 bg-[#F3F1ED] rounded-[12px] text-right text-[14px] text-[#2F2F2F] placeholder:text-[#6B7280] border-0 outline-none focus:ring-2 focus:ring-[#7895B2]/30"
                />
                {q && (
                  <button
                    onClick={() => setQ("")}
                    className="absolute left-4 top-1/2 -translate-y-1/2 text-[#6B7280] hover:text-[#2F2F2F]"
                  >
                    <X size={16} />
                  </button>
                )}
              </div>

              <div className="flex items-center gap-2 flex-wrap">
                <Filter size={16} className="text-[#6B7280]" />
                {(["all", "Pending"] as FilterKey[]).map((key) => (
                  <button
                    key={key}
                    onClick={() => setFilter(key)}
                    className={`px-3 py-2 rounded-[10px] text-[13px] font-semibold transition-all border ${
                      filter === key
                        ? "bg-[#7895B2] text-white border-[#7895B2] shadow-md"
                        : "bg-[#F3F1ED] text-[#6B7280] border-[#E5E7EB] hover:bg-[#E8E6E2]"
                    }`}
                  >
                    {key === "all" ? "الكل" : statusLabels[key]}
                    <span className={`mr-2 px-1.5 py-0.5 rounded text-[11px] ${
                      filter === key ? "bg-white/20" : "bg-[#7895B2]/10"
                    }`}>
                      {counts[key]}
                    </span>
                  </button>
                ))}
              </div>
            </div>
          </div>

          {/* Error State */}
          {error && (
            <div className="bg-[#C86E5D]/10 border border-[#C86E5D]/30 rounded-[12px] p-4 text-center">
              <p className="text-[#C86E5D] font-medium">{error}</p>
              <button onClick={fetchAppeals} className="mt-2 px-4 py-2 bg-[#7895B2] text-white rounded-[10px] font-semibold text-[14px]">
                إعادة المحاولة
              </button>
            </div>
          )}

          {/* Appeals List */}
          {!error && (
            <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
              {/* List */}
              <div className="lg:col-span-2 space-y-3">
                {filtered.length === 0 ? (
                  <div className="bg-white rounded-[16px] p-8 shadow-[0_4px_20px_rgba(0,0,0,0.04)] text-center">
                    <Scale className="mx-auto text-[#7895B2]/20 mb-4" size={48} />
                    <p className="text-[#6B7280] font-medium">لا يوجد بيانات حالياً</p>
                  </div>
                ) : (
                  filtered.map((item) => (
                    <div
                      key={item.appealId}
                      className={`bg-white rounded-[16px] p-5 shadow-[0_4px_20px_rgba(0,0,0,0.04)] cursor-pointer transition-all hover:shadow-[0_8px_30px_rgba(0,0,0,0.08)] ${
                        selectedAppeal?.appealId === item.appealId ? "ring-2 ring-[#7895B2]" : ""
                      }`}
                      onClick={() => {
                        setSelectedAppeal(item);
                        setReviewNotes("");
                      }}
                    >
                      <div className="flex items-start justify-between gap-4">
                        <div className="flex-1 min-w-0 text-right">
                          <div className="flex items-start justify-between gap-3 mb-3">
                            <div>
                              <h3 className="text-[#2F2F2F] font-bold text-[15px] truncate">
                                مراجعة #{item.appealId} - {item.entityTitle || `مهمة #${item.entityId}`}
                              </h3>
                              <p className="text-[#6B7280] text-[12px] mt-1 flex items-center gap-1 justify-end">
                                <User size={12} />
                                {item.workerName}
                              </p>
                            </div>
                            <span className={`px-2.5 py-1 rounded-full text-[10px] font-bold border shrink-0 ${statusColors[item.status]}`}>
                              {statusLabels[item.status]}
                            </span>
                          </div>

                          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                            <div className="bg-[#F3F1ED] rounded-[10px] px-3 py-2 border border-[#E5E7EB]">
                              <p className="text-[#6B7280] text-[11px]">المسافة عن الموقع</p>
                              <p className="text-[#C86E5D] text-[13px] font-bold">
                                {item.distanceMeters ? `${item.distanceMeters} متر` : "غير محدد"}
                              </p>
                            </div>
                            <div className="bg-[#F3F1ED] rounded-[10px] px-3 py-2 border border-[#E5E7EB]">
                              <p className="text-[#6B7280] text-[11px]">تاريخ الطعن</p>
                              <p className="text-[#2F2F2F] text-[13px] font-semibold">{formatDate(item.submittedAt)}</p>
                            </div>
                          </div>

                          <p className="text-[#6B7280] text-[13px] mt-3 line-clamp-2 flex items-start gap-1 justify-end">
                            <FileText size={14} className="mt-0.5 shrink-0" />
                            {item.workerExplanation}
                          </p>
                        </div>

                        <div className="flex flex-col gap-2 shrink-0">
                          <button
                            onClick={(e) => {
                              e.stopPropagation();
                              setSelectedAppeal(item);
                            }}
                            className="p-2 rounded-[10px] bg-[#7895B2]/10 text-[#7895B2] hover:bg-[#7895B2]/20 transition-colors border border-[#7895B2]/20"
                            title="عرض التفاصيل"
                          >
                            <Eye size={18} />
                          </button>
                        </div>
                      </div>
                    </div>
                  ))
                )}
              </div>

              {/* Detail Panel */}
              <div className="lg:col-span-1">
                {selectedAppeal ? (
                  <div className="bg-white rounded-[16px] p-5 shadow-[0_4px_20px_rgba(0,0,0,0.04)] sticky top-6">
                    <h3 className="text-[16px] font-bold text-[#2F2F2F] mb-4 border-b border-[#F3F1ED] pb-3 text-right">
                      تفاصيل الطعن #{selectedAppeal.appealId}
                    </h3>

                    <div className="space-y-4">
                      <div className="text-right">
                        <p className="text-[#6B7280] text-[11px] mb-1">المهمة</p>
                        <p className="text-[#2F2F2F] font-semibold text-[14px]">
                          {selectedAppeal.entityTitle || `مهمة #${selectedAppeal.entityId}`}
                        </p>
                      </div>

                      <div className="text-right">
                        <p className="text-[#6B7280] text-[11px] mb-1">العامل</p>
                        <p className="text-[#2F2F2F] font-semibold text-[14px] flex items-center gap-2 justify-end">
                          <User size={16} />
                          {selectedAppeal.workerName}
                        </p>
                      </div>

                      <div className="bg-[#C86E5D]/10 rounded-[12px] p-3 border border-[#C86E5D]/20 text-right">
                        <p className="text-[#C86E5D] text-[11px] mb-1 font-semibold">سبب الرفض التلقائي</p>
                        <p className="text-[#2F2F2F] text-[13px]">
                          {selectedAppeal.originalRejectionReason || "تجاوز المسافة المسموح بها"}
                        </p>
                      </div>

                      <div className="bg-[#F5B300]/10 rounded-[12px] p-3 border border-[#F5B300]/20 text-right">
                        <p className="text-[#D4A100] text-[11px] mb-1 font-semibold flex items-center gap-1 justify-end">
                          <MapPin size={12} />
                          المسافة عن موقع المهمة
                        </p>
                        <p className="text-[#2F2F2F] text-[18px] font-bold">
                          {selectedAppeal.distanceMeters ? `${selectedAppeal.distanceMeters} متر` : "غير محدد"}
                        </p>
                        <p className="text-[#6B7280] text-[11px] mt-1">(الحد الأقصى: 500 متر)</p>
                      </div>

                      <div className="text-right">
                        <p className="text-[#6B7280] text-[11px] mb-1">تفسير العامل</p>
                        <p className="text-[#2F2F2F] text-[13px] bg-[#F3F1ED] rounded-[12px] p-3 border border-[#E5E7EB]">
                          {selectedAppeal.workerExplanation}
                        </p>
                      </div>

                      {selectedAppeal.evidencePhotoUrl && (
                        <div className="text-right">
                          <p className="text-[#6B7280] text-[11px] mb-2">صورة الإثبات</p>
                          <img
                            src={selectedAppeal.evidencePhotoUrl}
                            alt="Evidence"
                            className="w-full rounded-[12px] border border-[#E5E7EB] cursor-pointer hover:opacity-90"
                            onClick={() => window.open(selectedAppeal.evidencePhotoUrl, "_blank")}
                          />
                        </div>
                      )}

                      <div className="flex items-center gap-2 text-[#6B7280] text-[12px] justify-end">
                        <Clock size={14} />
                        تم الإرسال: {formatDate(selectedAppeal.submittedAt)}
                      </div>

                      <div className="text-right">
                        <label className="text-[#6B7280] text-[11px] mb-1 block">ملاحظات المراجعة</label>
                        <textarea
                          value={reviewNotes}
                          onChange={(e) => setReviewNotes(e.target.value)}
                          placeholder="اكتب ملاحظاتك هنا... (مطلوب للرفض)"
                          className="w-full h-24 p-3 bg-[#F3F1ED] rounded-[12px] text-right text-[14px] text-[#2F2F2F] placeholder:text-[#6B7280] border-0 outline-none focus:ring-2 focus:ring-[#7895B2]/30 resize-none"
                        />
                      </div>

                      {selectedAppeal.status === "Pending" && (
                        <div className="flex gap-3 pt-2">
                          <button
                            onClick={() => handleApprove(selectedAppeal)}
                            disabled={processing === selectedAppeal.appealId}
                            className="flex-1 flex items-center justify-center gap-2 px-4 py-3 bg-[#8FA36A] text-white rounded-[12px] font-semibold hover:bg-[#7B9055] transition-colors disabled:opacity-50"
                          >
                            {processing === selectedAppeal.appealId ? (
                              <div className="w-5 h-5 border-2 border-white border-t-transparent rounded-full animate-spin" />
                            ) : (
                              <>
                                <CheckCircle size={18} />
                                قبول الطعن
                              </>
                            )}
                          </button>
                          <button
                            onClick={() => handleReject(selectedAppeal)}
                            disabled={processing === selectedAppeal.appealId}
                            className="flex-1 flex items-center justify-center gap-2 px-4 py-3 bg-[#C86E5D] text-white rounded-[12px] font-semibold hover:bg-[#B55D4C] transition-colors disabled:opacity-50"
                          >
                            {processing === selectedAppeal.appealId ? (
                              <div className="w-5 h-5 border-2 border-white border-t-transparent rounded-full animate-spin" />
                            ) : (
                              <>
                                <XCircle size={18} />
                                رفض الطعن
                              </>
                            )}
                          </button>
                        </div>
                      )}
                    </div>
                  </div>
                ) : (
                  <div className="bg-white rounded-[16px] p-8 shadow-[0_4px_20px_rgba(0,0,0,0.04)] text-center">
                    <Scale className="mx-auto text-[#7895B2]/20 mb-4" size={48} />
                    <p className="text-[#6B7280] font-medium">اختر طعناً لعرض التفاصيل</p>
                  </div>
                )}
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
