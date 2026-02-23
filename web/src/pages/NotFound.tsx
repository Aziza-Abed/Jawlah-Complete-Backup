import { useNavigate } from "react-router-dom";
import { Home, ArrowRight } from "lucide-react";

export default function NotFound() {
  const navigate = useNavigate();

  return (
    <div dir="rtl" className="min-h-screen bg-[#F3F1ED] flex items-center justify-center p-6">
      <div className="text-center max-w-md">
        <div className="text-[120px] font-black text-[#7895B2]/20 leading-none select-none">
          404
        </div>

        <h1 className="text-[24px] font-black text-[#2F2F2F] mt-2">
          الصفحة غير موجودة
        </h1>

        <p className="text-[15px] text-[#6B7280] mt-3 leading-relaxed">
          الصفحة التي تبحث عنها غير موجودة أو تم نقلها.
        </p>

        <div className="flex items-center justify-center gap-3 mt-8">
          <button
            onClick={() => navigate("/dashboard")}
            className="h-[44px] px-6 rounded-[12px] bg-[#7895B2] text-white font-semibold text-[14px] flex items-center gap-2 hover:opacity-95 shadow-[0_2px_0_rgba(0,0,0,0.15)]"
          >
            <Home size={16} />
            الصفحة الرئيسية
          </button>

          <button
            onClick={() => navigate(-1)}
            className="h-[44px] px-6 rounded-[12px] bg-white border border-black/10 text-[#2F2F2F] font-semibold text-[14px] flex items-center gap-2 hover:opacity-95"
          >
            <ArrowRight size={16} />
            العودة
          </button>
        </div>
      </div>
    </div>
  );
}
