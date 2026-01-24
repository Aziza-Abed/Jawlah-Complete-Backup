import React, { useEffect, useMemo, useState } from "react";
import logo from "../../assets/logo.png";

type Slide = {
  title: string;
  subtitle: string;
  body: string;
};

export default function AuthLayout({
  title,
  subtitle,
  children,
}: {
  title: string;
  subtitle?: string;
  children: React.ReactNode;
}) {
  const slides: Slide[] = useMemo(
    () => [
      {
        title: "Follow Up System",
        subtitle: "Supervisor Web Portal",
        body: "سجّل دخولك كمشرف لمتابعة المهام، البلاغات، الخريطة الحية والتقارير.",
      },
      {
        title: "إدارة المهام",
        subtitle: "Tasks & Reports",
        body: "أنشئ مهام جديدة، تابع حالات التنفيذ، وراجع إثباتات الإنجاز بسهولة.",
      },
      {
        title: "متابعة ميدانية",
        subtitle: "Live Map & Issues",
        body: "اطّلع على الخريطة الحية وتابع البلاغات لتسريع الاستجابة الميدانية.",
      },
    ],
    []
  );

  const [active, setActive] = useState(0);

  useEffect(() => {
    const t = setInterval(() => {
      setActive((p) => (p + 1) % slides.length);
    }, 4500);
    return () => clearInterval(t);
  }, [slides.length]);

  const current = slides[active];

  return (
    <div
      dir="rtl"
      className="min-h-screen w-full bg-gradient-to-br from-[#E2E8F0] to-[#D9D9D9] grid place-items-center p-4"
    >
      <div className="w-full max-w-[980px] grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Left: Info Slider */}
        <div className="hidden lg:flex bg-gradient-to-tr from-[#60778E] to-[#7895B2] rounded-[24px] p-10 text-right shadow-[0_20px_50px_rgba(0,0,0,0.1)] border border-white/10 flex-col">
          <div className="flex items-center justify-end gap-4">
            <div className="text-white">
              <div className="text-[22px] font-sans font-semibold">{current.title}</div>
              <div className="text-[14px] text-white/80 mt-1">{current.subtitle}</div>
            </div>
            <img src={logo} alt="Logo" className="w-[72px] h-[72px] object-contain" />
          </div>

          <div className="mt-10 text-white/90 text-[15px] leading-relaxed">
            {current.body}
          </div>

          <div className="mt-auto pt-10 flex items-center justify-end gap-2">
            {slides.map((_, i) => (
              <span
                key={i}
                className={`h-[8px] rounded-full transition-all ${
                  i === active ? "w-[28px] bg-white" : "w-[8px] bg-white/50"
                }`}
              />
            ))}
          </div>
        </div>

        {/* Right: Auth Panel */}
        <div className="bg-white/90 backdrop-blur-md rounded-[24px] p-6 sm:p-8 shadow-[0_20px_50px_rgba(0,0,0,0.05)] border border-white/50">
          <div className="flex items-center justify-between">
            <div className="text-right">
              <h1 className="text-[#2F2F2F] font-sans font-semibold text-[22px]">
                {title}
              </h1>
              {subtitle ? (
                <div className="mt-1 text-[13px] text-[#6B7280]">{subtitle}</div>
              ) : null}
            </div>

            <div className="lg:hidden">
              <img src={logo} alt="Logo" className="w-[56px] h-[56px] object-contain" />
            </div>
          </div>

          <div className="mt-6">{children}</div>
        </div>
      </div>
    </div>
  );
}
