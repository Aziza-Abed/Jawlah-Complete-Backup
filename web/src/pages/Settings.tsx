import React, { useState, useEffect } from "react";
import { changePassword } from "../api/users";
import { getMunicipalities, updateMunicipality, createMunicipality, type Municipality } from "../api/municipality";
import {
  KeyRound,
  Lock,
  ShieldCheck,
  AlertCircle,
  CheckCircle,
  Bell,
  Building2,
  Clock,
  Phone,
  Save,
  Loader2,
  Settings as SettingsIcon
} from "lucide-react";

type SettingsTab = "notifications" | "password" | "municipality";

export default function Settings() {
  const [activeTab, setActiveTab] = useState<SettingsTab>("notifications");
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState<{ type: "success" | "error"; text: string } | null>(null);

  // Municipality state (for admin)
  const [municipality, setMunicipality] = useState<Municipality | null>(null);
  const [municipalityLoading, setMunicipalityLoading] = useState(false);
  const [isEditingMunicipality, setIsEditingMunicipality] = useState(false);
  const [municipalityForm, setMunicipalityForm] = useState({
    name: "",
    contactPhone: "",
    defaultStartTime: "07:00:00",
    defaultEndTime: "15:00:00",
    defaultGraceMinutes: 15,
    maxAcceptableAccuracyMeters: 50,
  });

  // Get user info from localStorage
  const getUserInfo = () => {
    try {
      const userStr = localStorage.getItem("followup_user");
      if (userStr) {
        return JSON.parse(userStr);
      }
    } catch (e) {
      console.warn("Failed to parse user data");
    }
    return null;
  };

  const user = getUserInfo();
  const isAdmin = user?.role === "Admin" || user?.role === "Manager";

  // Fetch municipality data for admin
  useEffect(() => {
    if (isAdmin) {
      fetchMunicipality();
    }
  }, [isAdmin]);

  const fetchMunicipality = async () => {
    try {
      setMunicipalityLoading(true);
      const data = await getMunicipalities();
      if (data && data.length > 0) {
        setMunicipality(data[0]);
        setMunicipalityForm({
          name: data[0].name || "",
          contactPhone: data[0].contactPhone || "",
          defaultStartTime: data[0].defaultStartTime || "07:00:00",
          defaultEndTime: data[0].defaultEndTime || "15:00:00",
          defaultGraceMinutes: data[0].defaultGraceMinutes || 15,
          maxAcceptableAccuracyMeters: data[0].maxAcceptableAccuracyMeters || 50,
        });
      }
    } catch (err) {
      console.error("Failed to fetch municipality", err);
    } finally {
      setMunicipalityLoading(false);
    }
  };

  const handleMunicipalitySave = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setMessage(null);
    try {
      if (municipality) {
        await updateMunicipality(municipality.municipalityId, municipalityForm);
        setMunicipality({ ...municipality, ...municipalityForm });
        setMessage({ type: "success", text: "تم حفظ إعدادات البلدية بنجاح" });
        setIsEditingMunicipality(false);
      } else {
        await createMunicipality(municipalityForm);
        setMessage({ type: "success", text: "تم إنشاء إعدادات البلدية بنجاح" });
        fetchMunicipality();
      }
    } catch (err) {
      setMessage({ type: "error", text: "فشل حفظ الإعدادات" });
    } finally {
      setSaving(false);
      setTimeout(() => setMessage(null), 5000);
    }
  };

  // Password form state
  const [passwordForm, setPasswordForm] = useState({
    oldPassword: "",
    newPassword: "",
    confirmPassword: "",
  });

  // Handle password change
  const handlePasswordSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setMessage(null);

    if (passwordForm.newPassword !== passwordForm.confirmPassword) {
      setMessage({ type: "error", text: "كلمتا المرور الجديدتان غير متطابقتين" });
      return;
    }

    if (passwordForm.newPassword.length < 8) {
      setMessage({ type: "error", text: "كلمة المرور يجب أن تكون 8 أحرف على الأقل" });
      return;
    }

    setSaving(true);
    try {
      await changePassword({
        oldPassword: passwordForm.oldPassword,
        newPassword: passwordForm.newPassword,
      });
      setMessage({ type: "success", text: "تم تغيير كلمة المرور بنجاح" });
      setPasswordForm({ oldPassword: "", newPassword: "", confirmPassword: "" });
    } catch (err) {
      const axiosErr = err as { response?: { data?: { message?: string } } };
      setMessage({
        type: "error",
        text: axiosErr.response?.data?.message || "فشل تغيير كلمة المرور. يرجى التأكد من كلمة المرور القديمة.",
      });
    } finally {
      setSaving(false);
      setTimeout(() => setMessage(null), 5000);
    }
  };

  const tabs: { id: SettingsTab; label: string; icon: React.ReactNode; adminOnly?: boolean }[] = [
    { id: "notifications", label: "الإشعارات", icon: <Bell size={18} /> },
    { id: "password", label: "كلمة المرور", icon: <KeyRound size={18} /> },
    { id: "municipality", label: "إعدادات البلدية", icon: <Building2 size={18} />, adminOnly: true },
  ];

  // Filter tabs based on role
  const visibleTabs = tabs.filter(tab => !tab.adminOnly || isAdmin);

  const renderContent = () => {
    switch (activeTab) {
      case "notifications":
        return (
          <div className="space-y-6">
            <div className="flex items-center gap-4 mb-8 pb-6 border-b border-black/5">
              <div className="p-4 bg-[#8FA36A]/10 rounded-[20px] text-[#8FA36A] shadow-sm">
                <Bell size={28} />
              </div>
              <div className="text-right">
                <h2 className="text-xl font-black text-[#2F2F2F]">إعدادات الإشعارات</h2>
                <p className="text-[13px] font-bold text-[#AFAFAF]">تحكم في تنبيهات النظام فور حدوثها</p>
              </div>
            </div>

            <div className="grid gap-4">
              <NotificationToggle
                title="إشعارات المهام الجديدة"
                description="تلقي إشعار فوري عند إسناد مهام ميدانية جديدة لك"
                storageKey="new_tasks"
              />
              <NotificationToggle
                title="تذكير بدء الدوام"
                description="تنبيه ذكي قبل موعد بدء العمل بـ 15 دقيقة"
                storageKey="work_reminder"
              />
              <NotificationToggle
                title="إشعارات البلاغات"
                description="تلقي إشعارات عند تحديث حالة البلاغات التي تتابعها"
                storageKey="issues"
              />

              <div className="bg-[#8FA36A]/10 p-4 rounded-xl border border-[#8FA36A]/20 mt-4">
                 <p className="text-[11px] text-[#8FA36A] text-right font-black leading-relaxed">
                   <CheckCircle size={12} className="inline ml-1" />
                   يتم حفظ إعدادات الإشعارات تلقائياً على هذا الجهاز.
                 </p>
              </div>
            </div>
          </div>
        );

      case "password":
        return (
          <div className="space-y-6">
            <div className="flex items-center gap-4 mb-8 pb-6 border-b border-black/5">
              <div className="p-4 bg-[#C86E5D]/10 rounded-[20px] text-[#C86E5D] shadow-sm">
                <KeyRound size={28} />
              </div>
              <div className="text-right">
                <h2 className="text-xl font-black text-[#2F2F2F]">تغيير كلمة المرور</h2>
                <p className="text-[13px] font-bold text-[#AFAFAF]">تحديث كلمة المرور الخاصة بك بشكل دوري</p>
              </div>
            </div>

            <form onSubmit={handlePasswordSubmit} className="space-y-6">
              <div className="space-y-3">
                <label className="block text-right text-[10px] text-[#AFAFAF] font-black uppercase tracking-widest mb-1 pr-1">كلمة المرور الحالية</label>
                <div className="relative">
                  <input
                    type="password"
                    value={passwordForm.oldPassword}
                    onChange={(e) => setPasswordForm({ ...passwordForm, oldPassword: e.target.value })}
                    className="w-full h-14 rounded-[18px] bg-[#F9F8F6] border-0 px-6 pr-14 text-right outline-none focus:ring-4 focus:ring-[#7895B2]/10 transition-all font-black text-[15px] text-[#2F2F2F]"
                    required
                  />
                  <Lock className="absolute right-5 top-1/2 -translate-y-1/2 text-[#AFAFAF]" size={20} />
                </div>
              </div>

              <div className="space-y-3">
                <label className="block text-right text-[10px] text-[#AFAFAF] font-black uppercase tracking-widest mb-1 pr-1">كلمة المرور الجديدة</label>
                <div className="relative">
                  <input
                    type="password"
                    value={passwordForm.newPassword}
                    onChange={(e) => setPasswordForm({ ...passwordForm, newPassword: e.target.value })}
                    className="w-full h-14 rounded-[18px] bg-[#F9F8F6] border-0 px-6 pr-14 text-right outline-none focus:ring-4 focus:ring-[#7895B2]/10 transition-all font-black text-[15px] text-[#2F2F2F]"
                    required
                    minLength={8}
                  />
                  <ShieldCheck className="absolute right-5 top-1/2 -translate-y-1/2 text-[#AFAFAF]" size={20} />
                </div>
                <p className="text-[10px] text-[#6B7280] font-bold text-right mr-1">يجب أن تكون 8 أحرف على الأقل</p>
              </div>

              <div className="space-y-3">
                <label className="block text-right text-[10px] text-[#AFAFAF] font-black uppercase tracking-widest mb-1 pr-1">تأكيد كلمة المرور الجديدة</label>
                <div className="relative">
                  <input
                    type="password"
                    value={passwordForm.confirmPassword}
                    onChange={(e) => setPasswordForm({ ...passwordForm, confirmPassword: e.target.value })}
                    className="w-full h-14 rounded-[18px] bg-[#F9F8F6] border-0 px-6 pr-14 text-right outline-none focus:ring-4 focus:ring-[#7895B2]/10 transition-all font-black text-[15px] text-[#2F2F2F]"
                    required
                  />
                  <Lock className="absolute right-5 top-1/2 -translate-y-1/2 text-[#AFAFAF]" size={20} />
                </div>
              </div>

              <div className="pt-4 border-t border-black/5 flex justify-start">
                  <button
                    type="submit"
                    disabled={saving}
                    className="h-14 px-10 bg-[#7895B2] text-white rounded-[18px] font-black text-[15px] flex items-center justify-center gap-3 hover:bg-[#647e99] transition-all shadow-lg shadow-[#7895B2]/20 active:scale-95 disabled:opacity-50 group"
                  >
                    {saving ? "جاري التحديث..." : "تحديث كلمة المرور"}
                    <Save size={20} className="group-hover:translate-y-[-2px] transition-transform" />
                  </button>
              </div>
            </form>
          </div>
        );

      case "municipality":
        if (!isAdmin) return null;
        return (
          <div className="space-y-6">
            <div className="flex items-center gap-4 mb-8 pb-6 border-b border-black/5">
              <div className="p-4 bg-[#7895B2]/10 rounded-[20px] text-[#7895B2] shadow-sm">
                <Building2 size={28} />
              </div>
              <div className="flex-1">
                 <div className="flex items-center justify-between gap-4">
                    <div className="text-right">
                        <h2 className="text-xl font-black text-[#2F2F2F]">إعدادات البلدية</h2>
                        <p className="text-[13px] font-bold text-[#AFAFAF]">أوقات العمل والمعايير التقنية للنظام</p>
                    </div>
                    {municipality && !isEditingMunicipality && (
                        <button
                            onClick={() => setIsEditingMunicipality(true)}
                            className="bg-[#7895B2] text-white px-6 py-2.5 rounded-[15px] font-black text-[13px] flex items-center gap-2 hover:bg-[#647e99] transition-all"
                        >
                            تعديل الإعدادات
                        </button>
                    )}
                 </div>
              </div>
            </div>

            {municipalityLoading ? (
              <div className="flex items-center justify-center py-20">
                <Loader2 size={40} className="animate-spin text-[#7895B2] opacity-20" />
              </div>
            ) : (
              <form onSubmit={handleMunicipalitySave} className="space-y-6">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
                    {/* Municipality Name */}
                    <div className="space-y-3">
                        <label className="block text-right text-[10px] text-[#AFAFAF] font-black uppercase tracking-widest mb-1 pr-1">اسم البلدية</label>
                        <div className="relative">
                            <input
                            type="text"
                            value={municipalityForm.name}
                            onChange={(e) => setMunicipalityForm({ ...municipalityForm, name: e.target.value })}
                            disabled={!isEditingMunicipality && !!municipality}
                            className="w-full h-14 rounded-[18px] bg-[#F9F8F6] border-0 px-6 pr-14 text-right outline-none focus:ring-4 focus:ring-[#7895B2]/10 transition-all font-black text-[15px] text-[#2F2F2F] disabled:opacity-40"
                            required
                            />
                            <Building2 className="absolute right-5 top-1/2 -translate-y-1/2 text-[#AFAFAF]" size={20} />
                        </div>
                    </div>

                    {/* Contact Phone */}
                    <div className="space-y-3">
                        <label className="block text-right text-[10px] text-[#AFAFAF] font-black uppercase tracking-widest mb-1 pr-1">رقم الهاتف / الخط الساخن</label>
                        <div className="relative">
                            <input
                            type="text"
                            value={municipalityForm.contactPhone}
                            onChange={(e) => setMunicipalityForm({ ...municipalityForm, contactPhone: e.target.value })}
                            disabled={!isEditingMunicipality && !!municipality}
                            className="w-full h-14 rounded-[18px] bg-[#F9F8F6] border-0 px-6 pr-14 text-right outline-none focus:ring-4 focus:ring-[#7895B2]/10 transition-all font-black text-[15px] text-[#2F2F2F] disabled:opacity-40"
                            />
                            <Phone className="absolute right-5 top-1/2 -translate-y-1/2 text-[#AFAFAF]" size={20} />
                        </div>
                    </div>

                    {/* Work Hours Start */}
                    <div className="space-y-3">
                        <label className="block text-right text-[10px] text-[#AFAFAF] font-black uppercase tracking-widest mb-1 pr-1">وقت بدء الدوام</label>
                        <div className="relative">
                            <input
                            type="time"
                            value={municipalityForm.defaultStartTime?.substring(0, 5)}
                            onChange={(e) => setMunicipalityForm({ ...municipalityForm, defaultStartTime: e.target.value + ":00" })}
                            disabled={!isEditingMunicipality && !!municipality}
                            className="w-full h-14 rounded-[18px] bg-[#F9F8F6] border-0 px-6 pr-14 text-right outline-none focus:ring-4 focus:ring-[#7895B2]/10 transition-all font-black text-[15px] text-[#2F2F2F] disabled:opacity-40"
                            />
                            <Clock className="absolute right-5 top-1/2 -translate-y-1/2 text-[#AFAFAF]" size={20} />
                        </div>
                    </div>

                    {/* Work Hours End */}
                    <div className="space-y-3">
                        <label className="block text-right text-[10px] text-[#AFAFAF] font-black uppercase tracking-widest mb-1 pr-1">وقت انتهاء الدوام</label>
                        <div className="relative">
                            <input
                            type="time"
                            value={municipalityForm.defaultEndTime?.substring(0, 5)}
                            onChange={(e) => setMunicipalityForm({ ...municipalityForm, defaultEndTime: e.target.value + ":00" })}
                            disabled={!isEditingMunicipality && !!municipality}
                            className="w-full h-14 rounded-[18px] bg-[#F9F8F6] border-0 px-6 pr-14 text-right outline-none focus:ring-4 focus:ring-[#7895B2]/10 transition-all font-black text-[15px] text-[#2F2F2F] disabled:opacity-40"
                            />
                            <Clock className="absolute right-5 top-1/2 -translate-y-1/2 text-[#AFAFAF]" size={20} />
                        </div>
                    </div>

                    {/* Grace Period */}
                    <div className="space-y-3">
                        <label className="block text-right text-[10px] text-[#AFAFAF] font-black uppercase tracking-widest mb-1 pr-1">فترة السماح (دقائق)</label>
                        <input
                            type="number"
                            value={municipalityForm.defaultGraceMinutes}
                            onChange={(e) => setMunicipalityForm({ ...municipalityForm, defaultGraceMinutes: parseInt(e.target.value) })}
                            disabled={!isEditingMunicipality && !!municipality}
                            className="w-full h-14 rounded-[18px] bg-[#F9F8F6] border-0 px-6 text-right outline-none focus:ring-4 focus:ring-[#7895B2]/10 transition-all font-black text-[15px] text-[#2F2F2F] disabled:opacity-40"
                        />
                    </div>

                    {/* GPS Accuracy */}
                    <div className="space-y-3">
                        <label className="block text-right text-[10px] text-[#AFAFAF] font-black uppercase tracking-widest mb-1 pr-1">دقة GPS المقبولة (متر)</label>
                        <input
                            type="number"
                            value={municipalityForm.maxAcceptableAccuracyMeters}
                            onChange={(e) => setMunicipalityForm({ ...municipalityForm, maxAcceptableAccuracyMeters: parseInt(e.target.value) })}
                            disabled={!isEditingMunicipality && !!municipality}
                            className="w-full h-14 rounded-[18px] bg-[#F9F8F6] border-0 px-6 text-right outline-none focus:ring-4 focus:ring-[#7895B2]/10 transition-all font-black text-[15px] text-[#2F2F2F] disabled:opacity-40"
                        />
                    </div>
                </div>

                {(isEditingMunicipality || !municipality) && (
                  <div className="pt-8 border-t border-black/5 flex justify-start gap-4">
                     <button
                        type="submit"
                        disabled={saving}
                        className="h-14 px-10 bg-[#7895B2] text-white rounded-[18px] font-black text-[15px] flex items-center justify-center gap-3 hover:bg-[#647e99] transition-all shadow-lg shadow-[#7895B2]/20 active:scale-95 disabled:opacity-50 group"
                      >
                        {saving ? "جاري الحفظ..." : "حفظ الإعدادات"}
                        <Save size={20} className="group-hover:translate-y-[-2px] transition-transform" />
                      </button>
                      <button
                        type="button"
                        onClick={() => setIsEditingMunicipality(false)}
                        className="h-14 px-8 bg-[#F3F1ED] text-[#6B7280] rounded-[18px] font-black text-[15px] hover:bg-[#E5E7EB] transition-all"
                      >
                        إلغاء
                      </button>
                  </div>
                )}
              </form>
            )}
          </div>
        );

      default:
        return null;
    }
  };

  return (
    <div className="h-full w-full bg-[#F3F1ED] overflow-auto">
      <div className="p-4 sm:p-6 md:p-8">
        <div className="max-w-[1200px] mx-auto space-y-6">
          {/* Header (Corrected RTL Layout) */}
          <div className="flex items-center justify-between gap-6">
            <div className="flex items-center gap-4">
                 <div className="p-3.5 rounded-[18px] bg-[#7895B2]/10 text-[#7895B2] shadow-sm">
                    <SettingsIcon size={28} />
                </div>
                <div className="text-right">
                    <h1 className="font-sans font-black text-[28px] text-[#2F2F2F] tracking-tight">
                        الإعدادات العامة
                    </h1>
                    <p className="text-[14px] font-bold text-[#AFAFAF] mt-1">إدارة حسابك الشخصي وتفضيلات النظام وتنبيهاته</p>
                </div>
            </div>
          </div>

          {/* Message */}
          {message && (
            <div
              className={`p-4 rounded-[12px] flex items-center justify-end gap-3 ${
                message.type === "success"
                  ? "bg-[#8FA36A]/10 text-[#8FA36A] border border-[#8FA36A]/20"
                  : "bg-[#C86E5D]/10 text-[#C86E5D] border border-[#C86E5D]/20"
              }`}
            >
              <span className="font-semibold">{message.text}</span>
              {message.type === "success" ? <CheckCircle size={20} /> : <AlertCircle size={20} />}
            </div>
          )}

          {/* Main Content Grid */}
          <div className="grid grid-cols-1 lg:grid-cols-4 gap-6">
            {/* Content Card (Right side in RTL) */}
            <div className="lg:col-span-3">
              <div className="bg-white rounded-[24px] p-8 shadow-[0_4px_25px_rgba(0,0,0,0.03)] border border-black/5">
                {renderContent()}
              </div>
            </div>

            {/* Settings Navigation (Left side in RTL) */}
            <div className="lg:col-span-1">
              <div className="bg-white rounded-[24px] p-4 shadow-[0_4px_25px_rgba(0,0,0,0.03)] border border-black/5 sticky top-8">
                <nav className="space-y-3">
                  {visibleTabs.map((tab) => (
                    <button
                      key={tab.id}
                      onClick={() => setActiveTab(tab.id)}
                      className={`w-full flex items-center gap-3 px-5 py-3.5 rounded-xl transition-all text-right ${
                        activeTab === tab.id
                          ? "bg-[#7895B2] text-white shadow-lg shadow-[#7895B2]/20 scale-[1.02]"
                          : "text-[#2F2F2F] hover:bg-[#F9F8F6] hover:translate-x-[-4px]"
                      }`}
                    >
                      <div className={`p-2 rounded-lg ${activeTab === tab.id ? 'bg-white/20' : 'bg-[#F3F1ED]'}`}>
                        {React.cloneElement(tab.icon as React.ReactElement<{ size?: number }>, { size: 20 })}
                      </div>
                      <span className="font-black text-[15px]">{tab.label}</span>
                    </button>
                  ))}
                </nav>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

function NotificationToggle({ title, description, storageKey }: { title: string; description: string; storageKey: string }) {
  const [enabled, setEnabled] = React.useState(() => {
    const saved = localStorage.getItem(`notification_${storageKey}`);
    return saved !== null ? saved === "true" : true; // Default to enabled
  });

  const handleChange = (checked: boolean) => {
    setEnabled(checked);
    localStorage.setItem(`notification_${storageKey}`, String(checked));
  };

  return (
    <div className="flex items-center justify-between p-5 bg-[#F9F8F6] rounded-[20px] border border-black/5 hover:border-[#7895B2]/30 transition-all">
      <div className="text-right">
        <span className="text-[15px] font-black text-[#2F2F2F] block mb-1">{title}</span>
        <span className="text-[12px] font-bold text-[#AFAFAF]">{description}</span>
      </div>
      <label className="relative inline-flex items-center cursor-pointer">
        <input
          type="checkbox"
          checked={enabled}
          onChange={(e) => handleChange(e.target.checked)}
          className="sr-only peer"
        />
        <div className="w-12 h-6 bg-[#E5E7EB] peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full rtl:peer-checked:after:-translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:start-[2px] after:bg-white after:border-[#E5E7EB] after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-[#7895B2] shadow-inner"></div>
      </label>
    </div>
  );
}
