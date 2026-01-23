import { useEffect, useMemo, useState } from "react";
import { Scale, Search, CheckCircle, XCircle, Eye, Filter, X, MapPin, Clock, User, FileText } from "lucide-react";
import { getPendingAppeals, approveAppeal, rejectAppeal } from "../api/appeals";
import type { AppealResponse } from "../types/appeal";
import GlassCard from "../components/UI/GlassCard";

type FilterKey = "all" | "Pending" | "Approved" | "Rejected";

export default function AdminAppeals() {
  const [items, setItems] = useState<AppealResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [filter, setFilter] = useState<FilterKey>("Pending");
  const [q, setQ] = useState("");
  const [processing, setProcessing] = useState<number | null>(null);

  // Detail panel
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
      // Remove from list or update status
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
      // Remove from list
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
        x.workerName.toLowerCase().includes(query) ||
        x.workerExplanation.toLowerCase().includes(query) ||
        x.entityTitle?.toLowerCase().includes(query) ||
        String(x.appealId).includes(query)
      );
    });
  }, [items, filter, q]);

  const formatDate = (dateStr: string) => {
    const date = new Date(dateStr);
    return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")} ${String(date.getHours()).padStart(2, "0")}:${String(date.getMinutes()).padStart(2, "0")}`;
  };

  const statusColors = {
    Pending: "bg-warning/10 text-warning border-warning/30",
    Approved: "bg-secondary/10 text-secondary border-secondary/30",
    Rejected: "bg-accent/10 text-accent border-accent/30",
  };

  const statusLabels = {
    Pending: "قيد المراجعة",
    Approved: "تمت الموافقة",
    Rejected: "مرفوض",
  };

  return (
    <div className="min-h-screen bg-background-paper p-6 animate-fade-in">
      <div className="max-w-7xl mx-auto space-y-6">
        {/* Header */}
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="p-3 rounded-xl bg-primary/10 text-primary border border-primary/10">
              <Scale size={24} />
            </div>
            <div>
              <h1 className="text-2xl font-bold text-text-primary">مركز المراجعة</h1>
              <p className="text-text-secondary text-sm">
                مراجعة طلبات التظلم والاعتراضات على المهام
              </p>
            </div>
          </div>
          <div className="text-text-secondary text-sm">
            العدد الإجمالي: <span className="text-warning font-bold">{counts.Pending}</span>
          </div>
        </div>

        {/* Search & Filters */}
        <GlassCard>
          <div className="flex flex-col lg:flex-row gap-4">
            {/* Search */}
            <div className="flex-1 relative">
              <Search className="absolute right-4 top-1/2 -translate-y-1/2 text-text-muted" size={18} />
              <input
                value={q}
                onChange={(e) => setQ(e.target.value)}
                placeholder="بحث (اسم العامل / عنوان المهمة / رقم الطعن)..."
                className="w-full h-12 pr-12 pl-4 rounded-xl bg-primary/5 border border-primary/10 text-text-primary text-right placeholder:text-text-muted focus:outline-none focus:border-primary/30 transition-colors"
              />
              {q && (
                <button
                  onClick={() => setQ("")}
                  className="absolute left-4 top-1/2 -translate-y-1/2 text-text-muted hover:text-text-primary"
                >
                  <X size={16} />
                </button>
              )}
            </div>

            {/* Filter Chips */}
            <div className="flex items-center gap-2 flex-wrap">
              <Filter size={16} className="text-text-muted" />
              {(["all", "Pending"] as FilterKey[]).map((key) => (
                <button
                  key={key}
                  onClick={() => setFilter(key)}
                  className={`px-3 py-2 rounded-lg text-sm font-medium transition-all border ${
                    filter === key
                      ? "bg-primary text-white border-primary shadow-lg shadow-primary/20"
                      : "bg-primary/5 text-text-secondary border-primary/5 hover:bg-primary/10"
                  }`}
                >
                  {key === "all" ? "الكل" : statusLabels[key]}
                  <span className={`mr-2 px-1.5 py-0.5 rounded text-xs ${
                    filter === key ? "bg-white/20" : "bg-primary/10"
                  }`}>
                    {counts[key]}
                  </span>
                </button>
              ))}
            </div>
          </div>
        </GlassCard>

        {/* Loading State */}
        {loading && (
          <div className="text-center py-12">
            <div className="w-8 h-8 border-2 border-primary border-t-transparent rounded-full animate-spin mx-auto mb-4" />
            <p className="text-text-secondary">جاري التحميل...</p>
          </div>
        )}

        {/* Error State */}
        {error && (
          <GlassCard className="border-red-500/30 bg-red-500/10">
            <p className="text-red-400 text-center">{error}</p>
            <button onClick={fetchAppeals} className="mt-2 mx-auto block px-4 py-2 bg-primary text-white rounded-lg">
              إعادة المحاولة
            </button>
          </GlassCard>
        )}

        {/* Appeals List */}
        {!loading && !error && (
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
            {/* List */}
            <div className="lg:col-span-2 space-y-3">
              {filtered.length === 0 ? (
                <GlassCard className="text-center py-12">
                  <Scale className="mx-auto text-text-muted mb-4" size={48} />
                  <p className="text-text-secondary">لا يوجد بيانات حالياً</p>
                </GlassCard>
              ) : (
                filtered.map((item) => (
                  <GlassCard
                    key={item.appealId}
                    variant="hover"
                    className={`cursor-pointer transition-all ${
                      selectedAppeal?.appealId === item.appealId ? "ring-2 ring-primary" : ""
                    }`}
                    onClick={() => {
                      setSelectedAppeal(item);
                      setReviewNotes("");
                    }}
                  >
                    <div className="flex items-start justify-between gap-4">
                      {/* Right Side - Main Info */}
                      <div className="flex-1 min-w-0">
                        <div className="flex items-start justify-between gap-3 mb-3">
                          <div>
                            <h3 className="text-text-primary font-semibold text-lg truncate">
                              مراجعة #{item.appealId} - {item.entityTitle || `مهمة #${item.entityId}`}
                            </h3>
                            <p className="text-text-muted text-sm mt-1">
                              <User size={14} className="inline ml-1" />
                              {item.workerName}
                            </p>
                          </div>
                          <span className={`px-3 py-1 rounded-full text-xs font-medium border shrink-0 ${statusColors[item.status]}`}>
                            {statusLabels[item.status]}
                          </span>
                        </div>

                        {/* Meta Info */}
                        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                          <div className="bg-primary/5 rounded-lg px-3 py-2 border border-primary/5">
                            <p className="text-text-muted text-xs">المسافة عن الموقع</p>
                            <p className="text-accent text-sm font-bold">
                              {item.distanceMeters ? `${item.distanceMeters} متر` : "غير محدد"}
                            </p>
                          </div>
                          <div className="bg-primary/5 rounded-lg px-3 py-2 border border-primary/5">
                            <p className="text-text-muted text-xs">تاريخ الطعن</p>
                            <p className="text-text-primary text-sm font-medium">{formatDate(item.submittedAt)}</p>
                          </div>
                        </div>

                        {/* Explanation Preview */}
                        <p className="text-text-secondary text-sm mt-3 line-clamp-2">
                          <FileText size={14} className="inline ml-1" />
                          {item.workerExplanation}
                        </p>
                      </div>

                      {/* Left Side - Quick Actions */}
                      <div className="flex flex-col gap-2 shrink-0">
                        <button
                          onClick={(e) => {
                            e.stopPropagation();
                            setSelectedAppeal(item);
                          }}
                          className="p-2 rounded-lg bg-primary/10 text-primary hover:bg-primary/20 transition-colors border border-primary/10"
                          title="عرض التفاصيل"
                        >
                          <Eye size={18} />
                        </button>
                      </div>
                    </div>
                  </GlassCard>
                ))
              )}
            </div>

            {/* Detail Panel */}
            <div className="lg:col-span-1">
              {selectedAppeal ? (
                <GlassCard className="sticky top-6">
                  <h3 className="text-lg font-bold text-text-primary mb-4 border-b border-primary/10 pb-3">
                    تفاصيل الطعن #{selectedAppeal.appealId}
                  </h3>

                  <div className="space-y-4">
                    {/* Task Info */}
                    <div>
                      <p className="text-text-muted text-xs mb-1">المهمة</p>
                      <p className="text-text-primary font-medium">
                        {selectedAppeal.entityTitle || `مهمة #${selectedAppeal.entityId}`}
                      </p>
                    </div>

                    {/* Worker */}
                    <div>
                      <p className="text-text-muted text-xs mb-1">العامل</p>
                      <p className="text-text-primary font-medium flex items-center gap-2">
                        <User size={16} />
                        {selectedAppeal.workerName}
                      </p>
                    </div>

                    {/* Original Rejection */}
                    <div className="bg-accent/10 rounded-lg p-3 border border-accent/20">
                      <p className="text-accent text-xs mb-1 font-medium">سبب الرفض التلقائي</p>
                      <p className="text-text-primary text-sm">
                        {selectedAppeal.originalRejectionReason || "تجاوز المسافة المسموح بها"}
                      </p>
                    </div>

                    {/* Distance */}
                    <div className="bg-warning/10 rounded-lg p-3 border border-warning/20">
                      <p className="text-warning text-xs mb-1 font-medium flex items-center gap-1">
                        <MapPin size={14} />
                        المسافة عن موقع المهمة
                      </p>
                      <p className="text-text-primary text-lg font-bold">
                        {selectedAppeal.distanceMeters ? `${selectedAppeal.distanceMeters} متر` : "غير محدد"}
                      </p>
                      <p className="text-text-muted text-xs mt-1">
                        (الحد الأقصى: 500 متر)
                      </p>
                    </div>

                    {/* Worker Explanation */}
                    <div>
                      <p className="text-text-muted text-xs mb-1">تفسير العامل</p>
                      <p className="text-text-primary text-sm bg-primary/5 rounded-lg p-3 border border-primary/10">
                        {selectedAppeal.workerExplanation}
                      </p>
                    </div>

                    {/* Evidence Photo */}
                    {selectedAppeal.evidencePhotoUrl && (
                      <div>
                        <p className="text-text-muted text-xs mb-2">صورة الإثبات</p>
                        <img
                          src={selectedAppeal.evidencePhotoUrl}
                          alt="Evidence"
                          className="w-full rounded-lg border border-primary/10 cursor-pointer hover:opacity-90"
                          onClick={() => window.open(selectedAppeal.evidencePhotoUrl, "_blank")}
                        />
                      </div>
                    )}

                    {/* Submitted At */}
                    <div className="flex items-center gap-2 text-text-muted text-sm">
                      <Clock size={14} />
                      تم الإرسال: {formatDate(selectedAppeal.submittedAt)}
                    </div>

                    {/* Review Notes */}
                    <div>
                      <label className="text-text-muted text-xs mb-1 block">ملاحظات المراجعة</label>
                      <textarea
                        value={reviewNotes}
                        onChange={(e) => setReviewNotes(e.target.value)}
                        placeholder="اكتب ملاحظاتك هنا... (مطلوب للرفض)"
                        className="w-full h-24 p-3 rounded-lg bg-primary/5 border border-primary/10 text-text-primary text-right placeholder:text-text-muted focus:outline-none focus:border-primary/30 resize-none"
                      />
                    </div>

                    {/* Actions */}
                    {selectedAppeal.status === "Pending" && (
                      <div className="flex gap-3 pt-2">
                        <button
                          onClick={() => handleApprove(selectedAppeal)}
                          disabled={processing === selectedAppeal.appealId}
                          className="flex-1 flex items-center justify-center gap-2 px-4 py-3 bg-secondary text-white rounded-xl font-medium hover:bg-secondary/90 transition-colors disabled:opacity-50"
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
                          className="flex-1 flex items-center justify-center gap-2 px-4 py-3 bg-accent text-white rounded-xl font-medium hover:bg-accent/90 transition-colors disabled:opacity-50"
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
                </GlassCard>
              ) : (
                <GlassCard className="text-center py-12">
                  <Scale className="mx-auto text-text-muted mb-4" size={48} />
                  <p className="text-text-secondary">اختر طعناً لعرض التفاصيل</p>
                </GlassCard>
              )}
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
