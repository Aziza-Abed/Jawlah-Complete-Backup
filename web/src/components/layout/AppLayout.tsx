import { Outlet, NavLink, useLocation, useNavigate } from "react-router-dom";
import { useEffect, useMemo, useState } from "react";
import Sidebar from "./Sidebar";
import Topbar from "./Topbar";
import { X } from "lucide-react";
import {
  LayoutDashboard,
  ClipboardList,
  AlertCircle,
  PlusCircle,
  Map,
  BarChart3,
  Users,
  UserCog,
  Building,
  ShieldCheck,
  Scale,
  User,
  Settings as SettingsIcon,
  LogOut,
} from "lucide-react";

type UserRole = "admin" | "manager" | "supervisor";

type NavItem = {
  to: string;
  label: string;
  icon: React.ElementType;
  end?: boolean;
};

const supervisorItems: NavItem[] = [
  { to: "/dashboard", label: "لوحة التحكم", icon: LayoutDashboard, end: true },
  { to: "/tasks", label: "المهام", icon: ClipboardList },
  { to: "/issues", label: "البلاغات", icon: AlertCircle },
  { to: "/tasks/new", label: "تعيين مهمة جديدة", icon: PlusCircle },
  { to: "/zones", label: "الخريطة الحية", icon: Map },
  { to: "/reports", label: "التقارير", icon: BarChart3 },
];

const adminItems: NavItem[] = [
  { to: "/dashboard", label: "لوحة التحكم", icon: LayoutDashboard, end: true },
  { to: "/accounts", label: "إدارة الحسابات", icon: Users },
  { to: "/supervisors", label: "إدارة المشرفين", icon: UserCog },
  { to: "/departments", label: "إدارة الأقسام", icon: Building },
  { to: "/zones-admin", label: "إدارة المناطق", icon: Map },
  { to: "/task-oversight", label: "الرقابة على المهام", icon: ShieldCheck },
  { to: "/appeals", label: "مركز المراجعة", icon: Scale },
  { to: "/reports", label: "التقارير", icon: BarChart3 },
];

export default function AppLayout() {
  const location = useLocation();
  const navigate = useNavigate();
  const [drawerOpen, setDrawerOpen] = useState(false);

  const getUserRole = (): UserRole => {
    try {
      const userStr = localStorage.getItem("followup_user");
      if (userStr) {
        const user = JSON.parse(userStr);
        const backendRole = user.role?.toLowerCase();
        if (backendRole === "admin") return "admin";
        if (backendRole === "manager") return "manager";
        if (backendRole === "supervisor") return "supervisor";
      }
    } catch (err) {
      console.error("Failed to parse user role:", err);
    }
    return "supervisor";
  };

  const role = getUserRole();

  const items = useMemo(() => {
    return (role === "admin" || role === "manager") ? adminItems : supervisorItems;
  }, [role]);

  const handleLogout = () => {
    localStorage.removeItem("followup_token");
    localStorage.removeItem("followup_user");
    navigate("/login");
  };

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
    <div dir="rtl" className="h-screen w-screen bg-[#F3F1ED] overflow-hidden">
      <div className="h-full flex flex-col">
        <div className="shrink-0 w-full">
          <Topbar onMenuClick={() => setDrawerOpen(true)} />
        </div>

        <div className="flex-1 min-h-0 flex overflow-hidden" dir="rtl">
          <div className="w-[280px] shrink-0 h-full hidden md:block">
            <Sidebar role={role} />
          </div>

          <main className="flex-1 min-w-0 min-h-0 overflow-y-auto overflow-x-hidden">
            <Outlet />
          </main>
        </div>
      </div>

      {/* Mobile Drawer */}
      <div className={drawerOpen ? "md:hidden" : "hidden"}>
        <button
          type="button"
          onClick={() => setDrawerOpen(false)}
          className="fixed inset-0 bg-black/40 z-40"
          aria-label="إغلاق القائمة"
        />

        <aside
          className={[
            "fixed top-0 right-0 h-full w-[290px] max-w-[86vw]",
            "bg-[#7895B2] z-50 shadow-2xl",
            "transform transition-transform duration-200",
            drawerOpen ? "translate-x-0" : "translate-x-full",
          ].join(" ")}
          dir="rtl"
        >
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

          <nav className="px-3 pt-4 flex flex-col gap-2">
            {items.map((it) => (
              <NavLink
                key={it.to}
                to={it.to}
                end={it.end}
                className={({ isActive }) =>
                  [
                    "w-full rounded-[12px] px-4 py-2.5",
                    "font-sans font-semibold text-[14px]",
                    "flex items-center gap-3",
                    isActive
                      ? "bg-[#E2E8F0] text-[#2F2F2F]"
                      : "bg-white/15 text-white hover:bg-white/20",
                  ].join(" ")
                }
              >
                <it.icon size={18} className="shrink-0" />
                <span className="flex-1 text-right">{it.label}</span>
              </NavLink>
            ))}
          </nav>

          <div className="my-4 mx-4 h-[1px] bg-white/25" />

          <div className="px-3 flex flex-col gap-2">
            <NavLink
              to="/profile"
              className={({ isActive }) =>
                [
                  "w-full rounded-[12px] px-4 py-2.5",
                  "font-sans font-semibold text-[14px]",
                  "flex items-center gap-3",
                  isActive
                    ? "bg-[#E2E8F0] text-[#2F2F2F]"
                    : "bg-white/15 text-white hover:bg-white/20",
                ].join(" ")
              }
              onClick={() => setDrawerOpen(false)}
            >
              <User size={18} className="shrink-0" />
              <span className="flex-1 text-right">الملف الشخصي</span>
            </NavLink>

            <NavLink
              to="/settings"
              className={({ isActive }) =>
                [
                  "w-full rounded-[12px] px-4 py-2.5",
                  "font-sans font-semibold text-[14px]",
                  "flex items-center gap-3",
                  isActive
                    ? "bg-[#E2E8F0] text-[#2F2F2F]"
                    : "bg-white/15 text-white hover:bg-white/20",
                ].join(" ")
              }
              onClick={() => setDrawerOpen(false)}
            >
              <SettingsIcon size={18} className="shrink-0" />
              <span className="flex-1 text-right">الإعدادات</span>
            </NavLink>

            <button
              type="button"
              onClick={() => {
                setDrawerOpen(false);
                handleLogout();
              }}
              className="w-full rounded-[12px] px-4 py-2.5 font-sans font-semibold text-[14px] flex items-center gap-3 bg-white/15 text-white hover:bg-white/20 transition"
              aria-label="تسجيل الخروج"
            >
              <LogOut size={18} className="shrink-0" />
              <span className="flex-1 text-right">تسجيل الخروج</span>
            </button>
          </div>
        </aside>
      </div>
    </div>
  );
}
