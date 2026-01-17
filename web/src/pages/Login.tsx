import React, { useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import logo from "../assets/logo.png";

export default function Login() {
  const navigate = useNavigate();

  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setLoading(true);

    try {
      // TODO backend:
      // POST /auth/login  { username, password }
      // return token + role
      // store token (localStorage/cookie) then navigate
      await new Promise((r) => setTimeout(r, 700));

      navigate("/dashboard");
    } catch (err) {
      setError("بيانات الدخول غير صحيحة");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div dir="rtl" className="min-h-screen w-full bg-[#D9D9D9] grid place-items-center p-4">
      <div className="w-full max-w-[980px] grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Left: Info Card */}
        <div className="hidden lg:block bg-[#7895B2] rounded-[18px] p-10 text-right shadow-[0_10px_25px_rgba(0,0,0,0.08)]">
          <div className="flex items-center justify-end gap-4">
            <div className="text-white">
              <div className="text-[22px] font-sans font-semibold">Jawlah</div>
              <div className="text-[14px] text-white/80 mt-1">
                نظام إدارة فرق العمل والمهمات
              </div>
            </div>
            <img src={logo} alt="بلدية البيرة" className="w-[72px] h-[72px] object-contain" />
          </div>

          <div className="mt-10 text-white/90 text-[15px] leading-relaxed">
            سجّل دخولك كمشرف/مدير لمتابعة المهام، البلاغات، والخريطة الحية والتقارير.
          </div>
        </div>

        {/* Right: Login Form */}
        <div className="bg-[#F3F1ED] rounded-[18px] p-6 sm:p-8 shadow-[0_10px_25px_rgba(0,0,0,0.08)] border border-black/10">
          <div className="flex items-center justify-between">
            <div className="text-right">
              <h1 className="text-[#2F2F2F] font-sans font-semibold text-[22px]">
                تسجيل الدخول
              </h1>
              <div className="mt-1 text-[13px] text-[#6B7280]">
                أدخل اسم المستخدم وكلمة المرور
              </div>
            </div>

            <div className="lg:hidden">
              <img src={logo} alt="بلدية البيرة" className="w-[56px] h-[56px] object-contain" />
            </div>
          </div>

          {error && (
            <div className="mt-5 p-3 rounded-[12px] bg-red-100 text-red-700 text-right font-sans font-semibold">
              {error}
            </div>
          )}

          <form onSubmit={onSubmit} className="mt-6 space-y-5">
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
                <input type="checkbox" className="accent-[#60778E]" />
                تذكرني
              </label>

              <Link
                to="/forgot-password"
                className="text-[13px] font-sans font-semibold text-[#60778E] hover:opacity-80"
              >
                نسيت كلمة السر؟
              </Link>
            </div>

            <button
              type="submit"
              disabled={loading}
              className="w-full h-[52px] rounded-[12px] bg-[#60778E] text-white font-sans font-semibold text-[16px] shadow-[0_2px_0_rgba(0,0,0,0.15)] hover:opacity-95 disabled:opacity-50"
            >
              {loading ? "جاري تسجيل الدخول..." : "دخول"}
            </button>

            <div className="text-right text-[12px] text-[#6B7280] leading-relaxed">
              {/* TODO backend:
                  - login returns token/role
                  - store token then ProtectedRoute uses it */}
            </div>
          </form>
        </div>
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
