import React, { useEffect, useState } from "react";
import { useLocation, useNavigate, Link } from "react-router-dom";
import AuthLayout from "../components/auth/AuthLayout";

function digitsOnly(value: string) {
  return value.replace(/\D/g, "");
}

const OTP_LEN = 6;

export default function VerifyOtp() {
  const navigate = useNavigate();
  const location = useLocation();
  const phone = (location.state as { phone?: string } | null)?.phone;

  const [otp, setOtp] = useState("");
  const [loading, setLoading] = useState(false);

  const [error, setError] = useState("");
  const [info, setInfo] = useState("");

  useEffect(() => {
    if (!phone) navigate("/forgot-password");
  }, [phone, navigate]);

  const otpValid = otp.length === OTP_LEN;

  const onVerify = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setInfo("");

    if (!phone) {
      setError("الرجاء إدخال رقم الهاتف أولاً.");
      return;
    }
    if (!otpValid) {
      setError(`رمز التحقق يجب أن يكون ${OTP_LEN} أرقام.`);
      return;
    }

    setLoading(true);
    try {
      // TODO backend: verify OTP for phone
      // POST /auth/forgot-password/verify-otp { phone, otp }
      await new Promise((r) => setTimeout(r, 600));

      navigate("/reset-password", { state: { phone } });
    } catch (err) {
      setError("رمز التحقق غير صحيح.");
    } finally {
      setLoading(false);
    }
  };

  const onResend = async () => {
    setError("");
    setInfo("");
    if (!phone) return;

    setLoading(true);
    try {
      // TODO backend: resend OTP with rate limit
      // POST /auth/forgot-password/resend-otp { phone }
      await new Promise((r) => setTimeout(r, 500));

      setInfo("تمت إعادة إرسال رمز التحقق.");
    } catch (err) {
      setError("فشل إعادة إرسال الرمز.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <AuthLayout title="التحقق من الرمز" subtitle="أدخل رمز OTP المرسل إلى هاتفك">
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

      <form onSubmit={onVerify} className="space-y-5">
        <Field label="رقم الهاتف">
          <input
            value={phone || ""}
            readOnly
            className="w-full h-[46px] rounded-[12px] bg-[#F3F4F6] border border-black/10 px-4 text-right outline-none opacity-90 cursor-not-allowed"
          />
        </Field>

        <Field label="رمز التحقق (OTP)">
          <input
            value={otp}
            onChange={(e) =>
              setOtp(digitsOnly(e.target.value).slice(0, OTP_LEN))
            }
            placeholder="مثال: 123456"
            required
            disabled={loading}
            inputMode="numeric"
            pattern="\d*"
            maxLength={OTP_LEN}
            className="w-full h-[46px] rounded-[12px] bg-white border border-black/10 px-4 text-right outline-none focus:ring-2 focus:ring-black/10 disabled:opacity-50"
          />
          <div className="mt-2 text-[12px] text-[#6B7280] text-right">
            {otp.length}/{OTP_LEN}
          </div>
        </Field>

        <button
          type="submit"
          disabled={loading || !otpValid}
          className="w-full h-[52px] rounded-[12px] bg-[#7895B2] text-white font-sans font-semibold text-[16px] shadow-[0_2px_0_rgba(0,0,0,0.15)] hover:opacity-95 disabled:opacity-50"
        >
          {loading ? "جاري التحقق..." : "تحقق"}
        </button>

        <div className="flex items-center justify-between text-[13px]">
          <button
            type="button"
            onClick={onResend}
            disabled={loading || !phone}
            className="text-[#7895B2] font-sans font-semibold hover:opacity-80 disabled:opacity-50"
          >
            إعادة إرسال الرمز
          </button>

          <Link
            to="/forgot-password"
            className="text-[#7895B2] font-sans font-semibold hover:opacity-80"
          >
            رجوع
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
