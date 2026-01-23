  import { Outlet, NavLink, useLocation, useNavigate } from "react-router-dom";
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
  { to: "/tasks", label: "المهام", end: true },
  { to: "/issues", label: "البلاغات" },
  { to: "/tasks/new", label: "تعيين مهمة جديدة" },
  { to: "/zones", label: "الخريطة الحية" },
  { to: "/reports", label: "التقارير" },
];

const managerItems: NavItem[] = [
  { to: "/dashboard", label: "لوحة التحكم", end: true },
  { to: "/accounts", label: "إدارة الحسابات" },
  { to: "/supervisors", label: "إدارة المشرفين" },
  { to: "/system-settings", label: "إعدادات النظام" },
  { to: "/zones-admin", label: "إدارة المناطق" },
  { to: "/task-oversight", label: "الرقابة على المهام" },
  { to: "/appeals", label: "مركز المراجعة" },
  { to: "/reports", label: "التقارير" },
];

export default function AppLayout() {
  const location = useLocation();
  const navigate = useNavigate();
  const [drawerOpen, setDrawerOpen] = useState(false);

  // Get user role from localStorage
  const getUserRole = (): UserRole => {
    try {
      const userStr = localStorage.getItem("followup_user");
      if (userStr) {
        const user = JSON.parse(userStr);
        const backendRole = user.role?.toLowerCase();
        // Admin sees manager menu (with account management)
        if (backendRole === "admin" || backendRole === "manager") return "manager";
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
    <div dir="rtl" className="h-screen w-screen overflow-hidden bg-background">
      <div className="h-full flex flex-col">
        {/* Topbar */}
        <div className="shrink-0 w-full z-20">
          <Topbar onMenuClick={() => setDrawerOpen(true)} />
        </div>

        {/* Body */}
        <div className="flex-1 min-h-0 flex overflow-hidden relative z-10">
          {/* Sidebar (Desktop only) */}
          <div className="w-[280px] shrink-0 h-full hidden md:block border-l border-white/10">
            <Sidebar role={role} />
          </div>

          {/* Main */}
          <main className="flex-1 min-w-0 min-h-0 overflow-y-auto p-4 md:p-6 lg:p-8">
            <div className="max-w-7xl mx-auto">
              <Outlet />
            </div>
          </main>
        </div>
      </div>

      {/* Mobile Drawer (RIGHT) */}
      <div className={drawerOpen ? "md:hidden" : "hidden"}>
        {/* Overlay */}
        <div
          onClick={() => setDrawerOpen(false)}
          className="fixed inset-0 bg-black/60 backdrop-blur-sm z-40"
          aria-label="إغلاق القائمة"
        />

        {/* Panel */}
        <aside
          className={[
            "fixed top-0 right-0 h-full w-[290px] max-w-[86vw]",
            "bg-primary border-l border-white/10 z-50",
            "shadow-2xl shadow-black/50",
            "transform transition-transform duration-300 ease-out",
            drawerOpen ? "translate-x-0" : "translate-x-full",
          ].join(" ")}
        >
          {/* Header */}
          <div className="h-[76px] px-4 flex items-center justify-between border-b border-white/5">
            <div className="text-white font-sans font-bold text-xl">القائمة</div>
            <button
              type="button"
              onClick={() => setDrawerOpen(false)}
              className="w-[40px] h-[40px] rounded-full bg-white/5 grid place-items-center hover:bg-white/10 transition text-white"
              aria-label="إغلاق"
            >
              <X size={20} />
            </button>
          </div>

          {/* Links */}
          <nav className="px-3 pt-6 flex flex-col gap-2">
            {items.map((it) => (
              <NavLink
                key={it.to}
                to={it.to}
                end={it.end}
                className={({ isActive }) =>
                  [
                    "w-full rounded-xl px-4 py-3",
                    "font-sans font-medium text-[16px]",
                    "transition-all duration-200",
                    isActive 
                      ? "bg-primary text-white shadow-lg shadow-primary/20" 
                      : "text-text-secondary hover:bg-white/5 hover:text-white",
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
                navigate("/profile");
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
                navigate("/settings");
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
