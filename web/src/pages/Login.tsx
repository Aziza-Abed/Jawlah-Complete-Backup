import React, { useState, useRef, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import logo from "../assets/logo.png";
import { login, verifyOtp, resendOtp } from "../api/auth";
import { useMunicipality } from "../contexts/MunicipalityContext";
import { ArrowRight, ShieldCheck, Loader2 } from "lucide-react";

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
      <div dir="rtl" className="min-h-screen w-full flex items-center justify-center bg-[#f3f1ed] p-4 relative overflow-hidden">
        {/* Background blobs */}
        <div className="absolute top-[-20%] right-[-10%] w-[500px] h-[500px] bg-[#7895B2]/10 rounded-full blur-[100px]" />
        <div className="absolute bottom-[-10%] left-[-10%] w-[400px] h-[400px] bg-[#c97a63]/10 rounded-full blur-[100px]" />

        <div className="w-full max-w-md relative z-10">
          <div className="glass-card p-6 sm:p-8 border border-white/60">
            {/* Header */}
            <div className="flex flex-col items-center mb-8">
              <div className="w-16 h-16 rounded-2xl bg-[#7895B2]/10 flex items-center justify-center mb-4 text-[#7895B2]">
                <ShieldCheck size={36} />
              </div>
              <h1 className="text-2xl font-bold text-[#2F2F2F]">التحقق بخطوتين</h1>
              <p className="text-[#6c757d] mt-2 text-center text-sm">
                تم إرسال رمز التحقق إلى رقم الهاتف<br />
                <span className="font-semibold text-[#7895B2] text-lg mt-1 block" dir="ltr">{maskedPhone}</span>
              </p>
            </div>

            {otpError && (
              <div className="mb-6 p-4 rounded-xl bg-red-50 text-red-600 border border-red-100 text-sm text-center font-medium animate-pulse">
                {otpError}
              </div>
            )}

            {/* Inputs */}
            <div className="flex justify-center gap-3 mb-8" dir="ltr">
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
                  className="w-12 h-14 sm:w-14 sm:h-16 text-center text-2xl font-bold rounded-xl bg-white border-2 border-transparent focus:border-[#7895B2] focus:ring-4 focus:ring-[#7895B2]/10 shadow-sm transition-all outline-none text-[#2F2F2F]"
                />
              ))}
            </div>

            {/* Actions */}
            <button
              onClick={() => handleOtpSubmit()}
              disabled={otpLoading || otpCode.some(d => !d)}
              className="w-full h-14 bg-[#7895B2] hover:bg-[#647e99] text-white font-bold rounded-xl shadow-lg shadow-[#7895B2]/20 transition-all hover:-translate-y-0.5 disabled:opacity-50 disabled:hover:translate-y-0 flex items-center justify-center gap-2"
            >
              {otpLoading && <Loader2 className="animate-spin" />}
              {otpLoading ? "جاري التحقق..." : "تأكيد الدخول"}
            </button>

            <div className="mt-6 flex flex-col items-center gap-3">
              <button
                onClick={handleResendOtp}
                disabled={resendCooldown > 0 || resending}
                className="text-sm font-medium text-[#7895B2] hover:text-[#647e99] disabled:opacity-50 transition-colors"
              >
                {resending ? "جاري الإرسال..." : resendCooldown > 0 ? `إعادة إرسال الرمز (${resendCooldown})` : "إعادة إرسال الرمز"}
              </button>

              <button
                onClick={handleBackToLogin}
                className="text-sm text-[#6c757d] hover:text-[#2F2F2F] flex items-center gap-1 transition-colors"
              >
                <ArrowRight size={16} />
                العودة لتسجيل الدخول
              </button>
            </div>
          </div>
        </div>
      </div>
    );
  }

  // Login Form View
  return (
    <div dir="rtl" className="min-h-screen w-full flex items-center justify-center bg-[#f3f1ed] p-4 text-right">
      <div className="w-full max-w-[1000px] flex flex-col lg:flex-row bg-white rounded-[32px] shadow-[0_10px_60px_rgba(0,0,0,0.05)] overflow-hidden">
        
        {/* LEFT: FORM SECTION */}
        <div className="flex-1 p-4 sm:p-6 md:p-8">
          <div className="mb-10">
            <h1 className="text-[28px] font-black text-[#2F2F2F] mb-1">تسجيل الدخول</h1>
            <p className="text-[#AFAFAF] text-[14px] font-bold">أدخل اسم المستخدم وكلمة المرور للمتابعة</p>
          </div>

          {error && (
            <div className="mb-6 p-4 rounded-xl bg-red-50 border border-red-100 flex items-center gap-3">
              <ShieldCheck size={18} className="text-red-500" />
              <p className="text-red-700 text-[13px] font-black">{error}</p>
            </div>
          )}

          <form onSubmit={onSubmit} className="space-y-6">
            <div className="space-y-3">
              <label className="text-[14px] font-black text-[#2F2F2F]">اسم المستخدم</label>
              <input
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                placeholder="مثال: supervisor01"
                required
                disabled={loading}
                className="w-full h-14 px-5 rounded-2xl bg-[#F9F8F6] border border-black/5 focus:border-[#7895B2] focus:bg-white focus:ring-4 focus:ring-[#7895B2]/10 transition-all outline-none text-[#2F2F2F] placeholder:text-[#D1D1D1] font-black text-[15px]"
              />
            </div>

            <div className="space-y-3">
              <label className="text-[14px] font-black text-[#2F2F2F]">كلمة المرور</label>
              <input
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="••••••••"
                required
                disabled={loading}
                className="w-full h-14 px-5 rounded-2xl bg-[#F9F8F6] border border-black/5 focus:border-[#7895B2] focus:bg-white focus:ring-4 focus:ring-[#7895B2]/10 transition-all outline-none text-[#2F2F2F] placeholder:text-[#D1D1D1] font-black text-[15px]"
              />
            </div>

            <div className="flex items-center justify-between">
              <label className="flex items-center gap-2 cursor-pointer group">
                <input type="checkbox" className="w-5 h-5 rounded-lg border-2 border-[#E5E7EB] text-[#7895B2] focus:ring-[#7895B2]/20" />
                <span className="text-[13px] font-bold text-[#AFAFAF] group-hover:text-[#2F2F2F] transition-colors">تذكرني على هذا الجهاز</span>
              </label>
              <button type="button" className="text-[13px] font-black text-[#2F2F2F] hover:text-[#7895B2] transition-colors">نسيت كلمة السر؟</button>
            </div>

            <button
              type="submit"
              disabled={loading}
              className="w-full h-14 bg-[#7895B2] hover:bg-[#647e99] text-white font-black text-[16px] rounded-2xl shadow-xl shadow-[#7895B2]/20 transition-all active:scale-[0.98] hover:-translate-y-1 disabled:opacity-70 disabled:hover:translate-y-0 flex items-center justify-center gap-2 mt-4"
            >
              {loading && <Loader2 className="animate-spin" size={20} />}
              {loading ? "جاري التحقق..." : "تسجيل الدخول للنظام"}
            </button>
          </form>
        </div>

        {/* RIGHT: INFO SECTION */}
        <div className="w-full lg:w-[45%] bg-[#7895B2] p-6 sm:p-8 flex flex-col items-center justify-center text-center relative overflow-hidden">
          <div className="absolute top-0 right-0 w-64 h-64 bg-white/5 rounded-full -translate-y-1/2 translate-x-1/2 blur-2xl" />
          <div className="absolute bottom-0 left-0 w-64 h-64 bg-black/5 rounded-full translate-y-1/2 -translate-x-1/2 blur-2xl" />
          
          <div className="relative z-10 flex flex-col items-center max-w-sm">
            <div className="flex items-center gap-4 mb-4">
               <div className="text-right">
                  <h2 className="text-[32px] font-black text-white leading-none">FollowUp</h2>
                  <p className="text-white/80 text-[14px] font-bold mt-1">نظام متابعة العمل الميداني</p>
               </div>
               <div className="w-16 h-16 bg-white/10 backdrop-blur-md rounded-2xl flex items-center justify-center border border-white/20">
                  <img src={logo} alt="Logo" className="w-10 h-10 object-contain" />
               </div>
            </div>

            <div className="h-0.5 w-16 bg-white/20 rounded-full mb-8"></div>
            
            <p className="text-white/90 text-[17px] font-black leading-loose">
              سجل دخولك كمشرف أو مدير لمتابعة سير العمل الميداني، وإدارة المهام، وتلقي البلاغات والتقارير الميدانية بشكل مباشر.
            </p>
          </div>
        </div>

      </div>
    </div>
  );
}
