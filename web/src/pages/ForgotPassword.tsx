import React, { useMemo, useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import AuthLayout from "../components/auth/AuthLayout";

function digitsOnly(value: string) {
  return value.replace(/\D/g, "");
}

export default function ForgotPassword() {
  const navigate = useNavigate();

  const [phone, setPhone] = useState("");
  const [loading, setLoading] = useState(false);

  const [error, setError] = useState("");
  const [info, setInfo] = useState("");

  const normalizedPhone = useMemo(() => digitsOnly(phone), [phone]);

  const phoneLooksValid = useMemo(() => {
    // Accept 10 digits local (e.g., 059xxxxxxx) or 12 digits with 970 (e.g., 97059xxxxxxx)
    const p = normalizedPhone;
    const isLocal = p.length === 10 && p.startsWith("05");
    const isIntl = p.length === 12 && p.startsWith("9705");
    return isLocal || isIntl;
  }, [normalizedPhone]);

  const onSendCode = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setInfo("");

    if (!phoneLooksValid) {
      setError("رقم الهاتف غير صحيح. أدخل رقمًا مثل 059xxxxxxx.");
      return;
    }

    setLoading(true);
    try {
      // TODO backend: validate phone exists, then send OTP
      // POST /auth/forgot-password/send-otp { phone }
      await new Promise((r) => setTimeout(r, 600));

      setInfo("تم إرسال رمز التحقق إلى رقم الهاتف.");
      navigate("/verify-otp", { state: { phone: normalizedPhone } });
    } catch (err) {
      setError("رقم الهاتف غير مسجّل أو غير صحيح.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <AuthLayout title="استعادة كلمة المرور" subtitle="أدخل رقم الهاتف المسجّل">
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

      <form onSubmit={onSendCode} className="space-y-5">
        <Field label="رقم الهاتف">
          <input
            value={phone}
            onChange={(e) => setPhone(digitsOnly(e.target.value))}
            placeholder="مثال: 059xxxxxxx"
            required
            disabled={loading}
            inputMode="numeric"
            pattern="\d*"
            maxLength={12}
            className="w-full h-[46px] rounded-[12px] bg-white border border-black/10 px-4 text-right outline-none focus:ring-2 focus:ring-black/10 disabled:opacity-50"
          />
        </Field>

        <button
          type="submit"
          disabled={loading || !phoneLooksValid}
          className="w-full h-[52px] rounded-[12px] bg-[#60778E] text-white font-sans font-semibold text-[16px] shadow-[0_2px_0_rgba(0,0,0,0.15)] hover:opacity-95 disabled:opacity-50"
        >
          {loading ? "جاري الإرسال..." : "إرسال الرمز"}
        </button>

        <div className="flex items-center justify-between text-[13px]">
          <Link
            to="/login"
            className="text-[#60778E] font-sans font-semibold hover:opacity-80"
          >
            رجوع لتسجيل الدخول
          </Link>
          <span className="text-[#6B7280]">سيتم إرسال OTP</span>
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
