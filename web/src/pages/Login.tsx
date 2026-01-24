import React, { useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import { login } from "../api/auth";
import AuthLayout from "../components/auth/AuthLayout";

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
      // TODO backend: re-enable real login when backend is available
      // const response = await login({ username, password });

      // if (response.success && response.token) {
      //   localStorage.setItem("jawlah_token", response.token);

      //   if (response.user) {
      //     localStorage.setItem("jawlah_user", JSON.stringify(response.user));
      //   }

      //   navigate("/dashboard");
      // } else {
      //   setError(response.error || "بيانات الدخول غير صحيحة");
      // }

      // Temporary front-end bypass (development only)
      localStorage.setItem("jawlah_token", "dev-token");
      localStorage.setItem(
        "jawlah_user",
        JSON.stringify({
          id: "dev",
          name: "Dev Supervisor",
          role: "Supervisor",
        })
      );

      navigate("/dashboard");
    } catch (err) {
      setError("حدث خطأ في الاتصال بالخادم");
    } finally {
      setLoading(false);
    }
  };

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
