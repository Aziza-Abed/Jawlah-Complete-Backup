import React, { useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import AuthLayout from "../components/auth/AuthLayout";
import { forgotPassword } from "../api/auth";

export default function ForgotPassword() {
  const navigate = useNavigate();

  const [username, setUsername] = useState("");
  const [loading, setLoading] = useState(false);

  const [error, setError] = useState("");
  const [info, setInfo] = useState("");

  const usernameValid = username.trim().length >= 2;

  const onSendCode = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setInfo("");

    if (!usernameValid) {
      setError("اسم المستخدم غير صحيح.");
      return;
    }

    setLoading(true);
    try {
      const result = await forgotPassword({ username: username.trim() });

      if (result.success && result.sessionToken) {
        setInfo("تم إرسال رمز التحقق إلى رقم الهاتف.");
        navigate("/verify-otp", {
          state: {
            sessionToken: result.sessionToken,
            maskedPhone: result.maskedPhone || "****",
            username: username.trim(),
          },
        });
      } else {
        setError(result.message || "فشل إرسال رمز التحقق.");
      }
    } catch {
      setError("حدث خطأ. يرجى المحاولة مرة أخرى.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <AuthLayout title="استعادة كلمة المرور" subtitle="أدخل اسم المستخدم الخاص بك">
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
        <Field label="اسم المستخدم">
          <input
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            placeholder="أدخل اسم المستخدم"
            required
            disabled={loading}
            className="w-full h-[46px] rounded-[12px] bg-white border border-black/10 px-4 text-right outline-none focus:ring-2 focus:ring-black/10 disabled:opacity-50"
          />
        </Field>

        <button
          type="submit"
          disabled={loading || !usernameValid}
          className="w-full h-[52px] rounded-[12px] bg-[#7895B2] text-white font-sans font-semibold text-[16px] shadow-[0_2px_0_rgba(0,0,0,0.15)] hover:opacity-95 disabled:opacity-50"
        >
          {loading ? "جاري الإرسال..." : "إرسال رمز التحقق"}
        </button>

        <div className="flex items-center justify-between text-[13px]">
          <Link
            to="/login"
            className="text-[#7895B2] font-sans font-semibold hover:opacity-80"
          >
            رجوع لتسجيل الدخول
          </Link>
          <span className="text-[#6B7280]">سيتم إرسال OTP إلى هاتفك المسجّل</span>
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
