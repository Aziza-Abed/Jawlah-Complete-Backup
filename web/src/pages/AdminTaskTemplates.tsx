import { useEffect, useState } from "react";
import {
  getTaskTemplates,
  createTaskTemplate,
  deleteTaskTemplate,
  type TaskTemplate,
  type CreateTaskTemplateRequest,
} from "../api/templates";
import { getZones } from "../api/zones";
import type { ZoneResponse } from "../types/zone";
import {
  FileText,
  Plus,
  Trash2,
  CheckCircle,
  AlertCircle,
  X,
  Loader2,
  Search,
  Clock,
  MapPin,
  Calendar,
} from "lucide-react";

type FrequencyOption = "Daily" | "Weekly" | "Monthly";

const frequencyLabels: Record<FrequencyOption, string> = {
  Daily: "يومي",
  Weekly: "أسبوعي",
  Monthly: "شهري",
};

export default function AdminTaskTemplates() {
  const [templates, setTemplates] = useState<TaskTemplate[]>([]);
  const [zones, setZones] = useState<ZoneResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [message, setMessage] = useState<{ type: "success" | "error"; text: string } | null>(null);
  const [search, setSearch] = useState("");

  // Modal state
  const [showModal, setShowModal] = useState(false);
  const [saving, setSaving] = useState(false);

  // Form state
  const [formData, setFormData] = useState<CreateTaskTemplateRequest>({
    title: "",
    description: "",
    zoneId: undefined,
    frequency: "Daily",
    time: "08:00",
  });

  // Delete confirmation
  const [deleteConfirm, setDeleteConfirm] = useState<TaskTemplate | null>(null);
  const [deleting, setDeleting] = useState(false);

  useEffect(() => {
    fetchData();
  }, []);

  const fetchData = async () => {
    try {
      setLoading(true);
      const [templatesData, zonesData] = await Promise.all([
        getTaskTemplates(),
        getZones(),
      ]);
      setTemplates(templatesData);
      setZones(zonesData);
    } catch (err) {
      console.error("Failed to fetch data", err);
      setMessage({ type: "error", text: "فشل تحميل البيانات" });
    } finally {
      setLoading(false);
    }
  };

  const openCreateModal = () => {
    setFormData({
      title: "",
      description: "",
      zoneId: undefined,
      frequency: "Daily",
      time: "08:00",
    });
    setShowModal(true);
  };

  const closeModal = () => {
    setShowModal(false);
    setFormData({
      title: "",
      description: "",
      zoneId: undefined,
      frequency: "Daily",
      time: "08:00",
    });
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setMessage(null);

    try {
      await createTaskTemplate(formData);
      setMessage({ type: "success", text: "تم إنشاء القالب بنجاح" });
      closeModal();
      fetchData();
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : "فشل حفظ القالب";
      setMessage({ type: "error", text: errorMessage });
    } finally {
      setSaving(false);
      setTimeout(() => setMessage(null), 5000);
    }
  };

  const handleDelete = async () => {
    if (!deleteConfirm) return;
    setDeleting(true);
    try {
      await deleteTaskTemplate(deleteConfirm.id);
      setMessage({ type: "success", text: "تم حذف القالب بنجاح" });
      setDeleteConfirm(null);
      fetchData();
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : "فشل حذف القالب";
      setMessage({ type: "error", text: errorMessage });
    } finally {
      setDeleting(false);
      setTimeout(() => setMessage(null), 5000);
    }
  };

  const filteredTemplates = templates.filter(
    (t) =>
      t.title.includes(search) ||
      t.description.includes(search) ||
      t.zoneName.includes(search)
  );

  if (loading) {
    return (
      <div className="h-full w-full bg-[#F3F1ED] flex items-center justify-center">
        <div className="text-[#2F2F2F]">جاري التحميل...</div>
      </div>
    );
  }

  return (
    <div className="h-full w-full bg-[#F3F1ED] overflow-auto">
      <div className="p-4 sm:p-6 md:p-8">
        <div className="max-w-[1200px] mx-auto">
          {/* Header */}
          <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-8">
            <div className="text-right">
              <h1 className="font-sans font-black text-[28px] text-[#2F2F2F]">
                قوالب المهام
              </h1>
              <p className="text-[14px] font-bold text-[#AFAFAF] mt-1">
                إدارة {templates.length} قوالب للمهام المتكررة
              </p>
            </div>
            <button
              onClick={openCreateModal}
              className="flex items-center justify-center gap-3 bg-[#7895B2] hover:bg-[#647e99] text-white px-6 py-3 rounded-[16px] transition-all font-black shadow-lg shadow-[#7895B2]/20 group"
            >
              <div className="p-1.5 bg-white/20 rounded-lg group-hover:rotate-90 transition-transform">
                <Plus size={20} />
              </div>
              <span>إضافة قالب جديد</span>
            </button>
          </div>

          {/* Message */}
          {message && (
            <div
              className={`mb-4 p-4 rounded-[12px] flex items-center justify-end gap-3 ${
                message.type === "success"
                  ? "bg-[#8FA36A]/10 text-[#8FA36A]"
                  : "bg-[#C86E5D]/10 text-[#C86E5D]"
              }`}
            >
              <span className="font-semibold">{message.text}</span>
              {message.type === "success" ? <CheckCircle size={20} /> : <AlertCircle size={20} />}
            </div>
          )}

          {/* Search */}
          <div className="mb-6">
            <div className="relative max-w-md">
              <input
                type="text"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder="بحث عن قالب..."
                className="w-full h-[46px] bg-white rounded-[12px] border border-black/5 px-4 pr-12 text-right outline-none focus:ring-2 focus:ring-[#7895B2]/20 text-[#2F2F2F]"
              />
              <Search className="absolute right-4 top-1/2 -translate-y-1/2 text-[#6B7280]" size={18} />
            </div>
          </div>

          {/* Templates Grid */}
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {filteredTemplates.map((template) => (
              <div
                key={template.id}
                className={`bg-white rounded-[24px] p-6 shadow-[0_4px_25px_rgba(0,0,0,0.03)] border border-black/5 hover:shadow-[0_12px_40px_rgba(0,0,0,0.06)] transition-all group ${
                  !template.isActive ? "opacity-70 bg-[#F9F8F6]" : ""
                }`}
              >
                {/* Header */}
                <div className="flex items-start justify-between mb-6 flex-row-reverse">
                  <div className={`px-3 py-1 rounded-full text-[10px] font-black tracking-wider uppercase border ${
                    template.isActive
                      ? "bg-[#8FA36A]/10 text-[#8FA36A] border-[#8FA36A]/20"
                      : "bg-[#AFAFAF]/10 text-[#AFAFAF] border-[#AFAFAF]/20"
                  }`}>
                    {template.isActive ? "نشط" : "معطل"}
                  </div>

                  <div className="flex items-center gap-4 flex-row-reverse">
                    <div className="text-right">
                      <h3 className="text-[18px] font-black text-[#2F2F2F] leading-tight">{template.title}</h3>
                    </div>
                    <div className="w-12 h-12 bg-[#7895B2]/10 rounded-2xl flex items-center justify-center text-[#7895B2] group-hover:bg-[#7895B2] group-hover:text-white transition-colors duration-300">
                      <FileText size={22} />
                    </div>
                  </div>
                </div>

                {/* Description & Details */}
                <div className="space-y-4 mb-6">
                  {template.description ? (
                    <p className="text-[13px] text-[#6B7280] text-right line-clamp-2 leading-relaxed min-h-[40px]">
                      {template.description}
                    </p>
                  ) : (
                    <div className="h-[40px] flex items-center justify-end">
                      <span className="text-[12px] text-[#AFAFAF] italic font-medium">لا يوجد وصف</span>
                    </div>
                  )}

                  {/* Frequency */}
                  <div className="flex items-center justify-end gap-2 text-[13px] font-black text-[#2F2F2F] bg-[#F9F8F6] p-3 rounded-xl border border-black/5">
                    <span>{frequencyLabels[template.frequency as FrequencyOption] || template.frequency}</span>
                    <div className="w-8 h-8 rounded-lg bg-white flex items-center justify-center border border-black/5 text-[#7895B2]">
                      <Calendar size={16} />
                    </div>
                  </div>

                  {/* Time */}
                  <div className="flex items-center justify-end gap-2 text-[13px] font-black text-[#2F2F2F] bg-[#F9F8F6] p-3 rounded-xl border border-black/5">
                    <span>{template.time}</span>
                    <div className="w-8 h-8 rounded-lg bg-white flex items-center justify-center border border-black/5 text-[#7895B2]">
                      <Clock size={16} />
                    </div>
                  </div>

                  {/* Zone */}
                  {template.zoneName && (
                    <div className="flex items-center justify-end gap-2 text-[13px] font-black text-[#2F2F2F] bg-[#F9F8F6] p-3 rounded-xl border border-black/5">
                      <span>{template.zoneName}</span>
                      <div className="w-8 h-8 rounded-lg bg-white flex items-center justify-center border border-black/5 text-[#7895B2]">
                        <MapPin size={16} />
                      </div>
                    </div>
                  )}
                </div>

                {/* Actions */}
                <div className="flex items-center justify-between pt-5 border-t border-black/5">
                  <button
                    onClick={() => setDeleteConfirm(template)}
                    className="w-10 h-10 bg-[#C86E5D]/10 text-[#C86E5D] rounded-xl flex items-center justify-center hover:bg-[#C86E5D] hover:text-white transition-all border border-[#C86E5D]/10"
                    title="حذف"
                  >
                    <Trash2 size={16} />
                  </button>
                </div>
              </div>
            ))}
          </div>

          {/* Empty State */}
          {filteredTemplates.length === 0 && (
            <div className="text-center py-16">
              <FileText size={48} className="mx-auto text-[#7895B2]/30 mb-4" />
              <h3 className="text-[18px] font-bold text-[#2F2F2F] mb-2">
                {search ? "لا توجد نتائج" : "لا توجد قوالب"}
              </h3>
              <p className="text-[14px] text-[#6B7280]">
                {search ? "جرب البحث بكلمات مختلفة" : "قم بإضافة أول قالب للبدء"}
              </p>
            </div>
          )}

          {/* Create Modal */}
          {showModal && (
            <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4">
              <div className="bg-white rounded-[16px] shadow-2xl w-full max-w-lg">
                {/* Modal Header */}
                <div className="flex items-center justify-between p-5 border-b border-black/5">
                  <button
                    onClick={closeModal}
                    className="p-2 hover:bg-[#F3F1ED] rounded-[8px] transition-all"
                  >
                    <X size={18} className="text-[#6B7280]" />
                  </button>
                  <h2 className="text-[18px] font-bold text-[#2F2F2F]">
                    إضافة قالب جديد
                  </h2>
                </div>

                {/* Modal Body */}
                <form onSubmit={handleSubmit} className="p-5 space-y-4">
                  <div className="space-y-1.5 text-right">
                    <label className="text-[13px] font-semibold text-[#6B7280]">
                      عنوان المهمة <span className="text-[#C86E5D]">*</span>
                    </label>
                    <input
                      type="text"
                      value={formData.title}
                      onChange={(e) => setFormData({ ...formData, title: e.target.value })}
                      className="w-full h-[46px] bg-[#F3F1ED] rounded-[12px] px-4 text-right outline-none focus:ring-2 focus:ring-[#7895B2]/20 text-[#2F2F2F]"
                      placeholder="مثال: جولة تفتيشية صباحية"
                      required
                    />
                  </div>

                  <div className="space-y-1.5 text-right">
                    <label className="text-[13px] font-semibold text-[#6B7280]">
                      الوصف
                    </label>
                    <textarea
                      value={formData.description}
                      onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                      className="w-full h-[80px] bg-[#F3F1ED] rounded-[12px] px-4 py-3 text-right outline-none focus:ring-2 focus:ring-[#7895B2]/20 text-[#2F2F2F] resize-none"
                      placeholder="وصف تفصيلي للمهمة..."
                    />
                  </div>

                  <div className="space-y-1.5 text-right">
                    <label className="text-[13px] font-semibold text-[#6B7280]">
                      المنطقة (اختياري)
                    </label>
                    <select
                      value={formData.zoneId || ""}
                      onChange={(e) => setFormData({ ...formData, zoneId: e.target.value ? parseInt(e.target.value) : undefined })}
                      className="w-full h-[46px] bg-[#F3F1ED] rounded-[12px] px-4 text-right outline-none focus:ring-2 focus:ring-[#7895B2]/20 text-[#2F2F2F]"
                    >
                      <option value="">جميع المناطق</option>
                      {zones.map((zone) => (
                        <option key={zone.zoneId} value={zone.zoneId}>
                          {zone.zoneName}
                        </option>
                      ))}
                    </select>
                  </div>

                  <div className="grid grid-cols-2 gap-4">
                    <div className="space-y-1.5 text-right">
                      <label className="text-[13px] font-semibold text-[#6B7280]">
                        التكرار <span className="text-[#C86E5D]">*</span>
                      </label>
                      <select
                        value={formData.frequency}
                        onChange={(e) => setFormData({ ...formData, frequency: e.target.value })}
                        className="w-full h-[46px] bg-[#F3F1ED] rounded-[12px] px-4 text-right outline-none focus:ring-2 focus:ring-[#7895B2]/20 text-[#2F2F2F]"
                        required
                      >
                        <option value="Daily">يومي</option>
                        <option value="Weekly">أسبوعي</option>
                        <option value="Monthly">شهري</option>
                      </select>
                    </div>

                    <div className="space-y-1.5 text-right">
                      <label className="text-[13px] font-semibold text-[#6B7280]">
                        الوقت <span className="text-[#C86E5D]">*</span>
                      </label>
                      <input
                        type="time"
                        value={formData.time}
                        onChange={(e) => setFormData({ ...formData, time: e.target.value })}
                        className="w-full h-[46px] bg-[#F3F1ED] rounded-[12px] px-4 text-center outline-none focus:ring-2 focus:ring-[#7895B2]/20 text-[#2F2F2F]"
                        required
                      />
                    </div>
                  </div>

                  {/* Modal Actions */}
                  <div className="flex gap-3 pt-2">
                    <button
                      type="button"
                      onClick={closeModal}
                      className="flex-1 h-[46px] rounded-[12px] border border-black/10 text-[#2F2F2F] hover:bg-[#F3F1ED] transition-all font-semibold"
                    >
                      إلغاء
                    </button>
                    <button
                      type="submit"
                      disabled={saving}
                      className="flex-1 h-[46px] bg-[#7895B2] hover:opacity-90 text-white rounded-[12px] font-semibold transition-all disabled:opacity-50 flex items-center justify-center gap-2"
                    >
                      {saving && <Loader2 size={18} className="animate-spin" />}
                      إضافة
                    </button>
                  </div>
                </form>
              </div>
            </div>
          )}

          {/* Delete Confirmation Modal */}
          {deleteConfirm && (
            <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4">
              <div className="bg-white rounded-[16px] shadow-2xl w-full max-w-md p-6 text-center">
                <div className="w-14 h-14 bg-[#C86E5D]/10 rounded-full flex items-center justify-center mx-auto mb-4">
                  <Trash2 size={28} className="text-[#C86E5D]" />
                </div>
                <h3 className="text-[18px] font-bold text-[#2F2F2F] mb-2">حذف القالب</h3>
                <p className="text-[14px] text-[#6B7280] mb-6">
                  هل أنت متأكد من حذف قالب "{deleteConfirm.title}"؟
                  <br />
                  <span className="text-[#C86E5D] text-[13px]">هذا الإجراء لا يمكن التراجع عنه</span>
                </p>
                <div className="flex gap-3">
                  <button
                    onClick={() => setDeleteConfirm(null)}
                    className="flex-1 h-[46px] rounded-[12px] border border-black/10 text-[#2F2F2F] hover:bg-[#F3F1ED] transition-all font-semibold"
                  >
                    إلغاء
                  </button>
                  <button
                    onClick={handleDelete}
                    disabled={deleting}
                    className="flex-1 h-[46px] bg-[#C86E5D] hover:opacity-90 text-white rounded-[12px] font-semibold transition-all disabled:opacity-50 flex items-center justify-center gap-2"
                  >
                    {deleting && <Loader2 size={18} className="animate-spin" />}
                    حذف
                  </button>
                </div>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
