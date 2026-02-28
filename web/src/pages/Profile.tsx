import React, { useEffect, useState } from "react";
import { getProfile, updateProfile } from "../api/users";
import type { UserResponse } from "../types/user";
import { User, Phone, Mail, Shield, Save, CheckCircle } from "lucide-react";

export default function Profile() {
  const [profile, setProfile] = useState<UserResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState<{ type: "success" | "error"; text: string } | null>(null);

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

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setMessage(null);

    try {
      const updated = await updateProfile(formData);
      setProfile(updated);
      setMessage({ type: "success", text: "تم تحديث الملف الشخصي بنجاح" });
      
      // Clear success message after 3 seconds
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
          {/* Header (Corrected Layout) */}
          <div className="flex items-center justify-between gap-6 mb-8">
            <div className="flex items-center gap-4">
                 <div className="p-3.5 rounded-[18px] bg-[#7895B2]/10 text-[#7895B2] shadow-sm">
                    <User size={28} />
                </div>
                <div className="text-right">
                    <h1 className="font-sans font-black text-[28px] text-[#2F2F2F] tracking-tight">
                        الملف الشخصي
                    </h1>
                    <p className="text-[14px] font-bold text-[#AFAFAF] mt-1">تعديل بياناتك الشخصية ومعلومات الحساب</p>
                </div>
            </div>
          </div>

          <div className="bg-white rounded-[24px] shadow-[0_4px_25px_rgba(0,0,0,0.03)] border border-black/5 overflow-hidden">
            {/* User Banner */}
            <div className="bg-[#7895B2] p-8 text-white relative overflow-hidden group">
              <div className="absolute top-0 left-0 w-full h-full bg-[url('https://grainy-gradients.vercel.app/noise.svg')] opacity-10 pointer-events-none"></div>
              <div className="flex flex-col md:flex-row items-center gap-8 relative z-10">
                <div className="w-32 h-32 rounded-[28px] bg-white/20 flex items-center justify-center border-4 border-white/20 backdrop-blur-md shadow-2xl transition-transform group-hover:scale-105">
                  <User size={64} className="text-white drop-shadow-lg" />
                </div>
                <div className="text-right">
                  <h2 className="text-[32px] font-black tracking-tight">{profile?.fullName}</h2>
                  <div className="flex items-center justify-end gap-2 mt-2">
                    <span className="px-3 py-1 bg-white/20 rounded-full text-[12px] font-black uppercase tracking-widest backdrop-blur-sm">
                      {profile?.role === "Admin" ? "مدير النظام" : profile?.role === "Supervisor" ? "مشرف" : "عامل ميداني"}
                    </span>
                    <Shield size={16} className="text-white/80" />
                  </div>
                </div>
              </div>
            </div>

            <div className="p-6">
              {message && (
                <div className={`mb-8 p-4 rounded-[18px] flex items-center justify-end gap-3 animate-in fade-in slide-in-from-top-4 duration-500 border ${
                  message.type === "success" ? "bg-[#8FA36A]/10 text-[#8FA36A] border-[#8FA36A]/20" : "bg-[#C86E5D]/10 text-[#C86E5D] border-[#C86E5D]/20"
                }`}>
                  <span className="text-right font-black text-[14px]">{message.text}</span>
                  {message.type === "success" && <CheckCircle size={20} />}
                </div>
              )}

              <form onSubmit={handleSubmit} className="space-y-6">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
                  {/* Full Name */}
                  <div className="space-y-3">
                    <label className="block text-right text-[10px] text-[#AFAFAF] font-black uppercase tracking-widest mb-1 pr-1">الاسم الكامل</label>
                    <div className="relative">
                      <input
                        type="text"
                        value={formData.fullName}
                        onChange={(e) => setFormData({ ...formData, fullName: e.target.value })}
                        className="w-full h-14 rounded-[18px] bg-[#F9F8F6] border-0 px-6 pr-14 text-right outline-none focus:ring-4 focus:ring-[#7895B2]/10 transition-all font-black text-[15px] text-[#2F2F2F]"
                        required
                        placeholder="أدخل اسمك الكامل"
                      />
                      <User className="absolute right-5 top-1/2 -translate-y-1/2 text-[#AFAFAF]" size={20} />
                    </div>
                  </div>

                  {/* Phone Number */}
                  <div className="space-y-3">
                    <label className="block text-right text-[10px] text-[#AFAFAF] font-black uppercase tracking-widest mb-1 pr-1">رقم الهاتف</label>
                    <div className="relative">
                      <input
                        type="tel"
                        value={formData.phoneNumber}
                        onChange={(e) => setFormData({ ...formData, phoneNumber: e.target.value })}
                        className="w-full h-14 rounded-[18px] bg-[#F9F8F6] border-0 px-6 pr-14 text-right outline-none focus:ring-4 focus:ring-[#7895B2]/10 transition-all font-black text-[15px] text-[#2F2F2F]"
                        required
                        placeholder="059xxxxxxx"
                      />
                      <Phone className="absolute right-5 top-1/2 -translate-y-1/2 text-[#AFAFAF]" size={20} />
                    </div>
                  </div>

                  {/* Email */}
                  <div className="space-y-3">
                    <label className="block text-right text-[10px] text-[#AFAFAF] font-black uppercase tracking-widest mb-1 pr-1">البريد الإلكتروني</label>
                    <div className="relative">
                      <input
                        type="email"
                        value={formData.email}
                        onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                        className="w-full h-14 rounded-[18px] bg-[#F9F8F6] border-0 px-6 pr-14 text-right outline-none focus:ring-4 focus:ring-[#7895B2]/10 transition-all font-black text-[15px] text-[#2F2F2F]"
                        placeholder="example@mail.com"
                      />
                      <Mail className="absolute right-5 top-1/2 -translate-y-1/2 text-[#AFAFAF]" size={20} />
                    </div>
                  </div>

                  {/* Username (Read Only) */}
                  <div className="space-y-3 group">
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
        </div>
      </div>
    </div>
  );
}
