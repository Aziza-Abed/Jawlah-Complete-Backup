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
    } catch (err: any) {
      setMessage({ 
        type: "error", 
        text: err.response?.data?.message || "فشل تحديث الملف الشخصي" 
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
    <div className="h-full w-full bg-background overflow-auto">
      <div className="p-4 sm:p-6 md:p-8">
        <div className="max-w-[800px] mx-auto">
          <h1 className="text-right font-sans font-semibold text-[24px] text-[#2F2F2F] mb-6">
            الملف الشخصي
          </h1>

          <div className="bg-white/80 backdrop-blur-md rounded-[24px] border border-white/20 shadow-xl overflow-hidden">
            <div className="bg-[#7895B2] p-8 text-white relative">
              <div className="flex items-center justify-end gap-6 text-right">
                <div>
                  <h2 className="text-[28px] font-bold">{profile?.fullName}</h2>
                  <p className="opacity-90 text-[18px]">{profile?.role === "Admin" ? "مدير النظام" : "مشرف ميداني"}</p>
                </div>
                <div className="w-24 h-24 rounded-full bg-white/20 flex items-center justify-center border-4 border-white/30 backdrop-blur-sm">
                  <User size={48} />
                </div>
              </div>
            </div>

            <div className="p-8">
              {message && (
                <div className={`mb-6 p-4 rounded-xl flex items-center justify-end gap-3 ${
                  message.type === "success" ? "bg-green-100 text-green-700" : "bg-red-100 text-red-700"
                }`}>
                  <span className="text-right">{message.text}</span>
                  {message.type === "success" && <CheckCircle size={20} />}
                </div>
              )}

              <form onSubmit={handleSubmit} className="space-y-6">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                  {/* Full Name */}
                  <div className="space-y-2">
                    <label className="block text-right text-sm font-semibold text-[#6B7280]">الاسم الكامل</label>
                    <div className="relative">
                      <input
                        type="text"
                        value={formData.fullName}
                        onChange={(e) => setFormData({ ...formData, fullName: e.target.value })}
                        className="w-full h-12 rounded-xl bg-[#F9FAFB] border border-[#E5E7EB] px-4 pr-12 text-right outline-none focus:ring-2 focus:ring-[#7895B2]/50 transition-all font-sans"
                        required
                      />
                      <User className="absolute right-4 top-1/2 -translate-y-1/2 text-[#9CA3AF]" size={20} />
                    </div>
                  </div>

                  {/* Phone Number */}
                  <div className="space-y-2">
                    <label className="block text-right text-sm font-semibold text-[#6B7280]">رقم الهاتف</label>
                    <div className="relative">
                      <input
                        type="tel"
                        value={formData.phoneNumber}
                        onChange={(e) => setFormData({ ...formData, phoneNumber: e.target.value })}
                        className="w-full h-12 rounded-xl bg-[#F9FAFB] border border-[#E5E7EB] px-4 pr-12 text-right outline-none focus:ring-2 focus:ring-[#7895B2]/50 transition-all font-sans"
                        required
                      />
                      <Phone className="absolute right-4 top-1/2 -translate-y-1/2 text-[#9CA3AF]" size={20} />
                    </div>
                  </div>

                  {/* Email */}
                  <div className="space-y-2">
                    <label className="block text-right text-sm font-semibold text-[#6B7280]">البريد الإلكتروني</label>
                    <div className="relative">
                      <input
                        type="email"
                        value={formData.email}
                        onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                        className="w-full h-12 rounded-xl bg-[#F9FAFB] border border-[#E5E7EB] px-4 pr-12 text-right outline-none focus:ring-2 focus:ring-[#7895B2]/50 transition-all font-sans"
                      />
                      <Mail className="absolute right-4 top-1/2 -translate-y-1/2 text-[#9CA3AF]" size={20} />
                    </div>
                  </div>

                  {/* Username (Read Only) */}
                  <div className="space-y-2 opacity-70">
                    <label className="block text-right text-sm font-semibold text-[#6B7280]">اسم المستخدم</label>
                    <div className="relative">
                      <input
                        type="text"
                        value={profile?.username || ""}
                        readOnly
                        className="w-full h-12 rounded-xl bg-[#F3F4F6] border border-[#E5E7EB] px-4 pr-12 text-right cursor-not-allowed font-sans"
                      />
                      <Shield className="absolute right-4 top-1/2 -translate-y-1/2 text-[#9CA3AF]" size={20} />
                    </div>
                  </div>
                </div>

                <div className="pt-4 border-t border-[#F3F4F6] flex justify-end">
                  <button
                    type="submit"
                    disabled={saving}
                    className="h-12 px-8 bg-[#2F2F2F] text-white rounded-xl font-semibold flex items-center gap-2 hover:bg-black transition-all shadow-md active:scale-95 disabled:opacity-50"
                  >
                    {saving ? "جاري الحفظ..." : "حفظ التغييرات"}
                    <Save size={20} />
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
