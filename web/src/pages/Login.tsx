import React, { useState, useRef, useEffect } from "react";
import { useNavigate, Link } from "react-router-dom";
import logo from "../assets/logo.png";
import { login, verifyOtp, resendOtp } from "../api/auth";
import { useMunicipality } from "../contexts/MunicipalityContext";

export default function Login() {
  const navigate = useNavigate();
  const { municipalityName } = useMunicipality();

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
    if (showOtp && resendCooldown > 0) {
      const timer = setTimeout(() => setResendCooldown(resendCooldown - 1), 1000);
      return () => clearTimeout(timer);
    }
  }, [showOtp, resendCooldown]);

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
        // Clear OTP inputs on error
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
        // Note: Backend returns new sessionToken in some cases
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
      <div
        dir="rtl"
        className="min-h-screen w-full bg-background grid place-items-center p-4"
      >
        <div className="w-full max-w-md">
          <div className="bg-background-paper backdrop-blur-md rounded-[24px] p-6 sm:p-8 shadow-[0_20px_50px_rgba(0,0,0,0.05)] border border-primary/10">
            {/* Lock Icon */}
            <div className="flex justify-center mb-6">
              <div className="w-20 h-20 rounded-full bg-primary/10 flex items-center justify-center">
                <svg className="w-10 h-10 text-primary" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
                </svg>
              </div>
            </div>

            <h1 className="text-center text-text-primary font-sans font-semibold text-[22px] mb-2">
              التحقق بخطوتين
            </h1>
            <p className="text-center text-text-secondary text-[14px] mb-2">
              تم إرسال رمز التحقق إلى رقم الهاتف
            </p>
            <p className="text-center text-primary font-semibold text-[18px] mb-6 tracking-widest" dir="ltr">
              {maskedPhone}
            </p>

            {otpError && (
              <div className="mb-4 p-3 rounded-[12px] bg-accent/10 text-accent text-center font-sans text-[14px] border border-accent/20">
                {otpError}
              </div>
            )}

            {/* OTP Input Fields */}
            <div className="flex justify-center gap-2 mb-6" dir="ltr">
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
                  className="w-12 h-14 text-center text-[24px] font-semibold rounded-[12px] bg-white border border-black/10 outline-none focus:ring-2 focus:ring-primary/20 disabled:opacity-50 text-text-primary"
                />
              ))}
            </div>

            {/* Verify Button */}
            <button
              onClick={() => handleOtpSubmit()}
              disabled={otpLoading || otpCode.some(d => !d)}
              className="w-full h-[52px] rounded-[12px] bg-primary text-white font-sans font-semibold text-[16px] shadow-[0_2px_0_rgba(0,0,0,0.1)] hover:opacity-95 disabled:opacity-50 mb-4 transition-all"
            >
              {otpLoading ? "جاري التحقق..." : "تأكيد"}
            </button>

            {/* Resend OTP */}
            <div className="text-center mb-4">
              <button
                onClick={handleResendOtp}
                disabled={resendCooldown > 0 || resending}
                className="text-[14px] text-primary hover:opacity-80 disabled:opacity-50 disabled:cursor-not-allowed font-medium"
              >
                {resending
                  ? "جاري الإرسال..."
                  : resendCooldown > 0
                    ? `إعادة إرسال الرمز (${resendCooldown} ثانية)`
                    : "إعادة إرسال الرمز"}
              </button>
            </div>

            {/* Back to Login */}
            <div className="text-center">
              <button
                onClick={handleBackToLogin}
                className="text-[14px] text-text-secondary hover:text-primary flex items-center justify-center gap-2 mx-auto transition-colors"
              >
                <svg className="w-4 h-4 rotate-180" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                </svg>
                العودة لتسجيل الدخول
              </button>
            </div>

            {/* Security Note */}
            <div className="mt-6 p-3 rounded-[12px] bg-primary/5 flex items-start gap-3 border border-primary/10">
              <svg className="w-5 h-5 text-primary mt-0.5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
              </svg>
              <p className="text-[12px] text-text-secondary leading-relaxed">
                رمز التحقق صالح لمدة 5 دقائق فقط للحفاظ على أمان حسابك
              </p>
            </div>
          </div>
        </div>
      </div>
    );
  }

  // Login Form View
  return (
    <div
      dir="rtl"
      className="min-h-screen w-full bg-background grid place-items-center p-4"
    >
      <div className="w-full max-w-[980px] grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Left: Info Card */}
        <div className="hidden lg:block bg-primary rounded-[24px] p-10 text-right shadow-[0_20px_50px_rgba(0,0,0,0.1)] border border-white/10 relative overflow-hidden">
          <div className="absolute top-0 right-0 w-64 h-64 bg-white/5 rounded-full -translate-x-10 -translate-y-10 blur-3xl" />
          
          <div className="relative z-10 flex items-center justify-start gap-5">
            <img
              src={logo}
              alt={municipalityName}
              className="w-[84px] h-[84px] object-contain drop-shadow-2xl"
            />
            <div className="text-white text-right">
              <div className="text-[28px] font-black tracking-tight">FollowUp</div>
              <div className="text-[15px] text-white/90 font-medium mt-1">
                نظام متابعة العمل الميداني
              </div>
            </div>
          </div>

          <div className="mt-12 text-white/90 text-[16px] leading-loose font-medium max-w-[320px] mr-0 ml-auto opacity-90">
            سجّل دخولك كمشرف أو مدير لمتابعة سير العمل الميداني، إدارة المهام، وتلقي البلاغات والتقارير الميدانية بشكل مباشر.
          </div>
        </div>

        {/* Right: Login Form */}
        <div className="bg-background-paper backdrop-blur-md rounded-[24px] p-6 sm:p-8 shadow-[0_20px_50px_rgba(0,0,0,0.05)] border border-primary/10">
          <div className="flex items-center justify-between gap-4">
            <div className="lg:hidden">
              <img
                src={logo}
                alt={municipalityName}
                className="w-[60px] h-[60px] object-contain"
              />
            </div>

            <div className="text-right flex-1">
              <h1 className="text-text-primary font-sans font-extrabold text-[26px]">
                تسجيل الدخول
              </h1>
              <div className="mt-1 text-[14px] text-text-secondary font-medium">
                أدخل اسم المستخدم وكلمة المرور للمتابعة
              </div>
            </div>
          </div>

          {error && (
            <div className="mt-6 p-4 rounded-[16px] bg-accent/10 text-accent text-right font-sans font-bold border border-accent/20 flex items-center justify-end gap-3 text-sm">
              <span>{error}</span>
              <svg className="w-5 h-5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                 <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
            </div>
          )}

          <form onSubmit={onSubmit} className="mt-8 space-y-6">
            <Field label="اسم المستخدم">
              <input
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                placeholder="مثال: supervisor01"
                required
                disabled={loading}
                className="w-full h-[52px] rounded-[14px] bg-white border border-primary/10 px-4 text-right text-text-primary placeholder-text-muted outline-none focus:ring-2 focus:ring-primary/20 transition-all font-medium disabled:opacity-50"
              />
            </Field>

            <Field label="كلمة المرور">
              <input
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="********"
                required
                disabled={loading}
                className="w-full h-[52px] rounded-[14px] bg-white border border-primary/10 px-4 text-right text-text-primary placeholder-text-muted outline-none focus:ring-2 focus:ring-primary/20 transition-all font-medium disabled:opacity-50"
              />
            </Field>

            <div className="flex items-center justify-between gap-3 flex-wrap pt-2">
              <label className="flex items-center gap-2.5 text-[14px] text-text-primary font-medium cursor-pointer group">
                <input type="checkbox" className="w-4 h-4 rounded border-primary/20 text-primary focus:ring-primary accent-primary" />
                <span className="group-hover:text-primary transition-colors">تذكرني على هذا الجهاز</span>
              </label>

              <Link
                to="/forgot-password"
                className="text-[14px] font-bold text-primary hover:opacity-80 transition-opacity"
              >
                نسيت كلمة السر؟
              </Link>
            </div>

            <button
              type="submit"
              disabled={loading}
              className="w-full h-[56px] rounded-[16px] bg-primary text-white font-sans font-bold text-[17px] shadow-lg shadow-primary/20 hover:shadow-primary/30 active:scale-[0.98] transition-all disabled:opacity-50"
            >
              {loading ? "جاري التحقق من البيانات..." : "تسجيل الدخول للنظام"}
            </button>
          </form>
        </div>
      </div>
    </div>
  );
}

function Field({
  label,
  children,
}: {
  label: string;
  children: React.ReactNode;
}) {
  return (
    <div>
      <div className="text-right font-sans font-bold text-text-primary text-[14px] mb-2 px-1">
        {label}
      </div>
      {children}
    </div>
  );
}
