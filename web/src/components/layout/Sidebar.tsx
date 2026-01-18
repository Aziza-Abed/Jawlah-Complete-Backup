import React from "react";
import { NavLink } from "react-router-dom";
import { LayoutDashboard, ClipboardList, AlertCircle, PlusCircle, Map, BarChart3, Users, User, Settings as SettingsIcon } from "lucide-react";

type UserRole = "manager" | "supervisor";

type NavItem = {
  to: string;
  label: string;
  icon?: React.ElementType;
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

const managerItems: NavItem[] = [
  { to: "/dashboard", label: "لوحة التحكم", icon: LayoutDashboard, end: true },
  { to: "/reports", label: "التقارير", icon: BarChart3 },
  { to: "/accounts", label: "إدارة الحسابات", icon: Users },
];

export default function Sidebar({ role = "supervisor" }: { role?: UserRole }) {
  // TODO: Replace with role from backend/auth token or /me endpoint
  const items = role === "manager" ? managerItems : supervisorItems;

  return (
    <aside className="h-full flex flex-col bg-[#7895B2] w-64 sm:w-64 md:w-[280px] shadow-[-10px_0_30px_rgba(0,0,0,0.02)] border-l border-white/5">
      <div className="h-24" />

      {/* Pages */}
      <nav className="px-4 pt-14 pb-10 flex flex-col gap-4">
        {items.map((it) => (
          <SideItem key={it.to} to={it.to} icon={it.icon} end={it.end}>
            {it.label}
          </SideItem>
        ))}

        {/* Desktop-only secondary links */}
        <div className="hidden md:block">
          <div className="h-[1px] bg-white/10 my-4 mx-4" />
          <div className="flex flex-col gap-4">
            <SideItem to="/profile" icon={User}>الملف الشخصي</SideItem>
            <SideItem to="/settings" icon={SettingsIcon}>الإعدادات</SideItem>
          </div>
        </div>
      </nav>

      <div className="flex-1" />

      {/* Mobile-only: Profile + Settings inside Drawer */}
      <div className="md:hidden px-2.5 pb-6">
        <div className="h-[1px] bg-white/25 mb-4" />

        <div className="flex flex-col gap-3">
          <SideItem to="/profile">الصفحة الشخصية</SideItem>
          <SideItem to="/settings">الإعدادات</SideItem>

          {/* اختياري (لو بدك زر تسجيل خروج بعدين)
          <button
            type="button"
            className="w-full py-4 text-right px-3 text-[#F3F1ED] font-sans font-semibold text-[18px] rounded-lg hover:bg-white/10 transition"
            onClick={() => {
              // TODO: logout
            }}
          >
            تسجيل خروج
          </button>
          */}
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
          "block w-full group",
          "select-none font-sans font-semibold",
          "text-[18px] sm:text-[20px] md:text-[24px]",
          "py-4",
          "transition-all duration-200",
          isActive
            ? "bg-white text-[#2F2F2F] shadow-[0_4px_12px_rgba(0,0,0,0.08)] rounded-[16px] px-6 scale-[1.02]"
            : "text-[#E5E7EB] px-6 text-right hover:bg-white/10 hover:text-white transition-all duration-300",
        ].join(" ")
      }
    >
      <div className="flex items-center gap-4">
        {Icon && (
          <Icon
            size={24}
            className="flex-shrink-0 transition-transform duration-200 group-hover:scale-110"
          />
        )}
        <span className="flex-1 text-right">{children}</span>
      </div>
    </NavLink>
  );
}
