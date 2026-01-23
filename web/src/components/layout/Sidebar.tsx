import React from "react";
import { NavLink } from "react-router-dom";
import { LayoutDashboard, ClipboardList, AlertCircle, PlusCircle, Map, BarChart3, Users, User, Settings as SettingsIcon, ShieldCheck, UserCog, Scale } from "lucide-react";

type UserRole = "manager" | "supervisor";

type NavItem = {
  to: string;
  label: string;
  icon?: React.ElementType;
  end?: boolean;
};

const supervisorItems: NavItem[] = [
  { to: "/dashboard", label: "لوحة التحكم", icon: LayoutDashboard, end: true },
  { to: "/tasks", label: "المهام", icon: ClipboardList, end: true },
  { to: "/issues", label: "البلاغات", icon: AlertCircle },
  { to: "/tasks/new", label: "تعيين مهمة جديدة", icon: PlusCircle },
  { to: "/zones", label: "الخريطة الحية", icon: Map },
  { to: "/reports", label: "التقارير", icon: BarChart3 },
];

const managerItems: NavItem[] = [
  { to: "/dashboard", label: "لوحة التحكم", icon: LayoutDashboard, end: true },
  { to: "/accounts", label: "إدارة الحسابات", icon: Users },
  { to: "/supervisors", label: "إدارة المشرفين", icon: UserCog },
  { to: "/system-settings", label: "إعدادات النظام", icon: SettingsIcon },
  { to: "/zones-admin", label: "إدارة المناطق", icon: Map },
  { to: "/task-oversight", label: "الرقابة على المهام", icon: ShieldCheck },
  { to: "/appeals", label: "مركز المراجعة", icon: Scale },
  { to: "/reports", label: "التقارير", icon: BarChart3 },
];

export default function Sidebar({ role = "supervisor" }: { role?: UserRole }) {
  const items = role === "manager" ? managerItems : supervisorItems;

  return (
    <aside className="h-full flex flex-col bg-primary border-l border-white/10 w-64 sm:w-64 md:w-[280px] shadow-sm">
      <div className="h-10 border-b border-white/5 hidden md:block" />

      {/* Pages */}
      <nav className="px-4 pt-8 pb-10 flex flex-col gap-2 overflow-y-auto">
        {items.map((it) => (
          <SideItem key={it.to} to={it.to} icon={it.icon} end={it.end}>
            {it.label}
          </SideItem>
        ))}

        {/* Desktop-only secondary links */}
        <div className="hidden md:block mt-auto">
          <div className="h-[1px] bg-white/10 my-4 mx-2" />
          <div className="flex flex-col gap-2">
            <SideItem to="/profile" icon={User}>الملف الشخصي</SideItem>
            <SideItem to="/settings" icon={SettingsIcon}>الإعدادات</SideItem>
          </div>
        </div>
      </nav>

      {/* Mobile-only: Profile + Settings inside Drawer */}
      <div className="md:hidden px-2.5 pb-6 mt-auto">
        <div className="h-[1px] bg-white/10 mb-4" />
        <div className="flex flex-col gap-2">
          <SideItem to="/profile">الصفحة الشخصية</SideItem>
          <SideItem to="/settings">الإعدادات</SideItem>
        </div>
      </div>
    </aside>
  );
}

function SideItem({
  to,
  icon: Icon,
  end,
  children,
}: {
  to: string;
  icon?: React.ElementType;
  end?: boolean;
  children: React.ReactNode;
}) {
  return (
    <NavLink
      to={to}
      end={end}
      className={({ isActive }) =>
        [
          "relative flex items-center w-full group overflow-hidden",
          "select-none font-sans font-medium",
          "text-base md:text-lg",
          "py-3 px-4 rounded-xl",
          "transition-all duration-300",
          isActive
            ? "bg-background text-primary shadow-lg shadow-black/20 scale-[1.02]"
            : "text-white/80 hover:bg-white/10 hover:text-white hover:translate-x-1",
        ].join(" ")
      }
    >
      <div className="flex items-center gap-3 relative z-10">
        {Icon && (
          <Icon
            size={20}
            className="flex-shrink-0 transition-transform duration-300 group-hover:scale-110"
          />
        )}
        <span className="flex-1 text-right">{children}</span>
      </div>
      
      {/* Active Indicator Shimmer */}
      <div className="absolute inset-0 rounded-xl bg-gradient-to-r from-white/0 via-white/20 to-white/0 translate-x-[-100%] group-hover:animate-shimmer opacity-0 group-hover:opacity-100 transition-opacity" />
    </NavLink>
  );
}
