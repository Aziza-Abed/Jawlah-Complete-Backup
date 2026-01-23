import React, { useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import logo from "../assets/logo.png";
import { useMunicipality } from "../contexts/MunicipalityContext";

export default function ForgotPassword() {
  const navigate = useNavigate();
  const { municipalityName } = useMunicipality();

  const [phone, setPhone] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  const onSendCode = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setSuccess("");
    setLoading(true);

    try {
      // TODO backend:
      // POST /auth/forgot-password/send-otp  { phone }
      await new Promise((r) => setTimeout(r, 700));

      setSuccess("تم إرسال رمز التحقق إلى رقم الهاتف.");
      navigate("/reset-password", { state: { phone } });
    } catch (err) {
      setError("فشل إرسال الرمز. تأكدي من الرقم.");
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
              استعادة كلمة المرور
            </h1>
            <div className="mt-1 text-[13px] text-[#6B7280]">
              أدخل رقم الهاتف لإرسال رمز التحقق
            </div>
          </div>
          <img src={logo} alt={municipalityName} className="w-[56px] h-[56px] object-contain" />
        </div>

        {error && (
          <div className="mt-5 p-3 rounded-[12px] bg-red-100 text-red-700 text-right font-sans font-semibold">
            {error}
          </div>
        )}
        {success && (
          <div className="mt-5 p-3 rounded-[12px] bg-green-100 text-green-700 text-right font-sans font-semibold">
            {success}
          </div>
        )}

        <form onSubmit={onSendCode} className="mt-6 space-y-5">
          <div>
            <div className="text-right font-sans font-semibold text-[#2F2F2F] text-[14px] mb-2">
              رقم الهاتف
            </div>
            <input
              value={phone}
              onChange={(e) => setPhone(e.target.value)}
              placeholder="مثال: 059xxxxxxx"
              required
              disabled={loading}
              className="w-full h-[46px] rounded-[12px] bg-white border border-black/10 px-4 text-right outline-none focus:ring-2 focus:ring-black/10 disabled:opacity-50"
            />
          </div>

          <button
            type="submit"
            disabled={loading}
            className="w-full h-[52px] rounded-[12px] bg-[#60778E] text-white font-sans font-semibold text-[16px] shadow-[0_2px_0_rgba(0,0,0,0.15)] hover:opacity-95 disabled:opacity-50"
          >
            {loading ? "جاري الإرسال..." : "إرسال الرمز"}
          </button>

          <div className="flex items-center justify-between text-[13px]">
            <Link to="/login" className="text-[#60778E] font-sans font-semibold hover:opacity-80">
              رجوع لتسجيل الدخول
            </Link>
            <span className="text-[#6B7280]">سيتم إرسال OTP</span>
          </div>
        </form>

        <div className="mt-5 text-right text-[12px] text-[#6B7280]">
          {/* TODO backend: rate limit + resend rules */}
        </div>
      </div>
    </div>
  );
}
