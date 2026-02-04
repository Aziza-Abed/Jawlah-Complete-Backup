import React, { useState, useRef, useEffect } from "react";
import { useNavigate, Link } from "react-router-dom";
import { login, verifyOtp, resendOtp } from "../api/auth";
import { useMunicipality } from "../contexts/MunicipalityContext";
import AuthLayout from "../components/auth/AuthLayout";

export default function Login() {
  const navigate = useNavigate();
  const { municipalityName: _ } = useMunicipality();

  // Login form state
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  // OTP state
  const [showOtp, setShowOtp] = useState(false);
  const [sessionToken, setSessionToken] = useState("");
  const [maskedPhone, setMaskedPhone] = useState("");
  const [otpCode, setOtpCode] = useState(["", "", "", "", "", ""]);
  const [otpLoading, setOtpLoading] = useState(false);
  const [otpError, setOtpError] = useState("");
  const [resendCooldown, setResendCooldown] = useState(60);
  const [resending, setResending] = useState(false);

  // OTP input refs
  const otpRefs = useRef<(HTMLInputElement | null)[]>([]);

  // Cooldown timer
  useEffect(() => {
    if (!showOtp || resendCooldown <= 0) return;

    const timer = setInterval(() => {
      setResendCooldown(prev => {
        if (prev <= 1) {
          clearInterval(timer);
          return 0;
        }
        return prev - 1;
      });
    }, 1000);

    return () => clearInterval(timer);
  }, [showOtp]);

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setLoading(true);

    try {
      const response = await login({ username, password });

      if (response.success) {
        // Check if OTP is required
        if (response.requiresOtp && response.sessionToken) {
          setSessionToken(response.sessionToken);
          setMaskedPhone(response.maskedPhone || "****");
          setShowOtp(true);
          setResendCooldown(60);
          setLoading(false);
          // Focus first OTP input
          setTimeout(() => otpRefs.current[0]?.focus(), 100);
          return;
        }

        // No OTP required - complete login
        if (response.token) {
          localStorage.setItem("followup_token", response.token);
          if (response.user) {
            localStorage.setItem("followup_user", JSON.stringify(response.user));
          }
          navigate("/dashboard");
        }
      } else {
        setError(response.error || "بيانات الدخول غير صحيحة");
      }
    } catch (err) {
      setError("حدث خطأ في الاتصال بالخادم");
    } finally {
      setLoading(false);
    }
  };

  const handleOtpChange = (index: number, value: string) => {
    // Only allow digits
    if (value && !/^\d$/.test(value)) return;

    const newOtp = [...otpCode];
    newOtp[index] = value;
    setOtpCode(newOtp);
    setOtpError("");

    // Auto-focus next input
    if (value && index < 5) {
      otpRefs.current[index + 1]?.focus();
    }

    // Auto-submit when all digits entered
    if (value && index === 5 && newOtp.every(d => d)) {
      handleOtpSubmit(newOtp.join(""));
    }
  };

  const handleOtpKeyDown = (index: number, e: React.KeyboardEvent) => {
    if (e.key === "Backspace" && !otpCode[index] && index > 0) {
      otpRefs.current[index - 1]?.focus();
    }
  };

  const handleOtpSubmit = async (code?: string) => {
    const finalCode = code || otpCode.join("");
    if (finalCode.length !== 6) {
      setOtpError("يرجى إدخال الرمز المكون من 6 أرقام");
      return;
    }

    setOtpLoading(true);
    setOtpError("");

    try {
      const response = await verifyOtp({
        sessionToken,
        otpCode: finalCode
      });

      if (response.success && response.token) {
        localStorage.setItem("followup_token", response.token);
        if (response.user) {
          localStorage.setItem("followup_user", JSON.stringify(response.user));
        }
        navigate("/dashboard");
      } else {
        setOtpError(response.error || "رمز التحقق غير صحيح");
        setOtpCode(["", "", "", "", "", ""]);
        otpRefs.current[0]?.focus();
      }
    } catch (err) {
      setOtpError("حدث خطأ في الاتصال بالخادم");
    } finally {
      setOtpLoading(false);
    }
  };

  const handleResendOtp = async () => {
    if (resendCooldown > 0 || resending) return;

    setResending(true);
    setOtpError("");

    try {
      const response = await resendOtp({ sessionToken });

      if (response.success) {
        setResendCooldown(response.resendCooldownSeconds || 60);
      } else {
        setOtpError(response.message || "فشل إعادة إرسال الرمز");
      }
    } catch (err) {
      setOtpError("حدث خطأ في الاتصال بالخادم");
    } finally {
      setResending(false);
    }
  };

  const handleBackToLogin = () => {
    setShowOtp(false);
    setSessionToken("");
    setMaskedPhone("");
    setOtpCode(["", "", "", "", "", ""]);
    setOtpError("");
  };

  // OTP Verification View
  if (showOtp) {
    return (
      <AuthLayout title="التحقق بخطوتين" subtitle={`تم إرسال رمز التحقق إلى ${maskedPhone}`}>
        {otpError && (
          <div className="mb-5 p-3 rounded-[12px] bg-red-100 text-red-700 text-right font-sans font-semibold">
            {otpError}
          </div>
        )}

        <div className="space-y-5">
          {/* OTP Inputs */}
          <div className="flex justify-center gap-2" dir="ltr">
            {otpCode.map((digit, index) => (
              <input
                key={index}
                ref={el => { otpRefs.current[index] = el; }}
                type="text"
                inputMode="numeric"
                maxLength={1}
                value={digit}
                onChange={e => handleOtpChange(index, e.target.value)}
                onKeyDown={e => handleOtpKeyDown(index, e)}
                disabled={otpLoading}
                className="w-11 h-12 text-center text-xl font-semibold rounded-[12px] bg-white border border-black/10 outline-none focus:ring-2 focus:ring-black/10 disabled:opacity-50"
              />
            ))}
          </div>

          <button
            onClick={() => handleOtpSubmit()}
            disabled={otpLoading || otpCode.some(d => !d)}
            className="w-full h-[52px] rounded-[12px] bg-[#7895B2] text-white font-sans font-semibold text-[16px] shadow-[0_2px_0_rgba(0,0,0,0.15)] hover:opacity-95 disabled:opacity-50"
          >
            {otpLoading ? "جاري التحقق..." : "تأكيد الدخول"}
          </button>

          <div className="flex flex-col items-center gap-3 pt-2">
            <button
              onClick={handleResendOtp}
              disabled={resendCooldown > 0 || resending}
              className="text-[13px] font-sans font-semibold text-[#7895B2] hover:opacity-80 disabled:opacity-50"
            >
              {resending
                ? "جاري الإرسال..."
                : resendCooldown > 0
                ? `إعادة إرسال الرمز (${resendCooldown})`
                : "إعادة إرسال الرمز"}
            </button>

            <button
              onClick={handleBackToLogin}
              className="text-[13px] text-[#6B7280] hover:opacity-80"
            >
              ← العودة لتسجيل الدخول
            </button>
          </div>
        </div>
      </AuthLayout>
    );
  }

  // Login Form View
  return (
    <AuthLayout title="تسجيل الدخول" subtitle="أدخل اسم المستخدم وكلمة المرور">
      {error && (
        <div className="mb-5 p-3 rounded-[12px] bg-red-100 text-red-700 text-right font-sans font-semibold">
          {error}
        </div>
      )}

      <form onSubmit={onSubmit} className="space-y-5">
        <Field label="اسم المستخدم">
          <input
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            placeholder="مثال: supervisor01"
            required
            disabled={loading}
            className="w-full h-[46px] rounded-[12px] bg-white border border-black/10 px-4 text-right outline-none focus:ring-2 focus:ring-black/10 disabled:opacity-50"
          />
        </Field>

        <Field label="كلمة المرور">
          <input
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            placeholder="••••••••"
            required
            disabled={loading}
            className="w-full h-[46px] rounded-[12px] bg-white border border-black/10 px-4 text-right outline-none focus:ring-2 focus:ring-black/10 disabled:opacity-50"
          />
        </Field>

        <div className="flex items-center justify-between gap-3 flex-wrap">
          <label className="flex items-center gap-2 text-[13px] text-[#2F2F2F]">
            <input type="checkbox" className="accent-[#7895B2]" />
            تذكرني
          </label>

          <Link
            to="/forgot-password"
            className="text-[13px] font-sans font-semibold text-[#7895B2] hover:opacity-80"
          >
            نسيت كلمة السر؟
          </Link>
        </div>

        <button
          type="submit"
          disabled={loading}
          className="w-full h-[52px] rounded-[12px] bg-[#7895B2] text-white font-sans font-semibold text-[16px] shadow-[0_2px_0_rgba(0,0,0,0.15)] hover:opacity-95 disabled:opacity-50"
        >
          {loading ? "جاري تسجيل الدخول..." : "دخول"}
        </button>
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
