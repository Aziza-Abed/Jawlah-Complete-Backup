import { useState } from "react";
import Button from "../components/ui/Button";
import { login } from "../api/auth";

export default function Login() {
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setLoading(true);

    try {
      const response = await login({ username, password });

      if (response.success && response.token) {
        localStorage.setItem("jawlah_token", response.token);
        if (response.refreshToken) {
          localStorage.setItem("jawlah_refresh_token", response.refreshToken);
        }
        if (response.user) {
          localStorage.setItem("jawlah_user", JSON.stringify(response.user));
        }
        window.location.href = "/dashboard";
      } else {
        setError(response.error || "فشل تسجيل الدخول");
      }
    } catch (err) {
      setError("خطأ في الاتصال بالخادم");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen w-full bg-[#D5D5D5] flex items-center justify-center">
      <div className="w-[520px] bg-white rounded-[16px] p-8 text-right">
        <h1 className="text-[36px] font-bold">تسجيل الدخول</h1>
        <p className="mt-2 text-black/60">أدخل بيانات الدخول للمتابعة</p>

        <form onSubmit={handleSubmit} className="mt-8 space-y-4">
          <div>
            <label className="block text-[14px] font-semibold mb-2">اسم المستخدم</label>
            <input
              type="text"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              className="w-full h-[50px] rounded-[10px] bg-[#F3F1ED] border border-black/10 px-4 text-right outline-none focus:ring-2 focus:ring-black/10"
              placeholder="أدخل اسم المستخدم"
              required
              disabled={loading}
            />
          </div>

          <div>
            <label className="block text-[14px] font-semibold mb-2">كلمة المرور</label>
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="w-full h-[50px] rounded-[10px] bg-[#F3F1ED] border border-black/10 px-4 text-right outline-none focus:ring-2 focus:ring-black/10"
              placeholder="أدخل كلمة المرور"
              required
              disabled={loading}
            />
          </div>

          {error && (
            <div className="text-red-500 text-[14px] text-center">{error}</div>
          )}

          <Button
            type="submit"
            className="w-full rounded-[10px] h-[50px]"
            disabled={loading}
          >
            {loading ? "جاري الدخول..." : "دخول"}
          </Button>
        </form>
      </div>
    </div>
  );
}
