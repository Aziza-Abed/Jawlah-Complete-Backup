import React, { useEffect, useState } from "react";
import { useLocation, useNavigate, Link } from "react-router-dom";
import AuthLayout from "../components/auth/AuthLayout";
import { resetPassword } from "../api/auth";

interface LocationState {
  sessionToken?: string;
  otpCode?: string;
}

export default function ResetPassword() {
  const navigate = useNavigate();
  const location = useLocation();

  const state = location.state as LocationState | null;
  const sessionToken = state?.sessionToken;
  const otpCode = state?.otpCode;

  const [newPass, setNewPass] = useState("");
  const [confirm, setConfirm] = useState("");

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [info, setInfo] = useState("");

  useEffect(() => {
    if (!sessionToken || !otpCode) navigate("/forgot-password");
  }, [sessionToken, otpCode, navigate]);

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setInfo("");

    if (!sessionToken || !otpCode) {
      setError("الرجاء إكمال خطوات التحقق أولاً.");
      return;
    }
    if (newPass.length < 8 || !/[A-Za-z]/.test(newPass) || !/\d/.test(newPass) || !/[^A-Za-z0-9]/.test(newPass)) {
      setError("كلمة المرور يجب أن تكون 8 أحرف على الأقل وتحتوي على حرف ورقم ورمز خاص.");
      return;
    }
    if (newPass !== confirm) {
      setError("كلمتا السر غير متطابقتين.");
      return;
    }

    setLoading(true);
    try {
      const result = await resetPassword({
        sessionToken,
        otpCode,
        newPassword: newPass,
      });

      if (result.success) {
        setInfo("تم تغيير كلمة المرور بنجاح.");
        setTimeout(() => navigate("/login"), 1500);
      } else {
        setError(result.message || "فشل تغيير كلمة السر.");
      }
    } catch {
      setError("فشل تغيير كلمة السر. قد يكون رمز التحقق خاطئاً أو منتهي الصلاحية.");
    } finally {
      setLoading(false);
    }
  };

  const canSubmit = newPass.length >= 8 && confirm === newPass;

  return (
    <AuthLayout title="تغيير كلمة المرور" subtitle="أدخل كلمة سر جديدة">
      {error && (
        <div className="mb-4 p-3 rounded-[12px] bg-red-100 text-red-700 text-right font-sans font-semibold">
          {error}
        </div>
      )}
      {info && (
        <div className="mb-4 p-3 rounded-[12px] bg-green-100 text-green-700 text-right font-sans font-semibold">
          {info}
        </div>
      )}

      <form onSubmit={onSubmit} className="space-y-5">
        <Field label="كلمة سر جديدة">
          <input
            type="password"
            value={newPass}
            onChange={(e) => setNewPass(e.target.value)}
            placeholder="••••••••"
            required
            disabled={loading}
            className="w-full h-[46px] rounded-[12px] bg-white border border-black/10 px-4 text-right outline-none focus:ring-2 focus:ring-black/10 disabled:opacity-50"
          />
        </Field>

        <Field label="تأكيد كلمة السر">
          <input
            type="password"
            value={confirm}
            onChange={(e) => setConfirm(e.target.value)}
            placeholder="••••••••"
            required
            disabled={loading}
            className="w-full h-[46px] rounded-[12px] bg-white border border-black/10 px-4 text-right outline-none focus:ring-2 focus:ring-black/10 disabled:opacity-50"
          />
          {confirm.length > 0 && confirm !== newPass ? (
            <div className="mt-2 text-[12px] text-red-600 text-right font-sans font-semibold">
              كلمتا السر غير متطابقتين
            </div>
          ) : null}
        </Field>

        <button
          type="submit"
          disabled={loading || !canSubmit}
          className="w-full h-[52px] rounded-[12px] bg-[#7895B2] text-white font-sans font-semibold text-[16px] shadow-[0_2px_0_rgba(0,0,0,0.15)] hover:opacity-95 disabled:opacity-50"
        >
          {loading ? "جاري الحفظ..." : "حفظ كلمة المرور"}
        </button>

        <div className="flex items-center justify-between text-[13px]">
          <Link
            to="/login"
            className="text-[#7895B2] font-sans font-semibold hover:opacity-80"
          >
            رجوع لتسجيل الدخول
          </Link>
        </div>
      </form>
    </AuthLayout>
  );
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <div className="text-right font-sans font-semibold text-[#2F2F2F] text-[14px] mb-2">
        {label}
      </div>
      {children}
    </div>
  );
}
