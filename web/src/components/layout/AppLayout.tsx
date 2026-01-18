import { Outlet, NavLink, useLocation } from "react-router-dom";
import { useEffect, useState } from "react";
import Sidebar from "./Sidebar";
import Topbar from "./Topbar";
import { X, User, Settings } from "lucide-react";

type UserRole = "manager" | "supervisor";

type NavItem = {
  to: string;
  label: string;
  end?: boolean;
};

const supervisorItems: NavItem[] = [
  { to: "/dashboard", label: "لوحة التحكم", end: true },
  { to: "/tasks", label: "المهام" },
  { to: "/issues", label: "البلاغات" },
  { to: "/tasks/new", label: "تعيين مهمة جديدة" },
  { to: "/zones", label: "الخريطة الحية" },
  { to: "/reports", label: "التقارير" },
];

const managerItems: NavItem[] = [
  { to: "/dashboard", label: "لوحة التحكم", end: true },
  { to: "/reports", label: "التقارير" },
  { to: "/accounts", label: "إدارة الحسابات" },
];

export default function AppLayout() {
  const location = useLocation();
  const [drawerOpen, setDrawerOpen] = useState(false);

  // Get user role from localStorage
  const getUserRole = (): UserRole => {
    try {
      const userStr = localStorage.getItem("jawlah_user");
      if (userStr) {
        const user = JSON.parse(userStr);
        const backendRole = user.role?.toLowerCase();
        // Admin sees manager menu (with account management)
        if (backendRole === "admin") return "manager";
        // Supervisor sees field operations menu
        if (backendRole === "supervisor") return "supervisor";
      }
    } catch (err) {
      console.error("Failed to parse user role:", err);
    }
    return "supervisor"; // Default fallback
  };

  const role = getUserRole();
  const items = role === "manager" ? managerItems : supervisorItems;

  useEffect(() => {
    setDrawerOpen(false);
  }, [location.pathname]);

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") setDrawerOpen(false);
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, []);

  return (
    <div dir="rtl" className="h-screen w-screen bg-background overflow-hidden">
      <div className="h-full flex flex-col">
        {/* Topbar */}
        <div className="shrink-0 w-full">
          <Topbar onMenuClick={() => setDrawerOpen(true)} />
        </div>

        {/* Body */}
        <div className="flex-1 min-h-0 flex overflow-hidden" dir="rtl">
          {/* Sidebar (Desktop only) */}
          <div className="w-[250px] shrink-0 h-full hidden md:block">
            <Sidebar role={role} />
          </div>

          {/* Main */}
          <main  className="flex-1 min-w-0 min-h-0 overflow-y-auto overflow-x-hidden">
            <Outlet />
          </main>
        </div>
      </div>

      {/* Mobile Drawer (RIGHT) */}
      <div className={drawerOpen ? "md:hidden" : "hidden"}>
        {/* Overlay */}
        <button
          type="button"
          onClick={() => setDrawerOpen(false)}
          className="fixed inset-0 bg-black/40 z-40"
          aria-label="إغلاق القائمة"
        />

        {/* Panel */}
        <aside
          className={[
            "fixed top-0 right-0 h-full w-[290px] max-w-[86vw]",
            "bg-[#7895B2] z-50",
            "shadow-2xl",
            "transform transition-transform duration-200",
            drawerOpen ? "translate-x-0" : "translate-x-full",
          ].join(" ")}
          dir="rtl"
        >
          {/* Header */}
          <div className="h-[76px] px-4 flex items-center justify-between">
            <div className="text-white font-sans font-semibold">القائمة</div>
            <button
              type="button"
              onClick={() => setDrawerOpen(false)}
              className="w-[40px] h-[40px] rounded-full bg-white/20 grid place-items-center hover:bg-white/30 transition"
              aria-label="إغلاق"
            >
              <X className="text-white" size={20} />
            </button>
          </div>

          {/* Links */}
          <nav className="px-3 pt-4 flex flex-col gap-3">
            {items.map((it) => (
              <NavLink
                key={it.to}
                to={it.to}
                end={it.end}
                className={({ isActive }) =>
                  [
                    "w-full rounded-[12px] px-4 py-3",
                    "font-sans font-semibold text-[16px]",
                    isActive ? "bg-[#F3F1ED] text-[#2F2F2F]" : "bg-white/15 text-white hover:bg-white/20",
                  ].join(" ")
                }
              >
                {it.label}
              </NavLink>
            ))}
          </nav>

          {/* Divider */}
          <div className="my-5 mx-4 h-[1px] bg-white/25" />

          {/* Profile / Settings */}
          <div className="px-3 flex flex-col gap-3">
            <button
              type="button"
              onClick={() => {
                // TODO: navigate("/profile")
                setDrawerOpen(false);
              }}
              className="w-full rounded-[12px] px-4 py-3 bg-white/15 text-white hover:bg-white/20 transition flex items-center justify-between"
            >
              <span className="font-sans font-semibold text-[16px]">الملف الشخصي</span>
              <User size={18} className="text-white" />
            </button>

            <button
              type="button"
              onClick={() => {
                // TODO: navigate("/settings")
                setDrawerOpen(false);
              }}
              className="w-full rounded-[12px] px-4 py-3 bg-white/15 text-white hover:bg-white/20 transition flex items-center justify-between"
            >
              <span className="font-sans font-semibold text-[16px]">الإعدادات</span>
              <Settings size={18} className="text-white" />
            </button>
          </div>
        </aside>
      </div>
    </div>
  );
}
