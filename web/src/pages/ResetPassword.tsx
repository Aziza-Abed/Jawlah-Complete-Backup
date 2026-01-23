import React, { useState } from "react";
import { useLocation, Link } from "react-router-dom";
import logo from "../assets/logo.png";
import { useMunicipality } from "../contexts/MunicipalityContext";

export default function ResetPassword() {
  const location = useLocation();
  const { municipalityName } = useMunicipality();
  const phone = (location.state as any)?.phone as string | undefined;

  const [code, setCode] = useState("");
  const [newPass, setNewPass] = useState("");
  const [confirm, setConfirm] = useState("");

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");

    if (!phone) {
      setError("الرجاء الرجوع وإدخال رقم الهاتف أولاً.");
      return;
    }
    if (newPass.length < 6) {
      setError("كلمة السر يجب أن تكون 6 أحرف على الأقل.");
      return;
    }
    if (newPass !== confirm) {
      setError("كلمتا السر غير متطابقتين.");
      return;
    }

    setLoading(true);
    try {
      // NOTE: Backend password reset endpoints not implemented yet
      // Required endpoints:
      //   POST /api/auth/forgot-password (send OTP)
      //   POST /api/auth/reset-password (verify OTP & reset)
      setError("خاصية إعادة تعيين كلمة السر غير متوفرة حالياً. يرجى التواصل مع المسؤول.");
    } catch (err) {
      setError("فشل التحقق من الرمز أو تغيير كلمة السر.");
    } finally {
      setLoading(false);
    }
  };

  const onResend = async () => {
    if (!phone) return;
    setLoading(true);
    setError("");
    try {
      // NOTE: Backend password reset endpoints not implemented yet
      setError("خاصية إعادة تعيين كلمة السر غير متوفرة حالياً. يرجى التواصل مع المسؤول.");
    } catch (err) {
      setError("فشل إعادة إرسال الرمز.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div dir="rtl" className="min-h-screen w-full bg-background grid place-items-center p-4">
      <div className="w-full max-w-[520px] bg-[#F3F1ED] rounded-[18px] p-6 sm:p-8 shadow-[0_10px_25px_rgba(0,0,0,0.08)] border border-black/10">
        <div className="flex items-center justify-between">
          <div className="text-right">
            <h1 className="text-[#2F2F2F] font-sans font-semibold text-[20px]">
              تغيير كلمة المرور
            </h1>
            <div className="mt-1 text-[13px] text-[#6B7280]">
              أدخل رمز التحقق ثم كلمة سر جديدة
            </div>
            {phone && (
              <div className="mt-2 text-[12px] text-[#6B7280]">
                رقم الهاتف: <span className="font-sans font-semibold text-[#2F2F2F]">{phone}</span>
              </div>
            )}
          </div>
          <img src={logo} alt={municipalityName} className="w-[56px] h-[56px] object-contain" />
        </div>

        {error && (
          <div className="mt-5 p-3 rounded-[12px] bg-red-100 text-red-700 text-right font-sans font-semibold">
            {error}
          </div>
        )}

        <form onSubmit={onSubmit} className="mt-6 space-y-5">
          <Field label="رمز التحقق (OTP)">
            <input
              value={code}
              onChange={(e) => setCode(e.target.value)}
              placeholder="مثال: 123456"
              required
              disabled={loading}
              className="w-full h-[46px] rounded-[12px] bg-white border border-black/10 px-4 text-right outline-none focus:ring-2 focus:ring-black/10 disabled:opacity-50"
            />
          </Field>

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
          </Field>

          <button
            type="submit"
            disabled={loading}
            className="w-full h-[52px] rounded-[12px] bg-[#60778E] text-white font-sans font-semibold text-[16px] shadow-[0_2px_0_rgba(0,0,0,0.15)] hover:opacity-95 disabled:opacity-50"
          >
            {loading ? "جاري الحفظ..." : "حفظ كلمة المرور"}
          </button>

          <div className="flex items-center justify-between text-[13px]">
            <button
              type="button"
              disabled={loading || !phone}
              onClick={onResend}
              className="text-[#60778E] font-sans font-semibold hover:opacity-80 disabled:opacity-50"
            >
              إعادة إرسال الرمز
            </button>

            <Link to="/login" className="text-[#60778E] font-sans font-semibold hover:opacity-80">
              رجوع لتسجيل الدخول
            </Link>
          </div>
        </form>
      </div>
    </div>
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
