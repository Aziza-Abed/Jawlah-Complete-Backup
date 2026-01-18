import React, { useState } from "react";
import { changePassword } from "../api/users";
import { KeyRound, Lock, ShieldCheck, AlertCircle, CheckCircle } from "lucide-react";

export default function Settings() {
  const [formData, setFormData] = useState({
    oldPassword: "",
    newPassword: "",
    confirmPassword: "",
  });

  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState<{ type: "success" | "error"; text: string } | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setMessage(null);

    if (formData.newPassword !== formData.confirmPassword) {
      setMessage({ type: "error", text: "كلمتا المرور الجديدتان غير متطابقتين" });
      return;
    }

    setSaving(true);
    try {
      await changePassword({
        oldPassword: formData.oldPassword,
        newPassword: formData.newPassword,
      });
      setMessage({ type: "success", text: "تم تغيير كلمة المرور بنجاح" });
      setFormData({ oldPassword: "", newPassword: "", confirmPassword: "" });
    } catch (err: any) {
      setMessage({ 
        type: "error", 
        text: err.response?.data?.message || "فشل تغيير كلمة المرور. يرجى التأكد من كلمة المرور القديمة." 
      });
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="h-full w-full bg-[#D9D9D9] overflow-auto">
      <div className="p-4 sm:p-6 md:p-8">
        <div className="max-w-[600px] mx-auto">
          <h1 className="text-right font-sans font-semibold text-[24px] text-[#2F2F2F] mb-6">
            الإعدادات والأمان
          </h1>

          <div className="bg-white/80 backdrop-blur-md rounded-[24px] border border-white/20 shadow-xl overflow-hidden p-8">
            <div className="flex items-center justify-end gap-3 mb-8">
              <h2 className="text-xl font-bold text-[#2F2F2F] text-right">تغيير كلمة المرور</h2>
              <div className="p-2 bg-[#7895B2]/10 rounded-lg text-[#7895B2]">
                <KeyRound size={24} />
              </div>
            </div>

            {message && (
              <div className={`mb-6 p-4 rounded-xl flex items-center justify-end gap-3 ${
                message.type === "success" ? "bg-green-100 text-green-700" : "bg-red-100 text-red-700"
              }`}>
                <span className="text-right text-sm">{message.text}</span>
                {message.type === "success" ? <CheckCircle size={20} /> : <AlertCircle size={20} />}
              </div>
            )}

            <form onSubmit={handleSubmit} className="space-y-6">
              {/* Old Password */}
              <div className="space-y-2">
                <label className="block text-right text-sm font-semibold text-[#6B7280]">كلمة المرور الحالية</label>
                <div className="relative">
                  <input
                    type="password"
                    value={formData.oldPassword}
                    onChange={(e) => setFormData({ ...formData, oldPassword: e.target.value })}
                    className="w-full h-12 rounded-xl bg-[#F9FAFB] border border-[#E5E7EB] px-4 pr-12 text-right outline-none focus:ring-2 focus:ring-[#7895B2]/50 transition-all font-sans"
                    required
                  />
                  <Lock className="absolute right-4 top-1/2 -translate-y-1/2 text-[#9CA3AF]" size={20} />
                </div>
              </div>

              <div className="h-[1px] bg-[#F3F4F6]" />

              {/* New Password */}
              <div className="space-y-2">
                <label className="block text-right text-sm font-semibold text-[#6B7280]">كلمة المرور الجديدة</label>
                <div className="relative">
                  <input
                    type="password"
                    value={formData.newPassword}
                    onChange={(e) => setFormData({ ...formData, newPassword: e.target.value })}
                    className="w-full h-12 rounded-xl bg-[#F9FAFB] border border-[#E5E7EB] px-4 pr-12 text-right outline-none focus:ring-2 focus:ring-[#7895B2]/50 transition-all font-sans"
                    required
                    minLength={8}
                  />
                  <ShieldCheck className="absolute right-4 top-1/2 -translate-y-1/2 text-[#9CA3AF]" size={20} />
                </div>
                <p className="text-right text-[12px] text-[#9CA3AF]">يجب أن تكون 8 أحرف على الأقل</p>
              </div>

              {/* Confirm New Password */}
              <div className="space-y-2">
                <label className="block text-right text-sm font-semibold text-[#6B7280]">تأكيد كلمة المرور الجديدة</label>
                <div className="relative">
                  <input
                    type="password"
                    value={formData.confirmPassword}
                    onChange={(e) => setFormData({ ...formData, confirmPassword: e.target.value })}
                    className="w-full h-12 rounded-xl bg-[#F9FAFB] border border-[#E5E7EB] px-4 pr-12 text-right outline-none focus:ring-2 focus:ring-[#7895B2]/50 transition-all font-sans"
                    required
                  />
                  <Lock className="absolute right-4 top-1/2 -translate-y-1/2 text-[#9CA3AF]" size={20} />
                </div>
              </div>

              <div className="pt-4 flex justify-end">
                <button
                  type="submit"
                  disabled={saving}
                  className="h-12 px-10 bg-[#7895B2] text-white rounded-xl font-bold flex items-center gap-2 hover:bg-[#60778E] transition-all shadow-lg active:scale-95 disabled:opacity-50"
                >
                  {saving ? "جاري التحديث..." : "تحديث كلمة المرور"}
                </button>
              </div>
            </form>
          </div>
        </div>
      </div>
    </div>
  );
}
