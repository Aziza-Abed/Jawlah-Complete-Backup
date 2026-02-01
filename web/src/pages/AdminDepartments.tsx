import { useEffect, useState } from "react";
import {
  getDepartments,
  createDepartment,
  updateDepartment,
  deleteDepartment,
  type Department,
  type CreateDepartmentRequest,
  type UpdateDepartmentRequest,
} from "../api/departments";
import {
  Building,
  Plus,
  Pencil,
  Trash2,
  Users,
  CheckCircle,
  AlertCircle,
  X,
  Loader2,
  Search,
} from "lucide-react";

export default function AdminDepartments() {
  const [departments, setDepartments] = useState<Department[]>([]);
  const [loading, setLoading] = useState(true);
  const [message, setMessage] = useState<{ type: "success" | "error"; text: string } | null>(null);
  const [search, setSearch] = useState("");

  // Modal state
  const [showModal, setShowModal] = useState(false);
  const [editingDepartment, setEditingDepartment] = useState<Department | null>(null);
  const [saving, setSaving] = useState(false);

  // Form state
  const [formData, setFormData] = useState<CreateDepartmentRequest>({
    name: "",
    nameEnglish: "",
    code: "",
    description: "",
  });

  // Delete confirmation
  const [deleteConfirm, setDeleteConfirm] = useState<Department | null>(null);
  const [deleting, setDeleting] = useState(false);

  useEffect(() => {
    fetchDepartments();
  }, []);

  const fetchDepartments = async () => {
    try {
      setLoading(true);
      const data = await getDepartments();
      setDepartments(data);
    } catch (err) {
      console.error("Failed to fetch departments", err);
      setMessage({ type: "error", text: "فشل تحميل الأقسام" });
    } finally {
      setLoading(false);
    }
  };

  const openCreateModal = () => {
    setEditingDepartment(null);
    setFormData({ name: "", nameEnglish: "", code: "", description: "" });
    setShowModal(true);
  };

  const openEditModal = (dept: Department) => {
    setEditingDepartment(dept);
    setFormData({
      name: dept.name,
      nameEnglish: dept.nameEnglish || "",
      code: dept.code,
      description: dept.description || "",
    });
    setShowModal(true);
  };

  const closeModal = () => {
    setShowModal(false);
    setEditingDepartment(null);
    setFormData({ name: "", nameEnglish: "", code: "", description: "" });
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setMessage(null);

    try {
      if (editingDepartment) {
        const updateReq: UpdateDepartmentRequest = {
          name: formData.name,
          nameEnglish: formData.nameEnglish || undefined,
          code: formData.code,
          description: formData.description || undefined,
        };
        await updateDepartment(editingDepartment.departmentId, updateReq);
        setMessage({ type: "success", text: "تم تحديث القسم بنجاح" });
      } else {
        await createDepartment(formData);
        setMessage({ type: "success", text: "تم إنشاء القسم بنجاح" });
      }
      closeModal();
      fetchDepartments();
    } catch (err: any) {
      setMessage({
        type: "error",
        text: err.response?.data?.message || "فشل حفظ القسم",
      });
    } finally {
      setSaving(false);
      setTimeout(() => setMessage(null), 5000);
    }
  };

  const handleToggleActive = async (dept: Department) => {
    try {
      await updateDepartment(dept.departmentId, { isActive: !dept.isActive });
      setMessage({
        type: "success",
        text: dept.isActive ? "تم تعطيل القسم" : "تم تفعيل القسم",
      });
      fetchDepartments();
    } catch (err: any) {
      setMessage({
        type: "error",
        text: err.response?.data?.message || "فشل تحديث حالة القسم",
      });
    }
    setTimeout(() => setMessage(null), 3000);
  };

  const handleDelete = async () => {
    if (!deleteConfirm) return;
    setDeleting(true);
    try {
      await deleteDepartment(deleteConfirm.departmentId);
      setMessage({ type: "success", text: "تم حذف القسم بنجاح" });
      setDeleteConfirm(null);
      fetchDepartments();
    } catch (err: any) {
      setMessage({
        type: "error",
        text: err.response?.data?.message || "فشل حذف القسم",
      });
    } finally {
      setDeleting(false);
      setTimeout(() => setMessage(null), 5000);
    }
  };

  const filteredDepartments = departments.filter(
    (d) =>
      d.name.includes(search) ||
      d.code.toLowerCase().includes(search.toLowerCase()) ||
      (d.nameEnglish && d.nameEnglish.toLowerCase().includes(search.toLowerCase()))
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
          {/* Header (Corrected Layout) */}
          <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-8">
            <div className="text-right">
              <h1 className="font-sans font-black text-[28px] text-[#2F2F2F]">
                إدارة الأقسام الميدانية
              </h1>
              <p className="text-[14px] font-bold text-[#AFAFAF] mt-1">
                توزيع الصلاحيات على {departments.length} أقسام تابعة للبلدية
              </p>
            </div>
            <button
              onClick={openCreateModal}
              className="flex items-center justify-center gap-3 bg-[#7895B2] hover:bg-[#647e99] text-white px-6 py-3 rounded-[16px] transition-all font-black shadow-lg shadow-[#7895B2]/20 group"
            >
              <div className="p-1.5 bg-white/20 rounded-lg group-hover:rotate-90 transition-transform">
                <Plus size={20} />
              </div>
              <span>إضافة قسم جديد</span>
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
                placeholder="بحث عن قسم..."
                className="w-full h-[46px] bg-white rounded-[12px] border border-black/5 px-4 pr-12 text-right outline-none focus:ring-2 focus:ring-[#7895B2]/20 text-[#2F2F2F]"
              />
              <Search className="absolute right-4 top-1/2 -translate-y-1/2 text-[#6B7280]" size={18} />
            </div>
          </div>

          {/* Departments Grid */}
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {filteredDepartments.map((dept) => (
              <div
                key={dept.departmentId}
                className={`bg-white rounded-[24px] p-6 shadow-[0_4px_25px_rgba(0,0,0,0.03)] border border-black/5 hover:shadow-[0_12px_40px_rgba(0,0,0,0.06)] transition-all group ${
                  !dept.isActive ? "opacity-70 bg-[#F9F8F6]" : ""
                }`}
              >
                {/* Header (Corrected Alignment) */}
                <div className="flex items-start justify-between mb-6 flex-row-reverse">
                  <div className={`px-3 py-1 rounded-full text-[10px] font-black tracking-wider uppercase border ${
                    dept.isActive 
                      ? "bg-[#8FA36A]/10 text-[#8FA36A] border-[#8FA36A]/20" 
                      : "bg-[#AFAFAF]/10 text-[#AFAFAF] border-[#AFAFAF]/20"
                  }`}>
                    {dept.isActive ? "نشط" : "معطل"}
                  </div>
                  
                  <div className="flex items-center gap-4 flex-row-reverse">
                    <div className="text-right">
                      <h3 className="text-[18px] font-black text-[#2F2F2F] leading-tight">{dept.name}</h3>
                    </div>
                    <div className="w-12 h-12 bg-[#7895B2]/10 rounded-2xl flex items-center justify-center text-[#7895B2] group-hover:bg-[#7895B2] group-hover:text-white transition-colors duration-300">
                      <Building size={22} />
                    </div>
                  </div>
                </div>

                {/* Description & User Count */}
                <div className="space-y-4 mb-6">
                  {dept.description ? (
                    <p className="text-[13px] text-[#6B7280] text-right line-clamp-2 leading-relaxed min-h-[40px]">
                      {dept.description}
                    </p>
                  ) : (
                    <div className="h-[40px] flex items-center justify-end">
                      <span className="text-[12px] text-[#AFAFAF] italic font-medium">لا يوجد وصف للقسم</span>
                    </div>
                  )}

                  <div className="flex items-center justify-end gap-2 text-[13px] font-black text-[#2F2F2F] bg-[#F9F8F6] p-3 rounded-xl border border-black/5">
                    <span>{dept.usersCount} مستخدم في هذا القسم</span>
                    <div className="w-8 h-8 rounded-lg bg-white flex items-center justify-center border border-black/5 text-[#7895B2]">
                      <Users size={16} />
                    </div>
                  </div>
                </div>

                {/* Actions */}
                <div className="flex items-center justify-between pt-5 border-t border-black/5">
                  <div className="flex items-center gap-2">
                    <button
                      onClick={() => openEditModal(dept)}
                      className="w-10 h-10 bg-[#F3F1ED] text-[#7895B2] rounded-xl flex items-center justify-center hover:bg-[#7895B2] hover:text-white transition-all border border-black/5"
                      title="تعديل"
                    >
                      <Pencil size={16} />
                    </button>
                    <button
                      onClick={() => setDeleteConfirm(dept)}
                      disabled={dept.usersCount > 0}
                      className="w-10 h-10 bg-[#C86E5D]/10 text-[#C86E5D] rounded-xl flex items-center justify-center hover:bg-[#C86E5D] hover:text-white transition-all border border-[#C86E5D]/10 disabled:opacity-20 disabled:grayscale"
                      title={dept.usersCount > 0 ? "لا يمكن حذف قسم يحتوي مستخدمين" : "حذف"}
                    >
                      <Trash2 size={16} />
                    </button>
                  </div>

                  <button
                    onClick={() => handleToggleActive(dept)}
                    className={`px-5 py-2 rounded-xl text-[12px] font-black transition-all border shadow-sm ${
                      dept.isActive
                        ? "bg-white text-[#C86E5D] border-[#C86E5D]/20 hover:bg-[#C86E5D] hover:text-white"
                        : "bg-[#8FA36A] text-white border-[#8FA36A]/20 shadow-[#8FA36A]/20"
                    }`}
                  >
                    {dept.isActive ? "تعطيل القسم" : "تفعيل القسم"}
                  </button>
                </div>
              </div>
            ))}
          </div>

          {/* Empty State */}
          {filteredDepartments.length === 0 && (
            <div className="text-center py-16">
              <Building size={48} className="mx-auto text-[#7895B2]/30 mb-4" />
              <h3 className="text-[18px] font-bold text-[#2F2F2F] mb-2">
                {search ? "لا توجد نتائج" : "لا توجد أقسام"}
              </h3>
              <p className="text-[14px] text-[#6B7280]">
                {search ? "جرب البحث بكلمات مختلفة" : "قم بإضافة أول قسم للبدء"}
              </p>
            </div>
          )}

          {/* Create/Edit Modal */}
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
                    {editingDepartment ? "تعديل القسم" : "إضافة قسم جديد"}
                  </h2>
                </div>

                {/* Modal Body */}
                <form onSubmit={handleSubmit} className="p-5 space-y-4">
                  <div className="space-y-1.5 text-right">
                    <label className="text-[13px] font-semibold text-[#6B7280]">
                      اسم القسم (عربي) <span className="text-[#C86E5D]">*</span>
                    </label>
                    <input
                      type="text"
                      value={formData.name}
                      onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                      className="w-full h-[46px] bg-[#F3F1ED] rounded-[12px] px-4 text-right outline-none focus:ring-2 focus:ring-[#7895B2]/20 text-[#2F2F2F]"
                      placeholder="مثال: قسم الصحة والبيئة"
                      required
                    />
                  </div>

                  <div className="space-y-1.5 text-right">
                    <label className="text-[13px] font-semibold text-[#6B7280]">
                      اسم القسم (إنجليزي)
                    </label>
                    <input
                      type="text"
                      value={formData.nameEnglish}
                      onChange={(e) => setFormData({ ...formData, nameEnglish: e.target.value })}
                      className="w-full h-[46px] bg-[#F3F1ED] rounded-[12px] px-4 text-right outline-none focus:ring-2 focus:ring-[#7895B2]/20 text-[#2F2F2F]"
                      placeholder="Example: Health & Environment Dept."
                      dir="ltr"
                    />
                  </div>

                  <div className="space-y-1.5 text-right">
                    <label className="text-[13px] font-semibold text-[#6B7280]">
                      رمز القسم <span className="text-[#C86E5D]">*</span>
                    </label>
                    <input
                      type="text"
                      value={formData.code}
                      onChange={(e) => setFormData({ ...formData, code: e.target.value.toUpperCase() })}
                      className="w-full h-[46px] bg-[#F3F1ED] rounded-[12px] px-4 text-center font-sans outline-none focus:ring-2 focus:ring-[#7895B2]/20 text-[#2F2F2F] uppercase"
                      placeholder="HEALTH"
                      maxLength={20}
                      required
                    />
                    <p className="text-[11px] text-[#6B7280]">رمز فريد للقسم (حروف إنجليزية)</p>
                  </div>

                  <div className="space-y-1.5 text-right">
                    <label className="text-[13px] font-semibold text-[#6B7280]">الوصف</label>
                    <textarea
                      value={formData.description}
                      onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                      className="w-full h-[80px] bg-[#F3F1ED] rounded-[12px] px-4 py-3 text-right outline-none focus:ring-2 focus:ring-[#7895B2]/20 text-[#2F2F2F] resize-none"
                      placeholder="وصف مختصر لمهام القسم..."
                    />
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
                      {editingDepartment ? "تحديث" : "إضافة"}
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
                <h3 className="text-[18px] font-bold text-[#2F2F2F] mb-2">حذف القسم</h3>
                <p className="text-[14px] text-[#6B7280] mb-6">
                  هل أنت متأكد من حذف قسم "{deleteConfirm.name}"؟
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
