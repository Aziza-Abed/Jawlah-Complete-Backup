import React, { useEffect, useState, useRef } from "react";
import { useNavigate } from "react-router-dom";
import { getProfile, updateProfile, uploadProfilePhoto } from "../api/users";
import { STORAGE_KEYS } from "../constants/storageKeys";
import type { UserResponse } from "../types/user";
import { User, Phone, Mail, Shield, Save, CheckCircle, Camera, Calendar, Clock, Building, UserCog, KeyRound } from "lucide-react";

export default function Profile() {
  const navigate = useNavigate();
  const [profile, setProfile] = useState<UserResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [uploadingPhoto, setUploadingPhoto] = useState(false);
  const [message, setMessage] = useState<{ type: "success" | "error"; text: string } | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [formData, setFormData] = useState({
    fullName: "",
    phoneNumber: "",
    email: "",
  });

  useEffect(() => {
    fetchProfile();
  }, []);

  const fetchProfile = async () => {
    try {
      const data = await getProfile();
      setProfile(data);
      setFormData({
        fullName: data.fullName,
        phoneNumber: data.phoneNumber,
        email: data.email || "",
      });
    } catch (err) {
      console.error("Failed to fetch profile", err);
    } finally {
      setLoading(false);
    }
  };

  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});

  const validateProfileForm = (): boolean => {
    const errors: Record<string, string> = {};
    if (!formData.fullName.trim()) {
      errors.fullName = "الاسم الكامل مطلوب";
    } else if (formData.fullName.trim().length < 3) {
      errors.fullName = "الاسم يجب أن يكون 3 أحرف على الأقل";
    }
    if (!formData.phoneNumber.trim()) {
      errors.phoneNumber = "رقم الهاتف مطلوب";
    } else if (!/^\+?[0-9]{9,15}$/.test(formData.phoneNumber.replace(/\s/g, ''))) {
      errors.phoneNumber = "رقم الهاتف غير صالح (9-15 رقم)";
    }
    if (formData.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
      errors.email = "البريد الإلكتروني غير صالح";
    }
    setFieldErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setMessage(null);
    if (!validateProfileForm()) return;
    setSaving(true);

    try {
      const updated = await updateProfile(formData);
      setProfile(updated);
      setMessage({ type: "success", text: "تم تحديث الملف الشخصي بنجاح" });
      setTimeout(() => setMessage(null), 3000);
    } catch (err) {
      const axiosErr = err as { response?: { data?: { message?: string } } };
      setMessage({
        type: "error",
        text: axiosErr.response?.data?.message || "فشل تحديث الملف الشخصي"
      });
    } finally {
      setSaving(false);
    }
  };

  const handlePhotoUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    // Validate client-side
    const validTypes = ["image/jpeg", "image/png", "image/gif"];
    if (!validTypes.includes(file.type)) {
      setMessage({ type: "error", text: "يرجى اختيار صورة بصيغة JPG أو PNG أو GIF" });
      return;
    }
    setUploadingPhoto(true);
    setMessage(null);
    try {
      const photoUrl = await uploadProfilePhoto(file);
      setProfile(prev => prev ? { ...prev, profilePhotoUrl: photoUrl } : prev);
      // Update localStorage so Topbar reflects the new photo immediately
      try {
        const userStr = localStorage.getItem(STORAGE_KEYS.USER);
        if (userStr) {
          const user = JSON.parse(userStr);
          user.profilePhotoUrl = photoUrl;
          localStorage.setItem(STORAGE_KEYS.USER, JSON.stringify(user));
        }
      } catch {}
      setMessage({ type: "success", text: "تم تحديث الصورة الشخصية بنجاح" });
      setTimeout(() => setMessage(null), 3000);
    } catch (err) {
      const axiosErr = err as { response?: { data?: { message?: string } } };
      setMessage({
        type: "error",
        text: axiosErr.response?.data?.message || "فشل رفع الصورة"
      });
    } finally {
      setUploadingPhoto(false);
      // Reset file input
      if (fileInputRef.current) fileInputRef.current.value = "";
    }
  };

  const formatDate = (dateStr?: string) => {
    if (!dateStr) return "—";
    return new Date(dateStr).toLocaleDateString("ar-PS", {
      year: "numeric",
      month: "long",
      day: "numeric",
    });
  };

  const formatDateTime = (dateStr?: string) => {
    if (!dateStr) return "—";
    return new Date(dateStr).toLocaleDateString("ar-PS", {
      year: "numeric",
      month: "long",
      day: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    });
  };

  const getRoleLabel = (role?: string) => {
    switch (role) {
      case "Admin": return "مدير النظام";
      case "Supervisor": return "مشرف";
      case "Worker": return "عامل ميداني";
      default: return role || "—";
    }
  };

  if (loading) {
    return (
      <div className="h-full w-full flex items-center justify-center">
        <div className="text-[#2F2F2F]">جاري التحميل...</div>
      </div>
    );
  }

  return (
    <div className="h-full w-full bg-[#F3F1ED] overflow-auto">
      <div className="p-4 sm:p-6 md:p-8">
        <div className="max-w-[800px] mx-auto">
          {/* Header */}
          <div className="flex items-center justify-between gap-6 mb-8">
            <div className="flex items-center gap-4">
                 <div className="p-3.5 rounded-[18px] bg-[#7895B2]/10 text-[#7895B2] shadow-sm">
                    <User size={28} />
                </div>
                <div className="text-right">
                    <h1 className="font-black text-[28px] text-[#2F2F2F] tracking-tight">
                        الملف الشخصي
                    </h1>
                    <p className="text-[14px] font-bold text-[#AFAFAF] mt-1">تعديل بياناتك الشخصية ومعلومات الحساب</p>
                </div>
            </div>
          </div>

          {/* Messages */}
          {message && (
            <div className={`mb-6 p-4 rounded-[18px] flex items-center justify-end gap-3 animate-in fade-in slide-in-from-top-4 duration-500 border ${
              message.type === "success" ? "bg-[#8FA36A]/10 text-[#8FA36A] border-[#8FA36A]/20" : "bg-[#C86E5D]/10 text-[#C86E5D] border-[#C86E5D]/20"
            }`}>
              <span className="text-right font-black text-[14px]">{message.text}</span>
              {message.type === "success" && <CheckCircle size={20} />}
            </div>
          )}

          {/* Profile Card with Banner */}
          <div className="bg-white rounded-[24px] shadow-[0_4px_25px_rgba(0,0,0,0.03)] border border-black/5 overflow-hidden mb-6">
            {/* Banner */}
            <div className="bg-[#7895B2] p-8 text-white relative overflow-hidden group">
              <div className="absolute top-0 left-0 w-full h-full bg-[url('https://grainy-gradients.vercel.app/noise.svg')] opacity-10 pointer-events-none"></div>
              <div className="flex flex-col md:flex-row items-center gap-8 relative z-10">
                {/* Profile Photo */}
                <div className="relative">
                  <div className="w-32 h-32 rounded-[28px] bg-white/20 flex items-center justify-center border-4 border-white/20 backdrop-blur-md shadow-2xl transition-transform group-hover:scale-105 overflow-hidden">
                    {profile?.profilePhotoUrl ? (
                      <img
                        src={profile.profilePhotoUrl}
                        alt={profile.fullName}
                        className="w-full h-full object-cover"
                      />
                    ) : (
                      <User size={64} className="text-white drop-shadow-lg" />
                    )}
                  </div>
                  {/* Camera overlay button */}
                  <button
                    type="button"
                    onClick={() => fileInputRef.current?.click()}
                    disabled={uploadingPhoto}
                    className="absolute -bottom-2 -left-2 w-10 h-10 bg-white rounded-full flex items-center justify-center shadow-lg hover:scale-110 transition-transform disabled:opacity-50"
                  >
                    {uploadingPhoto ? (
                      <div className="w-5 h-5 border-2 border-[#7895B2] border-t-transparent rounded-full animate-spin" />
                    ) : (
                      <Camera size={18} className="text-[#7895B2]" />
                    )}
                  </button>
                  <input
                    ref={fileInputRef}
                    type="file"
                    accept="image/jpeg,image/png,image/gif"
                    onChange={handlePhotoUpload}
                    className="hidden"
                  />
                </div>

                <div className="text-right flex-1">
                  <h2 className="text-[32px] font-black tracking-tight">{profile?.fullName}</h2>
                  <p className="text-white/70 text-[14px] font-bold mt-1">@{profile?.username}</p>
                  <div className="flex items-center justify-end gap-2 mt-3">
                    <span className="px-3 py-1 bg-white/20 rounded-full text-[12px] font-black uppercase tracking-widest backdrop-blur-sm">
                      {getRoleLabel(profile?.role)}
                    </span>
                    <Shield size={16} className="text-white/80" />
                  </div>
                </div>
              </div>
            </div>

            {/* Account Info Cards */}
            <div className="p-6 grid grid-cols-2 md:grid-cols-4 gap-4">
              {profile?.department && (
                <div className="bg-[#F9F8F6] rounded-[16px] p-4 text-right">
                  <div className="flex items-center justify-end gap-2 mb-2">
                    <span className="text-[10px] text-[#AFAFAF] font-black uppercase tracking-widest">القسم</span>
                    <Building size={14} className="text-[#AFAFAF]" />
                  </div>
                  <p className="text-[14px] font-black text-[#2F2F2F]">{profile.department}</p>
                </div>
              )}

              {profile?.supervisorName && (
                <div className="bg-[#F9F8F6] rounded-[16px] p-4 text-right">
                  <div className="flex items-center justify-end gap-2 mb-2">
                    <span className="text-[10px] text-[#AFAFAF] font-black uppercase tracking-widest">المشرف</span>
                    <UserCog size={14} className="text-[#AFAFAF]" />
                  </div>
                  <p className="text-[14px] font-black text-[#2F2F2F]">{profile.supervisorName}</p>
                </div>
              )}

              <div className="bg-[#F9F8F6] rounded-[16px] p-4 text-right">
                <div className="flex items-center justify-end gap-2 mb-2">
                  <span className="text-[10px] text-[#AFAFAF] font-black uppercase tracking-widest">تاريخ الانضمام</span>
                  <Calendar size={14} className="text-[#AFAFAF]" />
                </div>
                <p className="text-[14px] font-black text-[#2F2F2F]">{formatDate(profile?.createdAt)}</p>
              </div>

              <div className="bg-[#F9F8F6] rounded-[16px] p-4 text-right">
                <div className="flex items-center justify-end gap-2 mb-2">
                  <span className="text-[10px] text-[#AFAFAF] font-black uppercase tracking-widest">آخر تسجيل دخول</span>
                  <Clock size={14} className="text-[#AFAFAF]" />
                </div>
                <p className="text-[14px] font-black text-[#2F2F2F]">{formatDateTime(profile?.lastLoginAt)}</p>
              </div>
            </div>
          </div>

          {/* Edit Profile Form */}
          <div className="bg-white rounded-[24px] shadow-[0_4px_25px_rgba(0,0,0,0.03)] border border-black/5 overflow-hidden mb-6">
            <div className="p-6">
              <h3 className="text-right text-[18px] font-black text-[#2F2F2F] mb-6">تعديل البيانات</h3>

              <form onSubmit={handleSubmit} className="space-y-6">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
                  {/* Full Name */}
                  <div className="space-y-3">
                    <label className="block text-right text-[10px] text-[#AFAFAF] font-black uppercase tracking-widest mb-1 pr-1">الاسم الكامل</label>
                    <div className="relative">
                      <input
                        type="text"
                        value={formData.fullName}
                        onChange={(e) => { setFormData({ ...formData, fullName: e.target.value }); setFieldErrors(prev => { const n = {...prev}; delete n.fullName; return n; }); }}
                        className={`w-full h-14 rounded-[18px] bg-[#F9F8F6] ${fieldErrors.fullName ? "ring-2 ring-red-400" : "border-0"} px-6 pr-14 text-right outline-none focus:ring-4 focus:ring-[#7895B2]/10 transition-all font-black text-[15px] text-[#2F2F2F]`}
                        required
                        minLength={3}
                        maxLength={100}
                        placeholder="أدخل اسمك الكامل"
                      />
                      <User className="absolute right-5 top-1/2 -translate-y-1/2 text-[#AFAFAF]" size={20} />
                    </div>
                    {fieldErrors.fullName && <p className="text-right text-red-500 text-[11px] font-bold">{fieldErrors.fullName}</p>}
                  </div>

                  {/* Phone Number */}
                  <div className="space-y-3">
                    <label className="block text-right text-[10px] text-[#AFAFAF] font-black uppercase tracking-widest mb-1 pr-1">رقم الهاتف</label>
                    <div className="relative">
                      <input
                        type="tel"
                        value={formData.phoneNumber}
                        onChange={(e) => { setFormData({ ...formData, phoneNumber: e.target.value }); setFieldErrors(prev => { const n = {...prev}; delete n.phoneNumber; return n; }); }}
                        className={`w-full h-14 rounded-[18px] bg-[#F9F8F6] ${fieldErrors.phoneNumber ? "ring-2 ring-red-400" : "border-0"} px-6 pr-14 text-right outline-none focus:ring-4 focus:ring-[#7895B2]/10 transition-all font-black text-[15px] text-[#2F2F2F]`}
                        required
                        minLength={9}
                        maxLength={15}
                        placeholder="+970599000000"
                      />
                      <Phone className="absolute right-5 top-1/2 -translate-y-1/2 text-[#AFAFAF]" size={20} />
                    </div>
                    {fieldErrors.phoneNumber && <p className="text-right text-red-500 text-[11px] font-bold">{fieldErrors.phoneNumber}</p>}
                  </div>

                  {/* Email */}
                  <div className="space-y-3">
                    <label className="block text-right text-[10px] text-[#AFAFAF] font-black uppercase tracking-widest mb-1 pr-1">البريد الإلكتروني</label>
                    <div className="relative">
                      <input
                        type="email"
                        value={formData.email}
                        onChange={(e) => { setFormData({ ...formData, email: e.target.value }); setFieldErrors(prev => { const n = {...prev}; delete n.email; return n; }); }}
                        className={`w-full h-14 rounded-[18px] bg-[#F9F8F6] ${fieldErrors.email ? "ring-2 ring-red-400" : "border-0"} px-6 pr-14 text-right outline-none focus:ring-4 focus:ring-[#7895B2]/10 transition-all font-black text-[15px] text-[#2F2F2F]`}
                        maxLength={100}
                        placeholder="example@mail.com"
                      />
                      <Mail className="absolute right-5 top-1/2 -translate-y-1/2 text-[#AFAFAF]" size={20} />
                    </div>
                    {fieldErrors.email && <p className="text-right text-red-500 text-[11px] font-bold">{fieldErrors.email}</p>}
                  </div>

                  {/* Username (Read Only) */}
                  <div className="space-y-3">
                    <label className="block text-right text-[10px] text-[#AFAFAF] font-black uppercase tracking-widest mb-1 pr-1">اسم المستخدم (ثابت)</label>
                    <div className="relative">
                      <input
                        type="text"
                        value={profile?.username || ""}
                        readOnly
                        className="w-full h-14 rounded-[18px] bg-[#F3F1ED] border border-black/5 px-6 pr-14 text-right cursor-not-allowed font-bold text-[#AFAFAF]"
                      />
                      <Shield className="absolute right-5 top-1/2 -translate-y-1/2 text-[#AFAFAF]" size={20} />
                    </div>
                  </div>
                </div>

                <div className="pt-8 border-t border-black/5 flex justify-start">
                  <button
                    type="submit"
                    disabled={saving}
                    className="h-14 px-10 bg-[#7895B2] text-white rounded-[18px] font-black text-[15px] flex items-center justify-center gap-3 hover:bg-[#647e99] transition-all shadow-lg shadow-[#7895B2]/20 active:scale-95 disabled:opacity-50 group"
                  >
                    {saving ? "جاري الحفظ..." : "حفظ التغييرات"}
                    <Save size={20} className="group-hover:translate-y-[-2px] transition-transform" />
                  </button>
                </div>
              </form>
            </div>
          </div>

          {/* Security Quick Link */}
          <div className="bg-white rounded-[24px] shadow-[0_4px_25px_rgba(0,0,0,0.03)] border border-black/5 overflow-hidden">
            <button
              type="button"
              onClick={() => navigate("/settings")}
              className="w-full p-6 flex items-center justify-between hover:bg-[#F9F8F6] transition-colors"
            >
              <span className="text-[14px] font-black text-[#7895B2]">تغيير كلمة المرور والإعدادات</span>
              <div className="flex items-center gap-3">
                <h3 className="text-[18px] font-black text-[#2F2F2F]">الأمان</h3>
                <KeyRound size={20} className="text-[#AFAFAF]" />
              </div>
            </button>
          </div>

        </div>
      </div>
    </div>
  );
}
